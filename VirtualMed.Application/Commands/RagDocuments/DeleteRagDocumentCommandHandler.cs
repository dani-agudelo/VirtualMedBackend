using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.RagDocuments;

public class DeleteRagDocumentCommandHandler : IRequestHandler<DeleteRagDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatbotClient _chatbotClient;

    public DeleteRagDocumentCommandHandler(
        IApplicationDbContext context,
        IChatbotClient chatbotClient)
    {
        _context = context;
        _chatbotClient = chatbotClient;
    }

    public async Task Handle(DeleteRagDocumentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Set<RagDocument>()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Documento RAG", request.Id);

        if (entity.Status == Domain.Enums.RagDocumentStatus.Indexed
            || entity.Status == Domain.Enums.RagDocumentStatus.Failed)
        {
            try
            {
                var chatbotFileName = string.IsNullOrWhiteSpace(entity.StorageKey)
                    ? entity.FileName
                    : entity.StorageKey;
                await _chatbotClient.DeleteIndexedDocumentAsync(chatbotFileName, cancellationToken);
            }
            catch
            {
                // Continuar con borrado en BD aunque el chatbot no responda.
            }
        }

        _context.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
