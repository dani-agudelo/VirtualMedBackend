namespace VirtualMed.Application.RagDocuments;

public sealed class RagDocumentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = null!;
    public string Status { get; init; } = null!;
    public long FileSizeBytes { get; init; }
    public int? IndexedNodeCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? IndexedAt { get; init; }
}

public sealed class UploadRagDocumentResultDto
{
    public RagDocumentDto Document { get; init; } = null!;
    public string Message { get; init; } = null!;
}
