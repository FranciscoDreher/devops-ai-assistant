namespace DevOpsAI.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Model { get; set; } = "gpt-4o-mini";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public static AiOptions FromEnvironment()
    {
        return new AiOptions
        {
            Model = GetEnvironmentValue("AI_MODEL", "AI__MODEL", "gpt-4o-mini"),
            Endpoint = GetEnvironmentValue("AI_ENDPOINT", "AI__ENDPOINT", string.Empty),
            ApiKey = GetEnvironmentValue("AI_API_KEY", "AI__API_KEY", string.Empty)
        };
    }

    private static string GetEnvironmentValue(string firstKey, string secondKey, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(firstKey)
            ?? Environment.GetEnvironmentVariable(secondKey);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }
}
