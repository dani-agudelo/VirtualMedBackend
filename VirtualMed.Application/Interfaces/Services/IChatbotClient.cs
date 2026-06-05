namespace VirtualMed.Application.Interfaces.Services;

public interface IChatbotClient
{
    Task<ChatbotApiResult> SendMessageAsync(
        string sessionId,
        string message,
        int similarityTopK,
        CancellationToken cancellationToken = default);

    Task<ChatbotHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<ChatbotIngestResult> IngestDocumentAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatbotIndexedDocument>> ListIndexedDocumentsAsync(
        CancellationToken cancellationToken = default);

    Task DeleteIndexedDocumentAsync(string fileName, CancellationToken cancellationToken = default);
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

public sealed class ChatbotIngestResult
{
    public required string FileName { get; init; }
    public required int IndexedDocuments { get; init; }
    public required int IndexedNodes { get; init; }
}

public sealed class ChatbotIndexedDocument
{
    public required string FileName { get; init; }
    public required int IndexedNodes { get; init; }
    public required long FileSizeBytes { get; init; }
}
