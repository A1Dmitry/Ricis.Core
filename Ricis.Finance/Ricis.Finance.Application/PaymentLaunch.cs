using Ricis.Core.Resources;
using Ricis.Finance.Domain;

namespace Ricis.Finance.Application;

/// <summary>Identifies a payment rail whose launch and confirmation contracts were explicitly verified.</summary>
public enum PaymentRail
{
    /// <summary>Belarus ERIP/E-POS QR or payment-link flow.</summary>
    BelarusEripEpos,

    /// <summary>Russian Faster Payments System (SBP) QR or payment-link flow.</summary>
    RussiaSbp,
}

/// <summary>Declares the HTTP action required for a provider-owned customer handoff form.</summary>
public enum PaymentHandoffMethod
{
    /// <summary>The customer browser must follow the action URI with HTTP GET.</summary>
    Get,

    /// <summary>The customer browser must submit the action URI with the declared form fields.</summary>
    Post,
}

/// <summary>Identifies a mobile platform for a provider-issued bank application link.</summary>
public enum MobilePlatform
{
    /// <summary>Apple iOS.</summary>
    Ios,

    /// <summary>Google Android.</summary>
    Android,

    /// <summary>Huawei AppGallery-compatible Android distribution.</summary>
    HuaweiApp,
}

/// <summary>Declares an explicit country/rail/currency combination an injected launch adapter can serve.</summary>
public sealed record PaymentRailCapability
{
    /// <summary>Creates one explicit capability without implying support for a wider region.</summary>
    public PaymentRailCapability(string payerCountryCode, PaymentRail rail, IReadOnlyCollection<string> supportedCurrencies)
    {
        PayerCountryCode = NormalizeCountryCode(payerCountryCode, nameof(payerCountryCode));
        Rail = rail;
        if (supportedCurrencies is null || supportedCurrencies.Count == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.a7273a4875d9"), nameof(supportedCurrencies));
        }

        SupportedCurrencies = supportedCurrencies
            .Select(currency => new Money(0m, currency).Currency)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Gets the ISO 3166-1 alpha-2 country code of the payer.</summary>
    public string PayerCountryCode { get; }

    /// <summary>Gets the verified payment rail.</summary>
    public PaymentRail Rail { get; }

    /// <summary>Gets the explicit ISO 4217 currencies admitted by this provider route.</summary>
    public IReadOnlyList<string> SupportedCurrencies { get; }

    /// <summary>Returns whether this capability accepts the supplied country and amount currency.</summary>
    public bool Supports(string payerCountryCode, PaymentRail rail, Money amount) =>
        StringComparer.Ordinal.Equals(PayerCountryCode, NormalizeCountryCode(payerCountryCode, nameof(payerCountryCode))) &&
        Rail == rail &&
        SupportedCurrencies.Contains(amount.Currency, StringComparer.Ordinal);

    internal static string NormalizeCountryCode(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 2 || !value.Trim().All(char.IsLetter))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.7fbc1d0c0ea3"), parameterName);
        }

        return value.Trim().ToUpperInvariant();
    }
}

/// <summary>Represents a hidden field the provider requires the host to send during customer handoff.</summary>
public sealed record PaymentHandoffField
{
    /// <summary>Creates a non-empty form field.</summary>
    public PaymentHandoffField(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.fcc9ed08dd2a"), nameof(name));
        }

        Name = name.Trim();
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the field name.</summary>
    public string Name { get; }

    /// <summary>Gets the provider-issued field value.</summary>
    public string Value { get; }
}

/// <summary>Represents a provider-owned browser page that performs bank selection and/or app handoff.</summary>
public sealed record PaymentHandoff
{
    /// <summary>Creates a browser handoff from a provider-issued absolute action URI and form payload.</summary>
    public PaymentHandoff(Uri action, PaymentHandoffMethod method, IReadOnlyCollection<PaymentHandoffField>? fields = null)
    {
        if (action is null || !action.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(action.Scheme, Uri.UriSchemeHttps))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.4e056044a375"), nameof(action));
        }

        Action = action;
        Method = method;
        Fields = fields?.ToArray() ?? [];
    }

    /// <summary>Gets the provider-owned HTTPS endpoint for the payer browser.</summary>
    public Uri Action { get; }

    /// <summary>Gets the required browser submission method.</summary>
    public PaymentHandoffMethod Method { get; }

    /// <summary>Gets immutable provider-supplied fields for POST-style handoff.</summary>
    public IReadOnlyList<PaymentHandoffField> Fields { get; }
}

