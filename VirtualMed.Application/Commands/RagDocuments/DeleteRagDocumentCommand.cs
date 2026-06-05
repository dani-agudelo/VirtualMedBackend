using MediatR;

namespace VirtualMed.Application.Commands.RagDocuments;

public record DeleteRagDocumentCommand(Guid Id) : IRequest;
