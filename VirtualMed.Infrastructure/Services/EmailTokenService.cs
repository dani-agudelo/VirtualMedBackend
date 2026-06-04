using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Infrastructure.Services;

public class EmailTokenService : IEmailTokenService
{
    private readonly IApplicationDbContext _context;

    public EmailTokenService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateTokenAsync(
        Guid userId,
        UserEmailTokenType tokenType,
        TimeSpan validity,
        CancellationToken cancellationToken = default)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var tokenHash = Hash(rawToken);
        var now = DateTime.UtcNow;

        var activeTokens = await _context.Set<UserEmailToken>()
            .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.UsedAt = now;
            _context.Update(token);
        }

        _context.Add(new UserEmailToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = tokenType,
            TokenHash = tokenHash,
            ExpiresAt = now.Add(validity),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public async Task<User?> ValidateAndConsumeTokenAsync(
        string rawToken,
        UserEmailTokenType tokenType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;

        var tokenHash = Hash(rawToken.Trim());
        var now = DateTime.UtcNow;

        var token = await _context.Set<UserEmailToken>()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                     && t.TokenType == tokenType
                     && t.UsedAt == null
                     && t.ExpiresAt > now,
                cancellationToken);

        if (token is null)
            return null;

        token.UsedAt = now;
        _context.Update(token);

        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
