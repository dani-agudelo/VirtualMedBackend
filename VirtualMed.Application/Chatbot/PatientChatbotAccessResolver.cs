using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Chatbot;

public static class PatientChatbotAccessResolver
{
    public static async Task<Guid> ResolveSelfPatientIdAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
                     ?? throw new UnauthorizedAccessException("Usuario autenticado no encontrado.");

        var role = currentUser.Role ?? string.Empty;
        if (!string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Solo los pacientes pueden usar el asistente clínico.");

        var patientId = await context.Set<Patient>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!patientId.HasValue)
            throw new NotFoundException("Paciente", "perfil");

        return patientId.Value;
    }
}
