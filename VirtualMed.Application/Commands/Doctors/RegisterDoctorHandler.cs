using MediatR;
using VirtualMed.Domain.Entities;
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

        public RegisterDoctorCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IMinioService minioService,
            INotificationService notification)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _minioService = minioService;
            _notification = notification;
        }

        public async Task<Guid> Handle(RegisterDoctorCommand request, CancellationToken cancellationToken)
        {
            // validar licencia única
            var exists = _context.Set<Doctor>()
                .Any(d => d.ProfessionalLicense == request.ProfessionalLicense);

            if (exists)
                throw new DuplicateEntityException("Doctor", "Tarjeta Profesional", request.ProfessionalLicense);

            // crear usuario
            var roleId = _context.Set<Role>()
                .Where(r => r.Name == "Doctor")
                .Select(r => r.Id)
                .FirstOrDefault();

            if (roleId == Guid.Empty)
                throw new NotFoundException("Rol 'Doctor' no encontrado en el sistema.");

            var user = new User
            {
                Id = Guid.NewGuid(),
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

            // notificar administrador
            await _notification.NotifyAdminAsync(
                $"Nuevo médico pendiente de aprobación: {user.Email}");

            return doctor.Id;
        }
    }
}
