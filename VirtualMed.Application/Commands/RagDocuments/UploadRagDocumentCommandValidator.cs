using FluentValidation;

namespace VirtualMed.Application.Commands.RagDocuments;

public class UploadRagDocumentCommandValidator : AbstractValidator<UploadRagDocumentCommand>
{
    public UploadRagDocumentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Solo se permiten archivos PDF.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(20 * 1024 * 1024)
            .WithMessage("El archivo supera el maximo de 20 MB.");
    }
}
