using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Interfaces.Services;

public interface IEmailTokenService
{
    Task<string> CreateTokenAsync(
        Guid userId,
        UserEmailTokenType tokenType,
        TimeSpan validity,
        CancellationToken cancellationToken = default);

    Task<User?> ValidateAndConsumeTokenAsync(
        string rawToken,
        UserEmailTokenType tokenType,
        CancellationToken cancellationToken = default);
}
