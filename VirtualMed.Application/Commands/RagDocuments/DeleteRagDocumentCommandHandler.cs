using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.RagDocuments;

public class DeleteRagDocumentCommandHandler : IRequestHandler<DeleteRagDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly IChatbotClient _chatbotClient;
    private readonly RagDocumentsSettings _ragSettings;

    public DeleteRagDocumentCommandHandler(
        IApplicationDbContext context,
        IMinioService minioService,
        IChatbotClient chatbotClient,
        IOptions<RagDocumentsSettings> ragSettings)
    {
        _context = context;
        _minioService = minioService;
        _chatbotClient = chatbotClient;
        _ragSettings = ragSettings.Value;
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
                await _chatbotClient.DeleteIndexedDocumentAsync(entity.FileName, cancellationToken);
            }
            catch
            {
                // Continuar con borrado en MinIO/BD aunque el chatbot no responda.
            }
        }

        try
        {
            await _minioService.DeleteAsync(_ragSettings.BucketName, entity.StorageKey, cancellationToken);
        }
        catch
        {
            // Continuar eliminando metadata aunque MinIO falle.
        }

        _context.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
