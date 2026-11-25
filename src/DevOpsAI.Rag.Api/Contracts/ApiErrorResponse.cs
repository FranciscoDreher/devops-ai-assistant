namespace DevOpsAI.Rag.Api.Contracts;

/// <summary>Describes a safe API error without exposing internal failure details.</summary>
public sealed record ApiErrorResponse(string Code, string Message, string TraceId);
