using DevOpsAI.Api;
using DevOpsAI.Api.Endpoints;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddIncidentApiServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(CreateErrorProblemDetails(
                "INCIDENT_PROCESSING_FAILED",
                "The incident request could not be processed.",
                StatusCodes.Status500InternalServerError,
                RequestObservabilityMiddleware.GetTraceId(context))).ConfigureAwait(false);
            return;
        }

#pragma warning disable CA1848
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DevOpsAI.Api");
        logger.LogError(
            "Unhandled exception while processing incident request. TraceId: {TraceId}",
            RequestObservabilityMiddleware.GetTraceId(context));
#pragma warning restore CA1848

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(CreateErrorProblemDetails(
            "INCIDENT_PROCESSING_FAILED",
            "The incident request could not be processed.",
            StatusCodes.Status500InternalServerError,
            RequestObservabilityMiddleware.GetTraceId(context))).ConfigureAwait(false);
    });
});

app.UseMiddleware<RequestObservabilityMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { service = "DevOpsAI.Api", status = "ready" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapIncidentEndpoints();

ProblemDetails CreateErrorProblemDetails(string code, string detail, int statusCode, string traceId)
{
    var problem = new ProblemDetails
    {
        Title = statusCode == StatusCodes.Status400BadRequest ? "Request validation failed." : "Incident processing failed.",
        Status = statusCode,
        Detail = detail,
        Type = "https://httpstatuses.com/" + statusCode
    };

    problem.Extensions["code"] = code;
    problem.Extensions["traceId"] = traceId;
    return problem;
}

app.Run();

public partial class Program
{
}
