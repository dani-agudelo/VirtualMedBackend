using FluentValidation;

namespace VirtualMed.Application.Commands.Doctors
{
    public class RegisterDoctorValidator : AbstractValidator<RegisterDoctorCommand>
    {
        public RegisterDoctorValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .MinimumLength(6);

            RuleFor(x => x.ProfessionalLicense)
                .NotEmpty()
                .Matches(@"^[A-Z0-9\-]+$")
                .WithMessage("Formato de tarjeta profesional inválido");
        }
    }
}