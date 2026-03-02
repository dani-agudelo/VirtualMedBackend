using FluentValidation;

namespace VirtualMed.Application.Commands.Patients;

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Document)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow.Date);

        RuleFor(x => x.Gender)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.BloodType)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Allergies)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Allergies));
    }
}