using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Authorization;
using VirtualMed.Application.Commands.RagDocuments;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Queries.RagDocuments;
using VirtualMed.Application.RagDocuments;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/admin/rag/documents")]
[Authorize]
[RequestSizeLimit(30 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024, ValueLengthLimit = 30 * 1024 * 1024)]
public class AdminRagDocumentsController : ControllerBase
{
    private const int MaxUploadBytes = 20 * 1024 * 1024;

    private readonly IMediator _mediator;
    private readonly IRagUploadSession _uploadSession;
    private readonly IRequestHandler<UploadRagDocumentCommand, UploadRagDocumentResultDto> _uploadHandler;
    private readonly ILogger<AdminRagDocumentsController> _logger;

    public AdminRagDocumentsController(
        IMediator mediator,
        IRagUploadSession uploadSession,
        IRequestHandler<UploadRagDocumentCommand, UploadRagDocumentResultDto> uploadHandler,
        ILogger<AdminRagDocumentsController> logger)
    {
        _mediator = mediator;
        _uploadSession = uploadSession;
        _uploadHandler = uploadHandler;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("RagDocument", "Read")]
    public async Task<IActionResult> List()
    {
        var result = await _mediator.Send(new ListRagDocumentsQuery());
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("RagDocument", "Upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Upload RAG: inicio de accion");

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Debe enviar un archivo PDF." });

        if (file.Length > MaxUploadBytes)
            return BadRequest(new { message = "El archivo supera el maximo de 20 MB." });

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Solo se permiten archivos PDF." });

        _logger.LogInformation(
            "Recibiendo upload RAG: {FileName} ({SizeBytes} bytes)",
            file.FileName,
            file.Length);

        var tempPath = Path.Combine(Path.GetTempPath(), $"virtualmed-rag-{Guid.NewGuid():N}.pdf");

        try
        {
            await using (var output = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(output, cancellationToken);
            }

            var writtenSize = new FileInfo(tempPath).Length;
            if (writtenSize != file.Length)
            {
                return BadRequest(new { message = "No se pudo leer el archivo completo." });
            }

            _logger.LogInformation("Archivo RAG guardado en disco temporal, iniciando handler: {FileName}", file.FileName);

            _uploadSession.SetTempFile(tempPath, file.Length);

            var result = await _uploadHandler.Handle(
                new UploadRagDocumentCommand(file.FileName, file.Length),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Archivo RAG rechazado por limite de multipart: {FileName}", file.FileName);
            return BadRequest(new { message = "El archivo supera el limite permitido de subida (20 MB)." });
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error en upload RAG: {FileName}", file.FileName);
            return StatusCode(500, new { message = "Error interno al subir el documento. Revise los logs del servidor." });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                try { System.IO.File.Delete(tempPath); } catch { /* best effort */ }
            }
        }
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("RagDocument", "Delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRagDocumentCommand(id), cancellationToken);
        return NoContent();
    }
}
