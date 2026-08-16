using System.Linq.Expressions;
using Microsoft.OpenApi.Models;
using Ricis.ConsoleApp;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

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

app.MapPost("/api/expressions/simplify", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "simplify"))
    .WithName("SimplifyExpression")
    .WithTags("Expressions")
    .Produces<ExpressionResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi();

app.MapPost("/api/expressions/derivative", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "derivative"))
    .WithName("DifferentiateExpression")
    .WithTags("Expressions")
    .Produces<ExpressionResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi();

app.MapPost("/api/expressions/system", (ExpressionRequest request) =>
    ProcessExpressionSystem(request))
    .WithName("ProcessExpressionSystem")
    .WithTags("Expression systems")
    .Produces<ExpressionSystemResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ParseErrorResponse>(StatusCodes.Status400BadRequest)
    .WithOpenApi();

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
        var simplified = RicisPhasePipeline.Simplify(source);

        return Results.Ok(new ExpressionResponse(
            request.Expression,
            operation,
            source.ToString(),
            simplified.ToString()));
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

public sealed record ExpressionRequest(string Expression);

public sealed record ExpressionResponse(
    string Source,
    string Operation,
    string Parsed,
    string Ricis);

public sealed record ExpressionSystemResponse(
    string Source,
    int Count,
    string System,
    IReadOnlyList<string> Expressions,
    IReadOnlyList<string> RicisExpressions);

public sealed record ErrorResponse(string Error);

public sealed record ParseErrorResponse(string Error, int Position);
