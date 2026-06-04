namespace VirtualMed.Application.Configuration;

public class EmailSettings
{
    public bool Enabled { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "VirtualMed";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    public string EmailApiKey { get; set; } = string.Empty;
    public string? AdminNotificationEmail { get; set; }
    public int EmailVerificationHours { get; set; } = 24;
    public int PasswordResetMinutes { get; set; } = 30;
}
