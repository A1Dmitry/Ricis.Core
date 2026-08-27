using System.Linq.Expressions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Ricis.ConsoleApp;
using Ricis.Core.CachedSolutions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Proofs;
using Ricis.WebApi.Proofs;

const int MaxRequestBodyBytes = 64 * 1024;
const int MaxExpressionLength = 4096;
const int MaxSystemExpressions = 64;
const string WebAssemblyCorsPolicy = "RicisWebAssembly";

var builder = WebApplication.CreateBuilder(args);
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5066"];
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RICIS III Web API",
        Version = "v1",
        Description = "Restricted RICIS expression processing API. User input is parsed through LambdaTextParser and is never executed as arbitrary C# code."
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Ricis.WebApi.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebAssemblyCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<IProofClock, SystemProofClock>();
builder.Services.AddSingleton<IProofRunIdFactory, GuidProofRunIdFactory>();
builder.Services.AddSingleton<IProofRunSnapshotStore, InMemoryProofRunSnapshotStore>();
builder.Services.AddSingleton<IProofRunDeriver>(serviceProvider =>
    new ExpressionEquivalenceProofRunDeriver(
        ProofEndpointComposition.CreateExpressionEquivalenceProfile(
            serviceProvider.GetRequiredService<IConfiguration>())));
builder.Services.AddSingleton<ProofRunApplicationService>();
// Startup seed: confirmed CachedSolutions are available to the 3D map from the first request.
builder.Services.AddSingleton(_ => DefaultCachedSolutions.CreateIndex());
var app = builder.Build();
app.UseCors(WebAssemblyCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "RICIS III API Explorer";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RICIS III Web API v1");
    });
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Ricis.WebApi",
    status = "ok",
    version = "v1"
}))
    .WithName("GetHealth")
    .WithTags("Diagnostics")
    .WithOpenApi();

app.MapGet("/api/cached-solutions/map-bubbles", (CachedSolutionIndex index) =>
    Results.Ok(new CachedSolutionMapResponse(
        "RICIS III confirmed cached solutions",
        index.ConfirmedBubbles)))
    .WithName("GetConfirmedCachedSolutionBubbles")
    .WithTags("Cached solutions", "3D map")
    .Produces<CachedSolutionMapResponse>(StatusCodes.Status200OK)
    .WithOpenApi();
app.MapPost("/api/expressions/simplify", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "simplify"))
    .WithName("SimplifyExpression")
    .WithTags("Expressions")
    .Produces<ExpressionResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi(operation => DescribeExpressionOperation(
        operation,
        "Simplify a RICIS expression",
        "Parses a restricted lambda expression and applies the RICIS structural pipeline without executing arbitrary C# code.",
        ("linear", "x => (x + 1)"),
        ("singular", "x => ((x * 0) * (1 / x))"),
        ("about metadata", "about => about + 1")));

app.MapPost("/api/expressions/derivative", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "derivative"))
    .WithName("DifferentiateExpression")
    .WithTags("Expressions")
    .Produces<ExpressionResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi(operation => DescribeExpressionOperation(
        operation,
        "Differentiate a RICIS expression",
        "Builds a symbolic derivative and sends the derived expression through the RICIS pipeline.",
        ("polynomial", "x => (x ^ 3)"),
        ("reciprocal bridge", "x => ((x * 0) * (1 / x))"),
        ("trigonometric", "x => sin(x)")));

app.MapPost("/api/expressions/system", (ExpressionRequest request) =>
    ProcessExpressionSystem(request))
    .WithName("ProcessExpressionSystem")
    .WithTags("Expression systems")
    .Produces<ExpressionSystemResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi(operation => DescribeExpressionOperation(
        operation,
        "Process an expression system",
        "Parses semicolon-separated lambda expressions as one structural system; each expression remains independently inspectable.",
        ("two curves", "x => (x + 1); x => (x - 1)"),
        ("coordinate system", "x => (x ^ 2); x => (2 * x)"),
        ("singular system", "x => (x / 0); x => (1 / x)")));

app.MapProofEndpoints();

app.Run();

