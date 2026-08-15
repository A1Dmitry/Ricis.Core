using System.Linq.Expressions;
using Ricis.ConsoleApp;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Ricis.WebApi",
    status = "ok",
    version = "v1"
}));

app.MapPost("/api/expressions/simplify", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "simplify"));

app.MapPost("/api/expressions/derivative", (ExpressionRequest request) =>
    ProcessSingleExpression(request, "derivative"));

app.MapPost("/api/expressions/system", (ExpressionRequest request) =>
    ProcessExpressionSystem(request));

app.Run();

static IResult ProcessSingleExpression(ExpressionRequest request, string operation)
{
    if (request is null || string.IsNullOrWhiteSpace(request.Expression))
    {
        return Results.BadRequest(new ErrorResponse("Expression is required."));
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
    catch (Exception exception)
    {
        return Results.BadRequest(new ErrorResponse(exception.Message));
    }
}

static IResult ProcessExpressionSystem(ExpressionRequest request)
{
    if (request is null || string.IsNullOrWhiteSpace(request.Expression))
    {
        return Results.BadRequest(new ErrorResponse("Expression is required."));
    }

    var fragments = request.Expression
        .Split(';', StringSplitOptions.TrimEntries);

    if (fragments.Length == 0 || fragments.Any(string.IsNullOrWhiteSpace))
    {
        return Results.BadRequest(new ErrorResponse(
            "Expression system must contain non-empty lambda expressions separated by ';'."));
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
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new ErrorResponse(exception.Message));
    }
    catch (Exception exception)
    {
        return Results.BadRequest(new ErrorResponse(exception.Message));
    }
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
