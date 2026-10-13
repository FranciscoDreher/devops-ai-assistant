using System.Globalization;

namespace DevOpsAI.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 86_400;
    public const int DefaultTimeoutSeconds = 30;

    public string Model { get; set; } = "gpt-4o-mini";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    public static bool IsValidTimeout(int timeoutSeconds)
    {
        return timeoutSeconds is >= MinTimeoutSeconds and <= MaxTimeoutSeconds;
    }

    public static AiOptions FromEnvironment()
    {
        return new AiOptions
        {
            Model = GetEnvironmentValue("AI_MODEL", "AI__MODEL", "gpt-4o-mini"),
            Endpoint = GetEnvironmentValue("AI_ENDPOINT", "AI__ENDPOINT", string.Empty),
            ApiKey = GetEnvironmentValue("AI_API_KEY", "AI__API_KEY", string.Empty),
            TimeoutSeconds = GetTimeoutFromEnvironment()
        };
    }

    private static int GetTimeoutFromEnvironment()
    {
        var value = GetEnvironmentValue("AI_TIMEOUT_SECONDS", "AI__TIMEOUT_SECONDS", DefaultTimeoutSeconds.ToString(CultureInfo.InvariantCulture));

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutSeconds) &&
            IsValidTimeout(timeoutSeconds))
        {
            return timeoutSeconds;
        }

        return DefaultTimeoutSeconds;
    }

    private static string GetEnvironmentValue(string firstKey, string secondKey, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(firstKey)
            ?? Environment.GetEnvironmentVariable(secondKey);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }
}
