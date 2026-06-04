using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VirtualMed.Infrastructure.Persistence.Interceptors;

public sealed class AuditUserIdSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditUserIdSaveChangesInterceptor>? _logger;

    private sealed class State
    {
        public bool SessionAuditContextSet { get; set; }
    }

    private readonly ConditionalWeakTable<DbContext, State> _states = new();

    public AuditUserIdSaveChangesInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditUserIdSaveChangesInterceptor>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? GetAppUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var sub = user?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(sub)) return sub;

        var nameId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(nameId)) return nameId;

        return null;
    }

    private State GetOrCreateState(DbContext context)
    {
        if (_states.TryGetValue(context, out var state))
            return state;

        state = new State();
        _states.Add(context, state);
        return state;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var appUserId = GetAppUserId();
        if (string.IsNullOrWhiteSpace(appUserId))
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var db = context.Database;
        var useLocalConfig = db.CurrentTransaction != null;

        try
        {
            await db.ExecuteSqlRawAsync(
                "SELECT set_config('app.user_id', {0}, {1});",
                new object[] { appUserId, useLocalConfig },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set app.user_id in PostgreSQL (audit context).");
        }

        if (!useLocalConfig)
            GetOrCreateState(context).SessionAuditContextSet = true;

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await ClearSessionAuditContextAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ClearSessionAuditContextAsync(eventData.Context, cancellationToken);
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private async Task ClearSessionAuditContextAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
            return;

        if (!_states.TryGetValue(context, out var state) || !state.SessionAuditContextSet)
            return;

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.user_id', {0}, false);",
                new object[] { string.Empty },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to clear app.user_id in PostgreSQL (audit context).");
        }
        finally
        {
            state.SessionAuditContextSet = false;
        }
    }
}
