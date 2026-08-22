using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ricis.Finance.Application;
using Ricis.Finance.Domain;

namespace Ricis.Finance.Bepaid;

/// <summary>Configuration owned by the host application for documented bePaid API access.</summary>
public sealed record BepaidOptions
{
    /// <summary>Creates validated bePaid credentials and an optional E-POS service selector.</summary>
    public BepaidOptions(string shopId, string secretKey, int? eripServiceNumber = null, Uri? apiBaseUri = null)
    {
        if (string.IsNullOrWhiteSpace(shopId))
        {
            throw new ArgumentException("bePaid Shop ID обязателен.", nameof(shopId));
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new ArgumentException("bePaid Secret Key обязателен.", nameof(secretKey));
        }

        if (eripServiceNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eripServiceNumber), "Номер услуги ЕРИП должен быть положительным.");
        }

        var effectiveApiBaseUri = apiBaseUri ?? new Uri("https://api.bepaid.by/", UriKind.Absolute);
        if (!effectiveApiBaseUri.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(effectiveApiBaseUri.Scheme, Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Адрес bePaid API должен быть абсолютным HTTPS URI.", nameof(apiBaseUri));
        }

        ShopId = shopId.Trim();
        SecretKey = secretKey.Trim();
        EripServiceNumber = eripServiceNumber;
        ApiBaseUri = effectiveApiBaseUri;
    }

    /// <summary>Gets the merchant shop identifier.</summary>
    public string ShopId { get; }

    /// <summary>Gets the merchant secret; it must be supplied by secure host configuration and never logged.</summary>
    public string SecretKey { get; }

    /// <summary>Gets the optional service number when a merchant has multiple ERIP services.</summary>
    public int? EripServiceNumber { get; }

    /// <summary>Gets the bePaid API base URI, defaulting to the documented production origin.</summary>
    public Uri ApiBaseUri { get; }
}

/// <summary>Infrastructure adapter for the documented bePaid ERIP/E-POS payment launch contract.</summary>
public sealed class BepaidPaymentLaunchPort : IPaymentLaunchPort
{
    private static readonly IReadOnlyList<PaymentRailCapability> SupportedCapabilities =
    [
        new PaymentRailCapability("BY", PaymentRail.BelarusEripEpos, ["BYN"]),
        new PaymentRailCapability("RU", PaymentRail.RussiaSbp, ["RUB"]),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly BepaidOptions _options;

    /// <summary>Creates a bePaid adapter from a host-managed HTTP client and securely supplied provider configuration.</summary>
    public BepaidPaymentLaunchPort(HttpClient httpClient, BepaidOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PaymentRailCapability> Capabilities => SupportedCapabilities;

    /// <inheritdoc />
    public async ValueTask<PaymentLaunchSession> CreateAsync(CreatePaymentLaunch request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!SupportedCapabilities.Any(capability => capability.Supports(request.PayerCountryCode, request.Rail, request.Amount)))
        {
            throw new NotSupportedException("Этот bePaid adapter поддерживает только подтверждённые маршруты BY/BelarusEripEpos/BYN и RU/RussiaSbp/RUB.");
        }

        return request.Rail switch
        {
            PaymentRail.BelarusEripEpos => await CreateEripAsync(request, cancellationToken),
            PaymentRail.RussiaSbp => await CreateSbpAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"Неподдерживаемый payment rail {request.Rail}."),
        };
    }

    private async ValueTask<PaymentLaunchSession> CreateEripAsync(CreatePaymentLaunch request, CancellationToken cancellationToken)
    {
        var payload = new BepaidEripCreatePaymentRequest(
            new BepaidEripPaymentRequest(
                ToMinorUnits(request.Amount),
                request.Amount.Currency,
                request.Description,
                request.PayerIpAddress,
                request.ReturnUrl.AbsoluteUri,
                request.NotificationUrl.AbsoluteUri,
                request.OrderReference,
                request.ExpiresAtUtc?.ToString("O"),
                request.Test,
                new BepaidEripPaymentMethod("erip", request.OrderReference, _options.EripServiceNumber)));

        var response = await SendAsync<BepaidEripCreatePaymentRequest, BepaidEripCreatePaymentResponse>(
            "beyag/payments",
            payload,
            request.IdempotencyKey,
            cancellationToken);
        var transaction = response.Transaction ?? throw new InvalidOperationException("bePaid не вернул transaction для созданного ЕРИП счёта.");
        var erip = transaction.Erip ?? throw new InvalidOperationException("bePaid не вернул ERIP QR/deep-link данные для созданного счёта.");
        if (string.IsNullOrWhiteSpace(transaction.Uid))
        {
            throw new InvalidOperationException("bePaid не вернул UID созданного ЕРИП счёта.");
        }

        var qrPayload = DecodeQrPayload(erip.QrCodeRaw);
        var applications = (erip.Banks ?? [])
            .Select(bank => ToBankApplicationOption(bank, qrPayload))
            .Where(option => option is not null)
            .Cast<BankApplicationOption>()
            .ToArray();

        return new PaymentLaunchSession(
            providerName: "bePaid",
            providerPaymentId: transaction.Uid,
            rail: request.Rail,
            amount: request.Amount,
            handoff: null,
            qrCodeDataUri: erip.QrCode,
            bankApplications: applications,
            expiresAtUtc: ParseOptionalDateTimeOffset(transaction.ExpiredAt) ?? request.ExpiresAtUtc);
    }

