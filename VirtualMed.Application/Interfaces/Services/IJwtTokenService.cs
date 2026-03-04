using System.Security.Claims;

namespace VirtualMed.Application.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string roleName);
    string GenerateRefreshToken();
    string GenerateTempTwoFactorToken(Guid userId);
    ClaimsPrincipal? ValidateAccessToken(string token);
    (bool Valid, Guid? UserId) ValidateRefreshToken(string token);
    (bool Valid, Guid? UserId) ValidateTempTwoFactorToken(string token);
    string HashToken(string token);
}
