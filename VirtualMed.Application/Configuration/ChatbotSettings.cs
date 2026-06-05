namespace VirtualMed.Application.Configuration;

public class ChatbotSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 60;
    public int IngestTimeoutSeconds { get; set; } = 180;
    public int SimilarityTopK { get; set; } = 5;
    public string InternalApiKey { get; set; } = string.Empty;
}
