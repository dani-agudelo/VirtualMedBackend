using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        JwtSettings jwtSettings)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _jwtTokenService.HashToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);
        if (stored == null)
            throw new UnauthorizedAccessException("Token de actualización inválido o expirado.");

        await _refreshTokenRepository.RevokeByIdAsync(stored.Id, cancellationToken);

        var user = await _context.Set<User>()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);
        if (user == null || user.Status != "Active")
            throw new UnauthorizedAccessException("Usuario no autorizado.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Role.Name);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshHash = _jwtTokenService.HashToken(newRefreshToken);

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(refreshEntity, cancellationToken);

        return new RefreshTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresInSeconds = _jwtSettings.AccessTokenMinutes * 60
        };
    }
}
