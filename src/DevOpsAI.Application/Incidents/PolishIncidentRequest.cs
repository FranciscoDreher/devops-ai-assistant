using DevOpsAI.Domain;

namespace DevOpsAI.Application;

public sealed record PolishIncidentRequest(string IncidentNote, IncidentAudience Audience);
