using DevOpsAI.Api;
using DevOpsAI.Api.Endpoints;

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
            await context.Response.WriteAsJsonAsync(RequestObservabilityMiddleware.CreateProblemDetails(
                "INCIDENT_PROCESSING_FAILED",
                "The incident request could not be processed.",
                StatusCodes.Status500InternalServerError,
                context)).ConfigureAwait(false);
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

        await context.Response.WriteAsJsonAsync(RequestObservabilityMiddleware.CreateProblemDetails(
            "INCIDENT_PROCESSING_FAILED",
            "The incident request could not be processed.",
            StatusCodes.Status500InternalServerError,
            context)).ConfigureAwait(false);
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

app.Run();

public partial class Program
{
}
