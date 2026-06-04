using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Configuration;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Application.Exceptions;

namespace VirtualMed.Application.Commands.Doctors
{
    public class RegisterDoctorCommandHandler
    : IRequestHandler<RegisterDoctorCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMinioService _minioService;
        private readonly INotificationService _notification;
        private readonly IEmailTokenService _emailTokenService;
        private readonly EmailSettings _emailSettings;

        public RegisterDoctorCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IMinioService minioService,
            INotificationService notification,
            IEmailTokenService emailTokenService,
            IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _minioService = minioService;
            _notification = notification;
            _emailTokenService = emailTokenService;
            _emailSettings = emailSettings.Value;
        }

        public async Task<Guid> Handle(RegisterDoctorCommand request, CancellationToken cancellationToken)
        {
            // validar licencia única
            var exists = await _context.Set<Doctor>()
                .AnyAsync(d => d.ProfessionalLicense == request.ProfessionalLicense, cancellationToken);

            if (exists)
                throw new DuplicateEntityException("Doctor", "Tarjeta Profesional", request.ProfessionalLicense);

            // crear usuario
            var roleId = await _context.Set<Role>()
                .Where(r => r.Name == "Doctor")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleId == Guid.Empty)
                throw new NotFoundException("Rol 'Doctor' no encontrado en el sistema.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Status = "PendingApproval",
                CreatedAt = DateTime.UtcNow,
                RoleId = roleId
            };

            _context.Add(user);

            // crear doctor
            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ProfessionalLicense = request.ProfessionalLicense,
                Verified = false
            };

            _context.Add(doctor);

            // guardar documento en MinIO (si existe)
            if (request.SupportingDocument != null)
            {
                using var stream = request.SupportingDocument.OpenReadStream();

                await _minioService.UploadAsync(
                    "doctor-documents",
                    $"{doctor.Id}/{request.SupportingDocument.FileName}",
                    stream,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var rawToken = await _emailTokenService.CreateTokenAsync(
                user.Id,
                UserEmailTokenType.EmailVerification,
                TimeSpan.FromHours(_emailSettings.EmailVerificationHours),
                cancellationToken);

            await _notification.SendEmailVerificationAsync(user, rawToken, cancellationToken);

            await _notification.NotifyAdminAsync(
                $"Nuevo médico pendiente de aprobación: {user.Email}",
                cancellationToken);

            return doctor.Id;
        }
    }
}
