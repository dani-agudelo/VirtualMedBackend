using Microsoft.EntityFrameworkCore;
using VirtualMed.Domain.Entities;
using VirtualMed.Application.Interfaces;
using System.Data;

namespace VirtualMed.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TwoFactorAuth> TwoFactorAuths => Set<TwoFactorAuth>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ClinicalEncounter> ClinicalEncounters => Set<ClinicalEncounter>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PrescriptionMedication> PrescriptionMedications => Set<PrescriptionMedication>();
    public DbSet<VideoSession> VideoSessions => Set<VideoSession>();
    public DbSet<VideoChatMessage> VideoChatMessages => Set<VideoChatMessage>();
    public DbSet<VitalSignReading> VitalSignReadings => Set<VitalSignReading>();
    public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
    public DbSet<HealthAlert> HealthAlerts => Set<HealthAlert>();
    public DbSet<RiskScore> RiskScores => Set<RiskScore>();
    public DbSet<UserEmailToken> UserEmailTokens => Set<UserEmailToken>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<RagDocument> RagDocuments => Set<RagDocument>();

    IQueryable<T> IApplicationDbContext.Set<T>() => Set<T>();

    void IApplicationDbContext.Add<T>(T entity) => Set<T>().Add(entity);

    void IApplicationDbContext.Update<T>(T entity) => Set<T>().Update(entity);

    void IApplicationDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    public async Task<bool> RagDocumentExistsByNormalizedNameAsync(
        string normalizedFileName,
        CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT EXISTS(SELECT 1 FROM rag_documents WHERE \"NormalizedFileName\" = @name)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "name";
        parameter.Value = normalizedFileName;
        command.Parameters.Add(parameter);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is bool exists && exists;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}