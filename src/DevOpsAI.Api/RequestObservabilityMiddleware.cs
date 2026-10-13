using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsAI.Api;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    private static readonly object TraceIdItemKey = new();

    private static readonly Action<ILogger, string, string, int, double, string, Exception?> RequestCompleted =
        LoggerMessage.Define<string, string, int, double, string>(
            LogLevel.Information,
            new EventId(1, nameof(RequestCompleted)),
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. TraceId: {TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = GetTraceId(context);
        context.Items[TraceIdItemKey] = traceId;

        using var fallbackActivity = Activity.Current is null
            ? CreateRequestActivity(context)
            : null;

        if (fallbackActivity is not null)
        {
            traceId = GetTraceId(context);
            context.Items[TraceIdItemKey] = traceId;
        }

        var startedTimestamp = Stopwatch.GetTimestamp();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
            return Task.CompletedTask;
        });

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            RequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds,
                traceId,
                null);
        }
    }

    public static string GetTraceId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(TraceIdItemKey, out var traceId) && traceId is string requestTraceId)
        {
            return requestTraceId;
        }

        if (context.Request.Headers.TryGetValue("traceparent", out var traceParentHeader) &&
            TryGetTraceIdFromTraceParent(traceParentHeader.ToString(), out var traceIdFromParent))
        {
            return traceIdFromParent;
        }

        if (context.Request.Headers.TryGetValue("X-Trace-Id", out var configuredTraceId) &&
            !string.IsNullOrWhiteSpace(configuredTraceId))
        {
            return configuredTraceId.ToString();
        }

        return Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
            ? activity.TraceId.ToString()
            : context.TraceIdentifier;
    }

    public static ProblemDetails CreateProblemDetails(string code, string message, int statusCode, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = new ProblemDetails
        {
            Title = statusCode == StatusCodes.Status400BadRequest ? "Request validation failed." : "Incident processing failed.",
            Status = statusCode,
            Detail = message,
            Type = "https://httpstatuses.com/" + statusCode
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = GetTraceId(context);
        return problem;
    }

    private static Activity CreateRequestActivity(HttpContext context)
    {
        var activity = new Activity("DevOpsAI.Api.Request").SetIdFormat(ActivityIdFormat.W3C);

        if (context.Request.Headers.TryGetValue("traceparent", out var traceParentHeader))
        {
            var incomingTraceParent = traceParentHeader.ToString();
            if (!string.IsNullOrWhiteSpace(incomingTraceParent) &&
                TryGetTraceIdFromTraceParent(incomingTraceParent, out _))
            {
                activity.SetParentId(incomingTraceParent);
            }
        }

        return activity.Start();
    }

    private static bool TryGetTraceIdFromTraceParent(string? traceParent, out string traceId)
    {
        traceId = string.Empty;

        if (string.IsNullOrWhiteSpace(traceParent))
        {
            return false;
        }

        var parts = traceParent.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 4 && parts[0].Length == 2 && parts[1].Length == 32)
        {
            traceId = parts[1];
            return true;
        }

        return false;
    }
}
