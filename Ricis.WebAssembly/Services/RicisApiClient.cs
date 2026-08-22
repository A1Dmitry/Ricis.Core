using System.Net.Http.Json;
using System.Text.Json;
using Ricis.WebAssembly.Models;
using Ricis.Core.Resources;

namespace Ricis.WebAssembly.Services;

/// <summary>
/// Calls the explicitly supported RICIS Web API endpoints from the WebAssembly client.
/// </summary>
public sealed class RicisApiClient
{
    /// <summary>
    /// Matches the server-side maximum expression length.
    /// </summary>
    public const int MaxExpressionLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a client using the configured RICIS API base address.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the RICIS API origin.</param>
    public RicisApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Gets the Swagger UI address for the configured RICIS API origin.
    /// </summary>
    public string SwaggerUrl => new Uri(httpClient.BaseAddress ?? throw new InvalidOperationException("RICIS API base address is missing."), "swagger/").AbsoluteUri;

    /// <summary>
    /// Processes a RICIS expression or expression system without evaluating supplied text as C#.
    /// </summary>
    /// <param name="operation">Supported server operation.</param>
    /// <param name="expression">Parser-language expression supplied by the user.</param>
    /// <param name="cancellationToken">Cancellation token for the HTTP request.</param>
    /// <returns>A normalized workspace result.</returns>
    public async Task<RicisWorkspaceResult> ProcessAsync(
        RicisOperation operation,
        string expression,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new RicisApiException(RicisLegacyTextResources.Get("runtime.legacy.4a8f65d19e0b"));
        }

        if (expression.Length > MaxExpressionLength)
        {
            throw new RicisApiException(RicisLegacyTextResources.Format("runtime.legacy.b49f0276d249", ("MaxExpressionLength", MaxExpressionLength)));
        }

        var route = operation switch
        {
            RicisOperation.Simplify => "api/expressions/simplify",
            RicisOperation.Derivative => "api/expressions/derivative",
            RicisOperation.System => "api/expressions/system",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown RICIS operation.")
        };

        using var response = await httpClient.PostAsJsonAsync(
            route,
            new RicisExpressionRequest(expression),
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new RicisApiException(await ReadErrorAsync(response, cancellationToken));
        }

        if (operation == RicisOperation.System)
        {
            var system = await response.Content.ReadFromJsonAsync<RicisExpressionSystemResponse>(JsonOptions, cancellationToken)
                ?? throw new RicisApiException(RicisLegacyTextResources.Get("runtime.legacy.10e02187ffaa"));

            return new RicisWorkspaceResult(
                operation,
                system.Source,
                string.Join(Environment.NewLine, system.Expressions),
                system.RicisExpressions,
                system.System);
        }

        var single = await response.Content.ReadFromJsonAsync<RicisExpressionResponse>(JsonOptions, cancellationToken)
            ?? throw new RicisApiException(RicisLegacyTextResources.Get("runtime.legacy.5d435ca6cf22"));

        return new RicisWorkspaceResult(
            operation,
            single.Source,
            single.Parsed,
            [single.Ricis],
            null);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<RicisApiErrorResponse>(JsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return error.Position is int position
                    ? RicisLegacyTextResources.Format("runtime.legacy.6ae6ce8d4f89", ("error.Error", error.Error), ("position", position))
                    : error.Error;
            }
        }
        catch (JsonException)
        {
            // A non-JSON reverse-proxy or infrastructure error is rendered below.
        }

        return $"RICIS Web API returned HTTP {(int)response.StatusCode}.";
    }
}

/// <summary>
/// Represents a controlled client-visible RICIS API error.
/// </summary>
public sealed class RicisApiException : Exception
{
    /// <summary>
    /// Initializes a controlled API error.
    /// </summary>
    /// <param name="message">Safe error message supplied by the client or API.</param>
    public RicisApiException(string message)
        : base(message)
    {
    }
}
