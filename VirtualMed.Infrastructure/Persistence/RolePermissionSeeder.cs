using Microsoft.EntityFrameworkCore;
using VirtualMed.Domain.Entities;
using VirtualMed.Infrastructure.Persistence;

namespace VirtualMed.Infrastructure.Persistence;

public class RolePermissionSeeder
{
    private readonly ApplicationDbContext _context;

    public RolePermissionSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Permissions.AnyAsync(cancellationToken))
            return;

        var permissions = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), Name = "Auth:2FA:Manage", Resource = "Auth", Action = "2FA:Manage", Description = "Habilitar/deshabilitar 2FA" },
            new() { Id = Guid.NewGuid(), Name = "Patient:ReadOwn", Resource = "Patient", Action = "ReadOwn", Description = "Ver propio perfil de paciente" },
            new() { Id = Guid.NewGuid(), Name = "Patient:UpdateOwn", Resource = "Patient", Action = "UpdateOwn", Description = "Actualizar propio perfil" },
            new() { Id = Guid.NewGuid(), Name = "Patient:Read", Resource = "Patient", Action = "Read", Description = "Ver pacientes (médico/admin)" },
            new() { Id = Guid.NewGuid(), Name = "Patient:Create", Resource = "Patient", Action = "Create", Description = "Registrar pacientes" },
            new() { Id = Guid.NewGuid(), Name = "Appointment:Read", Resource = "Appointment", Action = "Read", Description = "Ver citas" },
            new() { Id = Guid.NewGuid(), Name = "Appointment:Create", Resource = "Appointment", Action = "Create", Description = "Crear citas" },
            new() { Id = Guid.NewGuid(), Name = "Appointment:Update", Resource = "Appointment", Action = "Update", Description = "Actualizar citas" },
            new() { Id = Guid.NewGuid(), Name = "ClinicalEncounter:Read", Resource = "ClinicalEncounter", Action = "Read", Description = "Ver encuentros clínicos" },
            new() { Id = Guid.NewGuid(), Name = "ClinicalEncounter:Create", Resource = "ClinicalEncounter", Action = "Create", Description = "Crear encuentros" },
            new() { Id = Guid.NewGuid(), Name = "ClinicalEncounter:Update", Resource = "ClinicalEncounter", Action = "Update", Description = "Actualizar encuentros (solo administración)" },
            new() { Id = Guid.NewGuid(), Name = "Prescription:Read", Resource = "Prescription", Action = "Read", Description = "Ver recetas" },
            new() { Id = Guid.NewGuid(), Name = "Prescription:Create", Resource = "Prescription", Action = "Create", Description = "Crear recetas" },
            new() { Id = Guid.NewGuid(), Name = "VitalMetric:Read", Resource = "VitalMetric", Action = "Read", Description = "Ver métricas vitales" },
            new() { Id = Guid.NewGuid(), Name = "VitalMetric:Create", Resource = "VitalMetric", Action = "Create", Description = "Registrar métricas" },
            new() { Id = Guid.NewGuid(), Name = "Role:Read", Resource = "Role", Action = "Read", Description = "Ver roles" },
            new() { Id = Guid.NewGuid(), Name = "Role:Create", Resource = "Role", Action = "Create", Description = "Crear roles" },
            new() { Id = Guid.NewGuid(), Name = "Role:Update", Resource = "Role", Action = "Update", Description = "Actualizar roles" },
            new() { Id = Guid.NewGuid(), Name = "User:Read", Resource = "User", Action = "Read", Description = "Ver usuarios" },
            new() { Id = Guid.NewGuid(), Name = "User:ManageRoles", Resource = "User", Action = "ManageRoles", Description = "Asignar roles a usuarios" },
            new() { Id = Guid.NewGuid(), Name = "Doctor:Approve", Resource = "Doctor", Action = "Approve", Description = "Aprobar médicos" }
        };
        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(cancellationToken);

        var permDict = permissions.ToDictionary(p => $"{p.Resource}:{p.Action}");

        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            var existingRoles = await _context.Roles.Include(r => r.Permissions).ToListAsync(cancellationToken);
            foreach (var role in existingRoles)
            {
                if (role.Permissions.Count > 0) continue;
                AssignPermissionsToRole(role, role.Name, permDict);
            }
        }
        else
        {
            var patientRole = new Role { Id = Guid.NewGuid(), Name = "Patient" };
            var doctorRole = new Role { Id = Guid.NewGuid(), Name = "Doctor" };
            var specialistRole = new Role { Id = Guid.NewGuid(), Name = "Specialist" };
            var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
            var familyRole = new Role { Id = Guid.NewGuid(), Name = "FamilyMember" };

            AssignPermissionsToRole(patientRole, "Patient", permDict);
            AssignPermissionsToRole(doctorRole, "Doctor", permDict);
            AssignPermissionsToRole(specialistRole, "Specialist", permDict);
            AssignPermissionsToRole(adminRole, "Admin", permDict);
            AssignPermissionsToRole(familyRole, "FamilyMember", permDict);

            _context.Roles.AddRange(patientRole, doctorRole, specialistRole, adminRole, familyRole);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void AssignPermissionsToRole(Role role, string roleName, Dictionary<string, Permission> permDict)
    {
        var keys = GetPermissionKeysForRole(roleName);
        foreach (var key in keys)
        {
            if (permDict.TryGetValue(key, out var perm))
                role.Permissions.Add(perm);
        }
    }

    private static List<string> GetPermissionKeysForRole(string roleName)
    {
        return roleName switch
        {
            "Patient" => new List<string>
            {
                "Auth:2FA:Manage", "Patient:ReadOwn", "Patient:UpdateOwn", "Appointment:Read", "ClinicalEncounter:Read",
                "Prescription:Read", "VitalMetric:Read", "VitalMetric:Create"
            },
            "Doctor" => new List<string>
            {
                "Auth:2FA:Manage", "Patient:Read", "Patient:Create", "Appointment:Read", "Appointment:Create", "Appointment:Update",
                "ClinicalEncounter:Read", "ClinicalEncounter:Create",
                "Prescription:Read", "Prescription:Create", "VitalMetric:Read", "VitalMetric:Create"
            },
            "Specialist" => new List<string>
            {
                "Auth:2FA:Manage", "Patient:Read", "Appointment:Read", "Appointment:Create", "Appointment:Update",
                "ClinicalEncounter:Read", "ClinicalEncounter:Create",
                "Prescription:Read", "Prescription:Create", "VitalMetric:Read"
            },
            "Admin" => new List<string>
            {
                "Auth:2FA:Manage", "Patient:Read", "Patient:Create", "Appointment:Read", "Appointment:Create", "Appointment:Update",
                "ClinicalEncounter:Read", "ClinicalEncounter:Update",
                "Prescription:Read", "Prescription:Create", "VitalMetric:Read", "VitalMetric:Create",
                "Role:Read", "Role:Create", "Role:Update", "User:Read", "User:ManageRoles", "Doctor:Approve"
            },
            "FamilyMember" => new List<string>
            {
                "Auth:2FA:Manage", "Patient:ReadOwn", "Appointment:Read", "VitalMetric:Read"
            },
            _ => new List<string> { "Auth:2FA:Manage" }
        };
    }
}
