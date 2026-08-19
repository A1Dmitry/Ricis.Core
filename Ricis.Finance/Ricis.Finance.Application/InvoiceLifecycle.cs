using Ricis.Finance.Domain;

namespace Ricis.Finance.Application;

/// <summary>Issues one invoice with an explicit order reference, route and expiration.</summary>
public sealed record IssueInvoice(
    string OrderReference,
    Money Amount,
    InvoicePaymentRoute Route,
    DateTimeOffset ExpiresAtUtc,
    string IdempotencyKey);

/// <summary>Requests cancellation of an issued invoice.</summary>
public sealed record CancelInvoice(Guid InvoiceId);

/// <summary>Requests expiration transition after the invoice expiration instant.</summary>
public sealed record ExpireInvoice(Guid InvoiceId);

/// <summary>Requests a provider launch owned by an existing invoice.</summary>
public sealed record CreateInvoiceLaunch(
    Guid InvoiceId,
    string Description,
    string PayerIpAddress,
    Uri ReturnUrl,
    Uri NotificationUrl,
    string IdempotencyKey,
    bool Test = false);

/// <summary>Creates invoices and returns the existing aggregate for a duplicate issue key.</summary>
public sealed class IssueInvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClock _clock;

    /// <summary>Creates the invoice issuing workflow.</summary>
    public IssueInvoiceService(IInvoiceRepository invoices, IClock clock)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Issues one invoice or returns the idempotently stored aggregate.</summary>
    public async ValueTask<Invoice> HandleAsync(IssueInvoice command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await _invoices.FindByIssueIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!StringComparer.Ordinal.Equals(existing.OrderReference, command.OrderReference.Trim()) ||
                existing.Amount != command.Amount || existing.Route != command.Route)
            {
                throw new InvalidOperationException("Повторный invoice command использует тот же idempotency key с другими данными.");
            }

            return existing;
        }

        var duplicateOrder = await _invoices.FindByOrderReferenceAsync(command.OrderReference, cancellationToken);
        if (duplicateOrder is not null)
        {
            throw new InvalidOperationException($"Order reference уже принадлежит invoice {duplicateOrder.Id}.");
        }

        var issuedAt = _clock.UtcNow.ToUniversalTime();
        var invoice = new Invoice(
            Guid.NewGuid(),
            command.OrderReference,
            command.Amount,
            command.Route,
            issuedAt,
            command.ExpiresAtUtc,
            command.IdempotencyKey);
        await _invoices.StoreAsync(invoice, cancellationToken);
        return invoice;
    }
}

/// <summary>Applies a cancellation transition to an invoice aggregate.</summary>
public sealed class CancelInvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClock _clock;

    /// <summary>Creates the invoice cancellation workflow.</summary>
    public CancelInvoiceService(IInvoiceRepository invoices, IClock clock)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Cancels an existing issued invoice and persists the transition.</summary>
    public async ValueTask<Invoice> HandleAsync(CancelInvoice command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var invoice = await _invoices.FindByIdAsync(command.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} не найден.");
        invoice.Cancel(_clock.UtcNow);
        await _invoices.StoreAsync(invoice, cancellationToken);
        return invoice;
    }
}

/// <summary>Applies an expiration transition only after the invoice expiry instant.</summary>
public sealed class ExpireInvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClock _clock;

    /// <summary>Creates the invoice expiration workflow.</summary>
    public ExpireInvoiceService(IInvoiceRepository invoices, IClock clock)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Expires an existing issued invoice when its deadline has arrived.</summary>
    public async ValueTask<Invoice> HandleAsync(ExpireInvoice command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var invoice = await _invoices.FindByIdAsync(command.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} не найден.");
        invoice.Expire(_clock.UtcNow);
        await _invoices.StoreAsync(invoice, cancellationToken);
        return invoice;
    }
}

/// <summary>Creates a launch only while the invoice is active and keeps provider evidence idempotent.</summary>
public sealed class CreateInvoiceLaunchService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IInvoiceLaunchRepository _launches;
    private readonly IPaymentLaunchPort _paymentLaunch;
    private readonly IClock _clock;

    /// <summary>Creates the invoice-owned launch workflow.</summary>
    public CreateInvoiceLaunchService(
        IInvoiceRepository invoices,
        IInvoiceLaunchRepository launches,
        IPaymentLaunchPort paymentLaunch,
        IClock clock)
    {
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _launches = launches ?? throw new ArgumentNullException(nameof(launches));
        _paymentLaunch = paymentLaunch ?? throw new ArgumentNullException(nameof(paymentLaunch));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Creates or returns an idempotent provider session for an active invoice.</summary>
    public async ValueTask<InvoiceLaunchRecord> HandleAsync(CreateInvoiceLaunch command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var duplicate = await _launches.FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (duplicate is not null)
        {
            if (duplicate.InvoiceId != command.InvoiceId)
            {
                throw new InvalidOperationException("Launch idempotency key уже связан с другим invoice.");
            }

            return duplicate;
        }

        var invoice = await _invoices.FindByIdAsync(command.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} не найден.");
        if (!invoice.IsLaunchableAt(_clock.UtcNow))
        {
            if (invoice.Status == InvoiceStatus.Issued && _clock.UtcNow.ToUniversalTime() >= invoice.ExpiresAtUtc)
            {
                invoice.Expire(_clock.UtcNow);
                await _invoices.StoreAsync(invoice, cancellationToken);
            }

            throw new InvalidOperationException($"Invoice {invoice.Id} нельзя использовать для payment launch в состоянии {invoice.Status}.");
        }

        var request = new CreatePaymentLaunch(
            invoice.Route.PayerCountryCode,
            ParseRail(invoice.Route.RailCode),
            invoice.Amount,
            invoice.OrderReference,
            command.Description,
            command.PayerIpAddress,
            command.ReturnUrl,
            command.NotificationUrl,
            command.IdempotencyKey,
            invoice.ExpiresAtUtc,
            command.Test);
        var session = await _paymentLaunch.CreateAsync(request, cancellationToken);
        invoice.RecordLaunch(session.ProviderPaymentId, command.IdempotencyKey);
        await _invoices.StoreAsync(invoice, cancellationToken);
        var result = new InvoiceLaunchRecord(invoice.Id, command.IdempotencyKey, session);
        await _launches.StoreAsync(result, cancellationToken);
        return result;
    }

    private static PaymentRail ParseRail(string railCode) => Enum.TryParse<PaymentRail>(railCode, ignoreCase: false, out var rail)
        ? rail
        : throw new NotSupportedException($"Invoice содержит неподдержанный payment rail: {railCode}.");
}
