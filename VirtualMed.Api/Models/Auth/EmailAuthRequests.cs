namespace VirtualMed.Api.Models.Auth;

public record VerifyEmailRequest(string Token);

public record ResendEmailVerificationRequest(string Email);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);
