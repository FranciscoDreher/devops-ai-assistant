using DevOpsAI.Application;
using DevOpsAI.Domain;

namespace DevOpsAI.Api.Endpoints;

public static class IncidentEndpoints
{
    public static void MapIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/incidents");

        group.MapPost("/polish", async Task<IResult> (
            IncidentPolishRequestDto request,
            IIncidentPolisher polisher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (request is null)
            {
                return Results.Json(
                    RequestObservabilityMiddleware.CreateProblemDetails("INVALID_REQUEST", "Request body is required.", StatusCodes.Status400BadRequest, context),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.IncidentNote))
            {
                return Results.Json(
                    RequestObservabilityMiddleware.CreateProblemDetails("INVALID_INCIDENT_NOTE", "IncidentNote is required.", StatusCodes.Status400BadRequest, context),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!TryParseAudience(request.Audience, out var audience))
            {
                return Results.Json(
                    RequestObservabilityMiddleware.CreateProblemDetails("INVALID_AUDIENCE", "Audience is unsupported.", StatusCodes.Status400BadRequest, context),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await polisher.PolishAsync(
                new PolishIncidentRequest(request.IncidentNote.Trim(), audience),
                cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var statusCode = IsValidationError(result.Error!.Code)
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;

                return Results.Json(
                    RequestObservabilityMiddleware.CreateProblemDetails(result.Error.Code, result.Error.Message, statusCode, context),
                    statusCode: statusCode);
            }

            var incident = result.Incident!;
            return Results.Ok(new IncidentPolishResponse(
                incident.Summary,
                incident.Impact,
                incident.ActionsTaken.ToArray(),
                incident.CurrentStatus,
                incident.NextSteps.ToArray()));
        })
        .Produces<IncidentPolishResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithName("PolishIncident");
    }

    private static bool TryParseAudience(string? audience, out IncidentAudience parsedAudience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            parsedAudience = default;
            return false;
        }

        if (Enum.TryParse<IncidentAudience>(audience, true, out var parsed) &&
            Enum.IsDefined(parsed) &&
            string.Equals(Enum.GetName(parsed), audience, StringComparison.OrdinalIgnoreCase))
        {
            parsedAudience = parsed;
            return true;
        }

        parsedAudience = default;
        return false;
    }

    private static bool IsValidationError(string code)
    {
        return code is "INVALID_INCIDENT_NOTE" or "INVALID_AUDIENCE";
    }

    public sealed record IncidentPolishRequestDto
    {
        public string? IncidentNote { get; init; }

        public string? Audience { get; init; }
    }

    public sealed record IncidentPolishResponse(
        string Summary,
        string Impact,
        IReadOnlyList<string> ActionsTaken,
        string CurrentStatus,
        IReadOnlyList<string> NextSteps);
}
