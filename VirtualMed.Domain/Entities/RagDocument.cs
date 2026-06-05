using VirtualMed.Domain.Enums;

namespace VirtualMed.Domain.Entities;

public class RagDocument
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string NormalizedFileName { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public RagDocumentStatus Status { get; set; }
    public int? IndexedNodeCount { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? IndexedAt { get; set; }
}
