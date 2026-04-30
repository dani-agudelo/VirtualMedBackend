namespace VirtualMed.Application.Interfaces.Services;

public interface IVideoSessionAuditService
{
    Task LogEventAsync(
        Guid sessionId,
        string eventType,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