    private async ValueTask<PaymentLaunchSession> CreateSbpAsync(CreatePaymentLaunch request, CancellationToken cancellationToken)
    {
        var payload = new BepaidSbpCreatePaymentRequest(
            new BepaidSbpPaymentRequest(
                ToMinorUnits(request.Amount),
                request.Amount.Currency,
                request.Description,
                request.PayerIpAddress,
                request.ReturnUrl.AbsoluteUri,
                request.NotificationUrl.AbsoluteUri,
                request.OrderReference,
                request.ExpiresAtUtc?.ToString("O"),
                request.Test,
                new BepaidSbpPaymentMethod("sbp")));
        var response = await SendAsync<BepaidSbpCreatePaymentRequest, BepaidSbpCreatePaymentResponse>(
            "beyag/transactions/payments",
            payload,
            request.IdempotencyKey,
            cancellationToken);
        var transaction = response.Transaction ?? throw new InvalidOperationException("bePaid не вернул transaction для созданной СБП payment session.");
        if (string.IsNullOrWhiteSpace(transaction.Uid))
        {
            throw new InvalidOperationException("bePaid не вернул UID созданной СБП payment session.");
        }

        var form = transaction.Form ?? throw new InvalidOperationException("bePaid не вернул form.action — provider-hosted URL для СБП QR и выбора банка.");
        var handoff = ToPaymentHandoff(form);
        return new PaymentLaunchSession(
            providerName: "bePaid",
            providerPaymentId: transaction.Uid,
            rail: request.Rail,
            amount: request.Amount,
            handoff: handoff,
            qrCodeDataUri: null,
            bankApplications: null,
            expiresAtUtc: ParseOptionalDateTimeOffset(transaction.ExpiredAt) ?? request.ExpiresAtUtc);
    }

