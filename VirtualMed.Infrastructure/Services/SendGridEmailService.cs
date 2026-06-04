using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces.Services;

namespace VirtualMed.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IOptions<EmailSettings> settings, ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email deshabilitado. No se envió '{Subject}' a {Email}.", subject, toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.EmailApiKey))
        {
            _logger.LogWarning("EmailApiKey no configurada. No se envió '{Subject}' a {Email}.", subject, toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            _logger.LogWarning("FromEmail no configurado. No se envió '{Subject}' a {Email}.", subject, toEmail);
            return;
        }

        var client = new SendGridClient(_settings.EmailApiKey);
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
        var to = new EmailAddress(toEmail, string.IsNullOrWhiteSpace(toName) ? toEmail : toName);
        var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email enviado a {Email} con asunto '{Subject}'.", toEmail, subject);
            return;
        }

        var body = await response.Body.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "SendGrid respondió {StatusCode} al enviar a {Email}. Detalle: {Detail}",
            (int)response.StatusCode,
            toEmail,
            body);

        throw new InvalidOperationException($"No se pudo enviar el correo (HTTP {(int)response.StatusCode}).");
    }
}