static IResult ProcessSingleExpression(ExpressionRequest request, string operation)
{
    if (!TryValidateExpression(request, out var validationError))
    {
        return Results.BadRequest(new ErrorResponse(validationError!));
    }

    try
    {
        var parser = new LambdaTextParser();
        var source = parser.Parse(request.Expression);
        var result = operation switch
        {
            "simplify" => RicisPhasePipeline.Simplify(source),
            "derivative" => source.DxDt(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported expression operation."),
        };

        return Results.Ok(new ExpressionResponse(
            request.Expression,
            operation,
            source.ToString(),
            result.ToString()));
    }
    catch (LambdaParseException exception)
    {
        return Results.BadRequest(new ParseErrorResponse(
            exception.Message,
            exception.Position));
    }
    catch (Exception)
    {
        return Results.BadRequest(new ErrorResponse("Expression processing failed."));
    }
}

static IResult ProcessExpressionSystem(ExpressionRequest request)
{
    if (!TryValidateExpression(request, out var validationError))
    {
        return Results.BadRequest(new ErrorResponse(validationError!));
    }

    var fragments = request.Expression!
        .Split(';', StringSplitOptions.TrimEntries);

    if (fragments.Length == 0 || fragments.Length > MaxSystemExpressions || fragments.Any(string.IsNullOrWhiteSpace))
    {
        return Results.BadRequest(new ErrorResponse(
            $"Expression system must contain 1 to {MaxSystemExpressions} non-empty lambda expressions separated by ';'."));
    }

    try
    {
        var parser = new LambdaTextParser();
        var expressions = fragments
            .Select(parser.Parse)
            .Cast<LambdaExpression>()
            .ToArray();
        var system = ExpressionSystem<double>.FromLambdas(expressions);
        var simplified = expressions
            .Select(expression => RicisPhasePipeline.Simplify(expression).ToString())
            .ToArray();

        return Results.Ok(new ExpressionSystemResponse(
            request.Expression,
            system.Count,
            system.ToString(),
            expressions.Select(expression => expression.ToString()).ToArray(),
            simplified));
    }
    catch (LambdaParseException exception)
    {
        return Results.BadRequest(new ParseErrorResponse(
            exception.Message,
            exception.Position));
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new ErrorResponse("Expression system validation failed."));
    }
    catch (Exception)
    {
        return Results.BadRequest(new ErrorResponse("Expression system processing failed."));
    }
}

static OpenApiOperation DescribeExpressionOperation(
    OpenApiOperation operation,
    string summary,
    string description,
    params (string Name, string Value)[] examples)
{
    operation.Summary = summary;
    operation.Description = description;

    if (operation.RequestBody?.Content.TryGetValue("application/json", out var mediaType) == true)
    {
        mediaType.Examples = examples.ToDictionary(
            example => example.Name,
            example => new OpenApiExample
            {
                Summary = example.Name,
                Description = $"Example request body for {example.Name}.",
                Value = new OpenApiObject
                {
                    ["expression"] = new OpenApiString(example.Value)
                }
            },
            StringComparer.OrdinalIgnoreCase);
    }

    return operation;
}

static bool TryValidateExpression(ExpressionRequest? request, out string? error)
{
    error = null;
    if (request is null || string.IsNullOrWhiteSpace(request.Expression))
    {
        error = "Expression is required.";
        return false;
    }

    if (request.Expression.Length > MaxExpressionLength)
    {
        error = $"Expression exceeds the maximum length of {MaxExpressionLength} characters.";
        return false;
    }

    return true;
}

/// <summary>
/// Request containing one lambda expression or a semicolon-separated expression system.
/// </summary>
/// <param name="Expression">Restricted expression text accepted by <c>LambdaTextParser</c>.</param>
public sealed record ExpressionRequest(string Expression);

/// <summary>Startup-seeded confirmed solution bubbles for the RICIS III 3D map.</summary>
public sealed record CachedSolutionMapResponse(
    string Map,
    IReadOnlyList<CachedSolutionBubble> Bubbles);

/// <summary>
/// Result returned after parsing and applying a RICIS operation.
/// </summary>
/// <param name="Source">Original user input.</param>
/// <param name="Operation">Operation name, such as <c>simplify</c> or <c>derivative</c>.</param>
/// <param name="Parsed">Parsed expression-tree representation.</param>
/// <param name="Ricis">RICIS-derived structural result.</param>
public sealed record ExpressionResponse(
    string Source,
    string Operation,
    string Parsed,
    string Ricis);

/// <summary>
/// Result returned for a semicolon-separated expression system.
/// </summary>
/// <param name="Source">Original system input.</param>
/// <param name="Count">Number of parsed expressions.</param>
/// <param name="System">Structural system representation.</param>
/// <param name="Expressions">Parsed expression-tree representations.</param>
/// <param name="RicisExpressions">RICIS result for each expression.</param>
public sealed record ExpressionSystemResponse(
    string Source,
    int Count,
    string System,
    IReadOnlyList<string> Expressions,
    IReadOnlyList<string> RicisExpressions);

/// <summary>
/// Describes a rejected request or controlled processing failure.
/// </summary>
/// <param name="Error">Safe, non-sensitive error message.</param>
public sealed record ErrorResponse(string Error);

/// <summary>
/// Describes a parser failure and its zero-based input position.
/// </summary>
/// <param name="Error">Safe parser error message.</param>
/// <param name="Position">Zero-based position reported by the parser.</param>
public sealed record ParseErrorResponse(string Error, int Position);
