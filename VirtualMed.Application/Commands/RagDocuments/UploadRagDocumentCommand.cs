using MediatR;
using VirtualMed.Application.RagDocuments;

namespace VirtualMed.Application.Commands.RagDocuments;

public record UploadRagDocumentCommand(
    string FileName,
    long FileSizeBytes) : IRequest<UploadRagDocumentResultDto>;