/// <summary>Represents one bank application advertised by the provider for a particular payment session.</summary>
public sealed record BankApplicationOption
{
    /// <summary>Creates a provider-advertised bank application option with platform-specific deep links.</summary>
    public BankApplicationOption(string displayName, string? iconDataUri, IReadOnlyDictionary<MobilePlatform, Uri> deepLinks)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.c7a2a84d884a"), nameof(displayName));
        }

        if (deepLinks is null || deepLinks.Count == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.286fb82111c6"), nameof(deepLinks));
        }

        if (deepLinks.Values.Any(link => link is null || !link.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(link.Scheme, Uri.UriSchemeHttps)))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.38789cb23e52"), nameof(deepLinks));
        }

        DisplayName = displayName.Trim();
        IconDataUri = string.IsNullOrWhiteSpace(iconDataUri) ? null : iconDataUri.Trim();
        DeepLinks = new Dictionary<MobilePlatform, Uri>(deepLinks);
    }

    /// <summary>Gets the bank application name supplied by the payment provider.</summary>
    public string DisplayName { get; }

    /// <summary>Gets an optional provider-supplied data URI for the bank icon.</summary>
    public string? IconDataUri { get; }

    /// <summary>Gets platform-specific provider-issued bank handoff links.</summary>
    public IReadOnlyDictionary<MobilePlatform, Uri> DeepLinks { get; }
}

/// <summary>Requests creation of a payer-authorised payment QR/link session for an explicit country and rail.</summary>
public sealed record CreatePaymentLaunch
{
    /// <summary>Creates a validated payment-launch command.</summary>
    public CreatePaymentLaunch(
        string payerCountryCode,
        PaymentRail rail,
        Money amount,
        string orderReference,
        string description,
        string payerIpAddress,
        Uri returnUrl,
        Uri notificationUrl,
        string idempotencyKey,
        DateTimeOffset? expiresAtUtc = null,
        bool test = false)
    {
        PayerCountryCode = PaymentRailCapability.NormalizeCountryCode(payerCountryCode, nameof(payerCountryCode));
        Rail = rail;
        Amount = amount;
        OrderReference = RequireText(orderReference, 30, nameof(orderReference));
        Description = RequireText(description, 1_024, nameof(description));
        PayerIpAddress = RequireText(payerIpAddress, 45, nameof(payerIpAddress));
        ReturnUrl = RequireHttpsUrl(returnUrl, nameof(returnUrl));
        NotificationUrl = RequireHttpsUrl(notificationUrl, nameof(notificationUrl));
        IdempotencyKey = RequireText(idempotencyKey, 255, nameof(idempotencyKey));
        ExpiresAtUtc = expiresAtUtc?.ToUniversalTime();
        Test = test;
    }

    /// <summary>Gets the explicit ISO payer country used for rail selection.</summary>
    public string PayerCountryCode { get; }

    /// <summary>Gets the requested verified payment rail.</summary>
    public PaymentRail Rail { get; }

    /// <summary>Gets the non-negative payment amount and explicit currency.</summary>
    public Money Amount { get; }

    /// <summary>Gets the merchant-side reference that is safe for provider reconciliation.</summary>
    public string OrderReference { get; }

    /// <summary>Gets the payer-visible payment description.</summary>
    public string Description { get; }

    /// <summary>Gets the payer IP address required by the configured provider protocol.</summary>
    public string PayerIpAddress { get; }

    /// <summary>Gets the allow-listed merchant return URI after the payer completes or leaves the handoff.</summary>
    public Uri ReturnUrl { get; }

    /// <summary>Gets the allow-listed merchant notification URI for authoritative provider status events.</summary>
    public Uri NotificationUrl { get; }

    /// <summary>Gets the caller-supplied idempotency key forwarded to a provider that supports it.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Gets the optional provider session expiry in UTC.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>Gets whether the provider request must use its documented test mode.</summary>
    public bool Test { get; }

    private static string RequireText(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximumLength)
        {
            throw new ArgumentException(RicisLegacyTextResources.Format("runtime.legacy.e57021676712", ("parameterName", parameterName), ("maximumLength", maximumLength)), parameterName);
        }

        return value.Trim();
    }

    private static Uri RequireHttpsUrl(Uri value, string parameterName)
    {
        if (value is null || !value.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(value.Scheme, Uri.UriSchemeHttps))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.d00af1f5a2c3"), parameterName);
        }

        return value;
    }
}

