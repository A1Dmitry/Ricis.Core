namespace Ricis.Finance.Domain;

/// <summary>Lifecycle state of an invoice that owns one auditable payment order.</summary>
public enum InvoiceStatus
{
    Issued,
    Cancelled,
    Expired,
}

/// <summary>Explicit country and payment-rail identity kept by the invoice aggregate.</summary>
public sealed record InvoicePaymentRoute
{
    /// <summary>Creates a route without implying support for another country or rail.</summary>
    public InvoicePaymentRoute(string payerCountryCode, string railCode)
    {
        if (string.IsNullOrWhiteSpace(payerCountryCode) || payerCountryCode.Trim().Length != 2 || !payerCountryCode.Trim().All(char.IsLetter))
        {
            throw new ArgumentException("Код страны плательщика должен быть двухбуквенным ISO 3166-1 alpha-2 значением.", nameof(payerCountryCode));
        }

        if (string.IsNullOrWhiteSpace(railCode))
        {
            throw new ArgumentException("Код платёжного rail обязателен и не может быть fallback-значением.", nameof(railCode));
        }

        PayerCountryCode = payerCountryCode.Trim().ToUpperInvariant();
        RailCode = railCode.Trim();
    }

    /// <summary>Gets the explicit ISO 3166-1 alpha-2 payer country.</summary>
    public string PayerCountryCode { get; }

    /// <summary>Gets the versioned application rail identifier.</summary>
    public string RailCode { get; }
}

/// <summary>DDD aggregate owning an auditable order and its payment-launch eligibility.</summary>
public sealed class Invoice
{
    /// <summary>Creates an issued invoice with a strictly future expiration instant.</summary>
    public Invoice(
        Guid id,
        string orderReference,
        Money amount,
        InvoicePaymentRoute route,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        string issueIdempotencyKey)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Идентификатор invoice обязателен.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(orderReference))
        {
            throw new ArgumentException("Order reference обязателен.", nameof(orderReference));
        }

        if (orderReference.Trim().Length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(orderReference), "Order reference слишком длинный.");
        }

        ArgumentNullException.ThrowIfNull(route);
        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException("Invoice должен истекать позже момента выпуска.", nameof(expiresAtUtc));
        }

        Id = id;
        OrderReference = orderReference.Trim();
        Amount = amount;
        Route = route;
        IssuedAtUtc = issuedAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        IssueIdempotencyKey = RequireKey(issueIdempotencyKey, nameof(issueIdempotencyKey));
        Status = InvoiceStatus.Issued;
    }

    /// <summary>Gets the stable invoice identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the merchant-owned order reference used for reconciliation.</summary>
    public string OrderReference { get; }

    /// <summary>Gets the gross amount requested from the payer.</summary>
    public Money Amount { get; }

    /// <summary>Gets the explicit payer country and rail route.</summary>
    public InvoicePaymentRoute Route { get; }

    /// <summary>Gets the UTC issue instant.</summary>
    public DateTimeOffset IssuedAtUtc { get; }

    /// <summary>Gets the immutable UTC expiration instant.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the caller idempotency key used to issue this invoice.</summary>
    public string IssueIdempotencyKey { get; }

    /// <summary>Gets the current invoice lifecycle state.</summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>Gets the cancellation instant, when the invoice was cancelled.</summary>
    public DateTimeOffset? CancelledAtUtc { get; private set; }

    /// <summary>Gets the provider session created from this invoice, if any.</summary>
    public string? LastProviderPaymentId { get; private set; }

    /// <summary>Gets the launch idempotency key associated with the latest provider session.</summary>
    public string? LastLaunchIdempotencyKey { get; private set; }

    /// <summary>Returns whether a launch may be created at the supplied instant.</summary>
    public bool IsLaunchableAt(DateTimeOffset nowUtc) =>
        Status == InvoiceStatus.Issued && nowUtc.ToUniversalTime() < ExpiresAtUtc;

    /// <summary>Cancels an issued invoice before its expiration.</summary>
    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureStatus(InvoiceStatus.Issued);
        if (cancelledAtUtc.ToUniversalTime() >= ExpiresAtUtc)
        {
            throw new InvalidOperationException("Истёкший invoice нельзя отменить как активный; сначала требуется expiration transition.");
        }

        CancelledAtUtc = cancelledAtUtc.ToUniversalTime();
        Status = InvoiceStatus.Cancelled;
    }

    /// <summary>Expires an issued invoice at or after its expiration instant.</summary>
    public void Expire(DateTimeOffset expiredAtUtc)
    {
        EnsureStatus(InvoiceStatus.Issued);
        if (expiredAtUtc.ToUniversalTime() < ExpiresAtUtc)
        {
            throw new InvalidOperationException("Invoice нельзя перевести в Expired до наступления ExpiresAtUtc.");
        }

        Status = InvoiceStatus.Expired;
    }

    /// <summary>Records provider launch evidence without treating it as payment confirmation.</summary>
    public void RecordLaunch(string providerPaymentId, string launchIdempotencyKey)
    {
        EnsureStatus(InvoiceStatus.Issued);
        LastProviderPaymentId = RequireKey(providerPaymentId, nameof(providerPaymentId));
        LastLaunchIdempotencyKey = RequireKey(launchIdempotencyKey, nameof(launchIdempotencyKey));
    }

    private void EnsureStatus(InvoiceStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Операция допустима только для invoice в состоянии {expected}; текущее состояние: {Status}.");
        }
    }

    private static string RequireKey(string value, string parameterName) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Идентификатор/idempotency key обязателен.", parameterName)
        : value.Trim();
}