    private static PaymentHandoff ToPaymentHandoff(BepaidRedirectForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Action) || !Uri.TryCreate(form.Action, UriKind.Absolute, out var action))
        {
            throw new InvalidOperationException("bePaid вернул некорректный form.action для customer handoff.");
        }

        var method = form.Method?.Trim().ToUpperInvariant() switch
        {
            "GET" => PaymentHandoffMethod.Get,
            "POST" => PaymentHandoffMethod.Post,
            _ => throw new InvalidOperationException("bePaid вернул неподдерживаемый метод customer handoff формы."),
        };
        var fields = (form.Fields ?? [])
            .Where(field => !string.IsNullOrWhiteSpace(field.Name) && field.Value is not null)
            .Select(field => new PaymentHandoffField(field.Name!, field.Value!))
            .ToArray();
        return new PaymentHandoff(action, method, fields);
    }

    private async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri(_options.ApiBaseUri, relativePath);
        using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ShopId}:{_options.SecretKey}")));
        message.Headers.Add("RequestID", idempotencyKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"bePaid payment launch отклонён со статусом {(int)response.StatusCode}: {ExtractProviderError(json)}", null, response.StatusCode);
        }

        return JsonSerializer.Deserialize<TResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("bePaid вернул пустой или неподдерживаемый JSON ответ.");
    }

    private static long ToMinorUnits(Money amount) => checked(decimal.ToInt64(decimal.Round(amount.Amount * 100m, 0, MidpointRounding.ToEven)));

    private static string DecodeQrPayload(string? qrCodeRaw)
    {
        if (string.IsNullOrWhiteSpace(qrCodeRaw))
        {
            throw new InvalidOperationException("bePaid не вернул qr_code_raw, необходимый для безопасного построения provider-issued bank deep links.");
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(qrCodeRaw));
        }
        catch (FormatException error)
        {
            throw new InvalidOperationException("bePaid вернул qr_code_raw в неподдерживаемом Base64 формате.", error);
        }
    }

    private static BankApplicationOption? ToBankApplicationOption(BepaidEripBank bank, string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(bank.Name) || bank.PlatformUrls is null)
        {
            return null;
        }

        var links = new Dictionary<MobilePlatform, Uri>();
        AddDeepLink(links, MobilePlatform.Ios, bank.PlatformUrls.Ios, qrPayload);
        AddDeepLink(links, MobilePlatform.Android, bank.PlatformUrls.Android, qrPayload);
        AddDeepLink(links, MobilePlatform.HuaweiApp, bank.PlatformUrls.HuaweiApp, qrPayload);
        if (links.Count == 0)
        {
            return null;
        }

        var icon = string.IsNullOrWhiteSpace(bank.Icon) ? null : $"data:image/svg+xml;base64,{bank.Icon.Trim()}";
        return new BankApplicationOption(bank.Name, icon, links);
    }

    private static void AddDeepLink(IDictionary<MobilePlatform, Uri> links, MobilePlatform platform, string? prefix, string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        var candidate = string.Concat(prefix.Trim(), qrPayload);
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var deepLink) && StringComparer.OrdinalIgnoreCase.Equals(deepLink.Scheme, Uri.UriSchemeHttps))
        {
            links.Add(platform, deepLink);
        }
    }

    private static DateTimeOffset? ParseOptionalDateTimeOffset(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();

    private static string ExtractProviderError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("message", out var message) ? message.GetString() ?? BepaidRuntimeResources.UnknownProviderError : BepaidRuntimeResources.UnknownProviderError;
        }
        catch (JsonException)
        {
            return BepaidRuntimeResources.UnknownProviderError;
        }
    }

    private sealed record BepaidSbpCreatePaymentRequest([property: JsonPropertyName("request")] BepaidSbpPaymentRequest Request);

    private sealed record BepaidSbpPaymentRequest(
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("ip")] string PayerIpAddress,
        [property: JsonPropertyName("return_url")] string ReturnUrl,
        [property: JsonPropertyName("notification_url")] string NotificationUrl,
        [property: JsonPropertyName("tracking_id")] string TrackingId,
        [property: JsonPropertyName("expired_at")] string? ExpiresAt,
        [property: JsonPropertyName("test")] bool Test,
        [property: JsonPropertyName("method")] BepaidSbpPaymentMethod Method);

    private sealed record BepaidSbpPaymentMethod([property: JsonPropertyName("type")] string Type);

    private sealed record BepaidSbpCreatePaymentResponse([property: JsonPropertyName("transaction")] BepaidSbpTransaction? Transaction);

    private sealed record BepaidSbpTransaction(
        [property: JsonPropertyName("uid")] string? Uid,
        [property: JsonPropertyName("expired_at")] string? ExpiredAt,
        [property: JsonPropertyName("form")] BepaidRedirectForm? Form);

    private sealed record BepaidRedirectForm(
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("method")] string? Method,
        [property: JsonPropertyName("fields")] IReadOnlyList<BepaidRedirectField>? Fields);

    private sealed record BepaidRedirectField(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("value")] string? Value);

    private sealed record BepaidEripCreatePaymentRequest([property: JsonPropertyName("request")] BepaidEripPaymentRequest Request);

    private sealed record BepaidEripPaymentRequest(
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("ip")] string PayerIpAddress,
        [property: JsonPropertyName("success_url")] string SuccessUrl,
        [property: JsonPropertyName("notification_url")] string NotificationUrl,
        [property: JsonPropertyName("tracking_id")] string TrackingId,
        [property: JsonPropertyName("expired_at")] string? ExpiresAt,
        [property: JsonPropertyName("test")] bool Test,
        [property: JsonPropertyName("payment_method")] BepaidEripPaymentMethod PaymentMethod);

    private sealed record BepaidEripPaymentMethod(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("account_number")] string AccountNumber,
        [property: JsonPropertyName("service_no")] int? ServiceNumber);

    private sealed record BepaidEripCreatePaymentResponse([property: JsonPropertyName("transaction")] BepaidEripTransaction? Transaction);

    private sealed record BepaidEripTransaction(
        [property: JsonPropertyName("uid")] string? Uid,
        [property: JsonPropertyName("expired_at")] string? ExpiredAt,
        [property: JsonPropertyName("erip")] BepaidEripData? Erip);

    private sealed record BepaidEripData(
        [property: JsonPropertyName("qr_code_raw")] string? QrCodeRaw,
        [property: JsonPropertyName("qr_code")] string? QrCode,
        [property: JsonPropertyName("banks")] IReadOnlyList<BepaidEripBank>? Banks);

    private sealed record BepaidEripBank(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("icon")] string? Icon,
        [property: JsonPropertyName("platform_urls")] BepaidPlatformUrls? PlatformUrls);

    private sealed record BepaidPlatformUrls(
        [property: JsonPropertyName("ios")] string? Ios,
        [property: JsonPropertyName("android")] string? Android,
        [property: JsonPropertyName("huaweiapp")] string? HuaweiApp);
}
