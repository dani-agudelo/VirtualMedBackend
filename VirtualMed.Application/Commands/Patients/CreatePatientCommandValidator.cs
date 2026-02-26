using FluentValidation;

namespace VirtualMed.Application.Commands.Patients
{
    public class CreatePatientCommandValidator
        : AbstractValidator<CreatePatientCommand>
    {
        public CreatePatientCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Document)
                .NotEmpty()
                .MaximumLength(20);
        }
    }
}