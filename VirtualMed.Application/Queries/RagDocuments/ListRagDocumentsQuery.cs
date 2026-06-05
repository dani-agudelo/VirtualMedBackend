using MediatR;
using VirtualMed.Application.RagDocuments;

namespace VirtualMed.Application.Queries.RagDocuments;

public record ListRagDocumentsQuery : IRequest<IReadOnlyList<RagDocumentDto>>;
