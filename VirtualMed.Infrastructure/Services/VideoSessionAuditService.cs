using System.Text.Json;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Infrastructure.Services;

public class VideoSessionAuditService : IVideoSessionAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public VideoSessionAuditService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task LogEventAsync(
        Guid sessionId,
        string eventType,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var data = JsonSerializer.Serialize(new
        {
            eventType,
            occurredAt = now,
            payload
        });

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            TableName = "video_sessions",
            Operation = "I",
            RowPk = sessionId.ToString(),
            NewData = data,
            DbUser = "app",
            AppUserId = _currentUserService.UserId?.ToString()
        };

        _context.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
