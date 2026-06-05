using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Application.RagDocuments;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.RagDocuments;

public class UploadRagDocumentCommandHandler : IRequestHandler<UploadRagDocumentCommand, UploadRagDocumentResultDto>
{
    private static readonly SemaphoreSlim IngestSemaphore = new(1, 1);

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IRagUploadSession _uploadSession;
    private readonly IChatbotClient _chatbotClient;
    private readonly RagDocumentsSettings _ragSettings;
    private readonly ILogger<UploadRagDocumentCommandHandler> _logger;

    public UploadRagDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IRagUploadSession uploadSession,
        IChatbotClient chatbotClient,
        IOptions<RagDocumentsSettings> ragSettings,
        ILogger<UploadRagDocumentCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _uploadSession = uploadSession;
        _chatbotClient = chatbotClient;
        _ragSettings = ragSettings.Value;
        _logger = logger;
    }

    public async Task<UploadRagDocumentResultDto> Handle(
        UploadRagDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var (tempPath, sizeBytes) = _uploadSession.TakeTempFile();

        try
        {
            return await HandleInternalAsync(request, tempPath, sizeBytes, cancellationToken);
        }
        finally
        {
            TryDeleteTempFile(tempPath);
        }
    }

    private async Task<UploadRagDocumentResultDto> HandleInternalAsync(
        UploadRagDocumentCommand request,
        string tempPath,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedAccessException("Usuario autenticado no encontrado.");

        if (sizeBytes == 0)
            throw new BusinessRuleException("RAG_EMPTY_FILE", "El archivo PDF está vacío.");

        if (request.FileSizeBytes > _ragSettings.MaxFileSizeBytes)
            throw new BusinessRuleException(
                "RAG_FILE_TOO_LARGE",
                $"El archivo supera el maximo de {_ragSettings.MaxFileSizeBytes / (1024 * 1024)} MB.");

        var normalizedFileName = Path.GetFileName(request.FileName).Trim().ToLowerInvariant();
        var displayFileName = Path.GetFileName(request.FileName);

        _logger.LogInformation(
            "Iniciando subida RAG: {FileName} ({SizeBytes} bytes)",
            displayFileName,
            sizeBytes);

        _logger.LogInformation("Verificando duplicado RAG: {FileName}", displayFileName);
        var duplicateExists = await _context.RagDocumentExistsByNormalizedNameAsync(
            normalizedFileName,
            cancellationToken);
        _logger.LogInformation("Resultado duplicado RAG: {FileName} => {Exists}", displayFileName, duplicateExists);

        if (duplicateExists)
            throw new BusinessRuleException(
                "RAG_DUPLICATE",
                $"Ya existe un documento con el nombre '{displayFileName}'.");

        var documentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entity = new RagDocument
        {
            Id = documentId,
            FileName = displayFileName,
            NormalizedFileName = normalizedFileName,
            StorageKey = displayFileName,
            FileSizeBytes = request.FileSizeBytes,
            Status = RagDocumentStatus.Pending,
            UploadedByUserId = userId,
            CreatedAt = now
        };

        _context.Add(entity);
        _logger.LogInformation("Guardando registro RAG en base de datos: {DocumentId}", documentId);
        await _context.SaveChangesAsync(cancellationToken);

        await IngestSemaphore.WaitAsync(cancellationToken);
        try
        {
            entity.Status = RagDocumentStatus.Ingesting;
            await _context.SaveChangesAsync(cancellationToken);

            await using var ingestStream = File.OpenRead(tempPath);

            _logger.LogInformation(
                "Enviando PDF al chatbot (data/ + indexacion): {FileName}",
                displayFileName);

            var ingestResult = await _chatbotClient.IngestDocumentAsync(
                ingestStream,
                displayFileName,
                cancellationToken);

            entity.Status = RagDocumentStatus.Indexed;
            entity.StorageKey = ingestResult.FileName;
            entity.IndexedNodeCount = ingestResult.IndexedNodes;
            entity.IndexedAt = DateTime.UtcNow;
            entity.ErrorMessage = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Documento RAG indexado: {FileName} ({IndexedNodes} nodos)",
                displayFileName,
                ingestResult.IndexedNodes);

            return new UploadRagDocumentResultDto
            {
                Document = MapToDto(entity),
                Message = "Documento indexado correctamente."
            };
        }
        catch (BusinessRuleException ex) when (ex.ErrorCode == "RAG_DUPLICATE")
        {
            await MarkFailedAndCleanupAsync(entity, ex.Message, null, cancellationToken);
            throw;
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(ex, "Fallo la indexacion RAG para {FileName}", displayFileName);
            await MarkFailedAndCleanupAsync(entity, ex.Message, null, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo la indexacion RAG para {FileName}", displayFileName);
            await MarkFailedAndCleanupAsync(entity, ex.Message, null, cancellationToken);
            throw new ExternalServiceException(
                "No fue posible indexar el documento en el asistente clínico.",
                ex);
        }
        finally
        {
            IngestSemaphore.Release();
        }
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort.
        }
    }

    private async Task MarkFailedAndCleanupAsync(
        RagDocument entity,
        string errorMessage,
        string? chatbotFileName,
        CancellationToken cancellationToken)
    {
        entity.Status = RagDocumentStatus.Failed;
        entity.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        await _context.SaveChangesAsync(cancellationToken);

        var nameToDelete = chatbotFileName ?? entity.StorageKey ?? entity.FileName;
        try
        {
            await _chatbotClient.DeleteIndexedDocumentAsync(nameToDelete, cancellationToken);
        }
        catch
        {
            // Best effort: evita dejar el PDF en data/ si fallo la indexacion.
        }
    }

    private static RagDocumentDto MapToDto(RagDocument entity) => new()
    {
        Id = entity.Id,
        FileName = entity.FileName,
        Status = entity.Status.ToString(),
        FileSizeBytes = entity.FileSizeBytes,
        IndexedNodeCount = entity.IndexedNodeCount,
        ErrorMessage = entity.ErrorMessage,
        CreatedAt = entity.CreatedAt,
        IndexedAt = entity.IndexedAt
    };
}
