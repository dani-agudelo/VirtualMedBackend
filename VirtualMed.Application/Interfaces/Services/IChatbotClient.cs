namespace VirtualMed.Application.Interfaces.Services;

public interface IChatbotClient
{
    Task<ChatbotApiResult> SendMessageAsync(
        string sessionId,
        string message,
        int similarityTopK,
        CancellationToken cancellationToken = default);

    Task<ChatbotHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);
}

public sealed class ChatbotApiResult
{
    public required string Answer { get; init; }
    public required IReadOnlyList<ChatbotSourceItem> Sources { get; init; }
}

public sealed class ChatbotSourceItem
{
    public required string FileName { get; init; }
    public required string PageLabel { get; init; }
    public double? Score { get; init; }
}

public sealed class ChatbotHealthStatus
{
    public required string Status { get; init; }
}
