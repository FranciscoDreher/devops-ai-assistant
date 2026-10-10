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
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Incident processing failed.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "The incident request could not be processed."
            }).ConfigureAwait(false);
            return;
        }

#pragma warning disable CA1848
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DevOpsAI.Api");
        logger.LogError(exception, "Unhandled exception while processing incident request.");
#pragma warning restore CA1848

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Incident processing failed.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The incident request could not be processed."
        }).ConfigureAwait(false);
    });
});

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
