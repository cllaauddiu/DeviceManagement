namespace DeviceManagement.AI.Options;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public string GeminiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string GeminiApiVersion { get; set; } = "v1beta";
    public string GeminiModel { get; set; } = "gemini-1.5-flash";
    public string GeminiApiKey { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.4;
    public int MaxOutputTokens { get; set; } = 140;
}
