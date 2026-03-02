using FluentValidation;

namespace VirtualMed.Application.Commands.Patients
{
    public class RegisterDoctorValidator
        : AbstractValidator<CreatePatientCommand>
    {
        public RegisterDoctorValidator()
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