/// <summary>Returns a provider-created payment session without treating browser navigation as payment confirmation.</summary>
public sealed record PaymentLaunchSession
{
    /// <summary>Creates a completed provider launch response.</summary>
    public PaymentLaunchSession(
        string providerName,
        string providerPaymentId,
        PaymentRail rail,
        Money amount,
        PaymentHandoff? handoff,
        string? qrCodeDataUri,
        IReadOnlyCollection<BankApplicationOption>? bankApplications,
        DateTimeOffset? expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.329c4c2e84ab"), nameof(providerName));
        }

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.75f32b399418"), nameof(providerPaymentId));
        }

        ProviderName = providerName.Trim();
        ProviderPaymentId = providerPaymentId.Trim();
        Rail = rail;
        Amount = amount;
        Handoff = handoff;
        QrCodeDataUri = string.IsNullOrWhiteSpace(qrCodeDataUri) ? null : qrCodeDataUri.Trim();
        BankApplications = bankApplications?.ToArray() ?? [];
        if (Handoff is null && QrCodeDataUri is null && BankApplications.Count == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.ee05c93f5d65"), nameof(handoff));
        }

        ExpiresAtUtc = expiresAtUtc?.ToUniversalTime();
    }

    /// <summary>Gets the provider name for audit and downstream webhook routing.</summary>
    public string ProviderName { get; }

    /// <summary>Gets the provider transaction/session identifier; it is not a settlement event id.</summary>
    public string ProviderPaymentId { get; }

    /// <summary>Gets the rail used to create this session.</summary>
    public PaymentRail Rail { get; }

    /// <summary>Gets the payment amount declared to the provider.</summary>
    public Money Amount { get; }

    /// <summary>Gets a provider-owned browser handoff artifact when the rail supplies a hosted selector page.</summary>
    public PaymentHandoff? Handoff { get; }

    /// <summary>Gets an optional provider-returned QR image data URI for desktop checkout.</summary>
    public string? QrCodeDataUri { get; }

    /// <summary>Gets the provider-advertised bank options where the rail exposes them.</summary>
    public IReadOnlyList<BankApplicationOption> BankApplications { get; }

    /// <summary>Gets the provider-declared session expiry when available.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; }
}

/// <summary>Creates a provider payment session for one or more explicit country/rail capabilities.</summary>
public interface IPaymentLaunchPort
{
    /// <summary>Gets the provider capabilities registered by this adapter.</summary>
    IReadOnlyCollection<PaymentRailCapability> Capabilities { get; }

    /// <summary>Creates a payer-authorised session and returns the browser/deep-link handoff artifact.</summary>
    ValueTask<PaymentLaunchSession> CreateAsync(CreatePaymentLaunch request, CancellationToken cancellationToken);
}

/// <summary>Resolves injected payment-launch adapters only by their explicit capabilities.</summary>
public sealed class PaymentRailRegistry
{
    private readonly IReadOnlyDictionary<(string Country, PaymentRail Rail), IPaymentLaunchPort> _ports;
    private readonly IReadOnlyList<PaymentRailCapability> _capabilities;

    /// <summary>Registers ports and rejects ambiguous provider configuration for the same country and rail.</summary>
    public PaymentRailRegistry(IEnumerable<IPaymentLaunchPort> ports)
    {
        ArgumentNullException.ThrowIfNull(ports);
        var registered = new Dictionary<(string Country, PaymentRail Rail), IPaymentLaunchPort>();
        var capabilities = new List<PaymentRailCapability>();

        foreach (var port in ports)
        {
            ArgumentNullException.ThrowIfNull(port);
            foreach (var capability in port.Capabilities ?? throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.adb3ca595293"), nameof(ports)))
            {
                var key = (capability.PayerCountryCode, capability.Rail);
                if (!registered.TryAdd(key, port))
                {
                    throw new ArgumentException(RicisLegacyTextResources.Format("runtime.legacy.1842bfd8d58a", ("capability.PayerCountryCode", capability.PayerCountryCode), ("capability.Rail", capability.Rail)), nameof(ports));
                }

                capabilities.Add(capability);
            }
        }

        _ports = registered;
        _capabilities = capabilities
            .OrderBy(capability => capability.PayerCountryCode, StringComparer.Ordinal)
            .ThenBy(capability => capability.Rail)
            .ToArray();
    }

    /// <summary>Gets explicit capabilities for a payer country; an empty result means no default or inferred route exists.</summary>
    public IReadOnlyList<PaymentRailCapability> GetCapabilities(string payerCountryCode)
    {
        var normalizedCountry = PaymentRailCapability.NormalizeCountryCode(payerCountryCode, nameof(payerCountryCode));
        return _capabilities.Where(capability => StringComparer.Ordinal.Equals(capability.PayerCountryCode, normalizedCountry)).ToArray();
    }

    /// <summary>Resolves a provider only when country, rail and currency are all explicitly supported.</summary>
    public IPaymentLaunchPort Resolve(CreatePaymentLaunch request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_ports.TryGetValue((request.PayerCountryCode, request.Rail), out var port) ||
            !port.Capabilities.Any(capability => capability.Supports(request.PayerCountryCode, request.Rail, request.Amount)))
        {
            throw new NotSupportedException(RicisLegacyTextResources.Format("runtime.legacy.f14ca42ca77b", ("request.PayerCountryCode", request.PayerCountryCode), ("request.Rail", request.Rail), ("request.Amount.Currency", request.Amount.Currency)));
        }

        return port;
    }
}

/// <summary>Application service that creates a payment launch without promoting a browser return to a payment fact.</summary>
public sealed class CreatePaymentLaunchService
{
    private readonly PaymentRailRegistry _registry;

    /// <summary>Creates the service from injected, explicitly capable provider adapters.</summary>
    public CreatePaymentLaunchService(PaymentRailRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>Creates a provider session for a validated request.</summary>
    public ValueTask<PaymentLaunchSession> HandleAsync(CreatePaymentLaunch request, CancellationToken cancellationToken) =>
        _registry.Resolve(request).CreateAsync(request, cancellationToken);
}
