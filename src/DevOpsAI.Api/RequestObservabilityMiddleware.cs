using System.Diagnostics;

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
        using var fallbackActivity = Activity.Current is null
            ? new Activity("DevOpsAI.Api.Request").SetIdFormat(ActivityIdFormat.W3C).Start()
            : null;
        var traceId = GetTraceId(context);
        context.Items[TraceIdItemKey] = traceId;
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

        return Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
            ? activity.TraceId.ToString()
            : context.TraceIdentifier;
    }
}
