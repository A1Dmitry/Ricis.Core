using Ricis.Core.Resources;
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
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.7fbc1d0c0ea3"), nameof(payerCountryCode));
        }

        if (string.IsNullOrWhiteSpace(railCode))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.10f4a7f2827c"), nameof(railCode));
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
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.0e59d2cd4962"), nameof(id));
        }

        if (string.IsNullOrWhiteSpace(orderReference))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.52e7c2966b0a"), nameof(orderReference));
        }

        if (orderReference.Trim().Length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(orderReference), RicisLegacyTextResources.Get("runtime.legacy.c723ed041d94"));
        }

        ArgumentNullException.ThrowIfNull(route);
        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.e9f1a008ebd2"), nameof(expiresAtUtc));
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
            throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.d525f22701f4"));
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
            throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.4081959cecd6"));
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
            throw new InvalidOperationException(RicisLegacyTextResources.Format("runtime.legacy.478bf12b4057", ("expected", expected), ("Status", Status)));
        }
    }

    private static string RequireKey(string value, string parameterName) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.df78568415c9"), parameterName)
        : value.Trim();
}
