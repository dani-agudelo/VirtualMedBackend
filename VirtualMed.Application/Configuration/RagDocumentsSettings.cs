namespace VirtualMed.Application.Configuration;

public class RagDocumentsSettings
{
    public string BucketName { get; set; } = "rag-documents";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}
