using System.Net.Http.Json;
using System.Text.Json;
using Ricis.WebAssembly.Models;

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
            throw new RicisApiException("Введите lambda-выражение или систему выражений.");
        }

        if (expression.Length > MaxExpressionLength)
        {
            throw new RicisApiException($"Выражение превышает лимит {MaxExpressionLength} символов.");
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
                ?? throw new RicisApiException("Web API вернул пустой ответ для системы выражений.");

            return new RicisWorkspaceResult(
                operation,
                system.Source,
                string.Join(Environment.NewLine, system.Expressions),
                system.RicisExpressions,
                system.System);
        }

        var single = await response.Content.ReadFromJsonAsync<RicisExpressionResponse>(JsonOptions, cancellationToken)
            ?? throw new RicisApiException("Web API вернул пустой ответ для выражения.");

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
                    ? $"{error.Error} Позиция: {position}."
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
