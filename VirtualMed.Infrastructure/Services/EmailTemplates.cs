using System.Net;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Infrastructure.Services;

internal static class EmailTemplates
{
    public static string Layout(string title, string bodyHtml) =>
        $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(title)}</title></head>
        <body style="font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;padding:24px;color:#0f172a;">
          <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;padding:24px;">
            <p style="margin:0 0 8px;font-size:12px;letter-spacing:.12em;text-transform:uppercase;color:#2563eb;">VirtualMed</p>
            <h1 style="margin:0 0 16px;font-size:22px;">{WebUtility.HtmlEncode(title)}</h1>
            {bodyHtml}
            <p style="margin-top:24px;font-size:12px;color:#64748b;">
              Este mensaje es informativo. Si no reconoces esta acción, contacta al equipo de soporte de VirtualMed.
            </p>
          </div>
        </body>
        </html>
        """;

    public static string EmailVerification(string fullName, string verifyUrl) =>
        Layout(
            "Verifica tu correo",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Gracias por registrarte en VirtualMed. Para activar tu cuenta, verifica tu correo electrónico:</p>
            <p><a href="{WebUtility.HtmlEncode(verifyUrl)}" style="display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:12px 20px;border-radius:999px;">Verificar correo</a></p>
            <p style="font-size:13px;color:#64748b;">Si el botón no funciona, copia y pega este enlace en tu navegador:<br>{WebUtility.HtmlEncode(verifyUrl)}</p>
            """);

    public static string PasswordReset(string fullName, string resetUrl) =>
        Layout(
            "Restablecer contraseña",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Recibimos una solicitud para restablecer tu contraseña en VirtualMed.</p>
            <p><a href="{WebUtility.HtmlEncode(resetUrl)}" style="display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:12px 20px;border-radius:999px;">Restablecer contraseña</a></p>
            <p style="font-size:13px;color:#64748b;">Este enlace expira pronto. Si no solicitaste este cambio, ignora este mensaje.</p>
            """);

    public static string PasswordChanged(string fullName) =>
        Layout(
            "Contraseña actualizada",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Tu contraseña de VirtualMed fue actualizada correctamente.</p>
            <p>Si no fuiste tú, contacta de inmediato al soporte.</p>
            """);

    public static string TwoFactorEnabled(string fullName) =>
        Layout(
            "Autenticación de dos factores activada",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Se activó la autenticación de dos factores (2FA) en tu cuenta de VirtualMed.</p>
            <p>Si no reconoces esta acción, desactiva 2FA y cambia tu contraseña.</p>
            """);

    public static string TwoFactorDisabled(string fullName) =>
        Layout(
            "Autenticación de dos factores desactivada",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Se desactivó la autenticación de dos factores (2FA) en tu cuenta de VirtualMed.</p>
            <p>Si no fuiste tú, vuelve a activar 2FA y cambia tu contraseña.</p>
            """);

    public static string VitalAlertPatient(string fullName, AlertSeverity severity) =>
        Layout(
            "Nueva alerta de salud",
            $"""
            <p>Hola {WebUtility.HtmlEncode(fullName)},</p>
            <p>Tienes una nueva alerta de salud en VirtualMed.</p>
            <p><strong>Severidad:</strong> {WebUtility.HtmlEncode(MapSeverity(severity))}</p>
            <p>Inicia sesión en la plataforma para revisar el detalle clínico de forma segura.</p>
            <p style="font-size:13px;color:#64748b;">Por privacidad, no incluimos valores clínicos en este correo.</p>
            """);

    public static string VitalAlertDoctor(string doctorName, string patientName, AlertSeverity severity) =>
        Layout(
            "Alerta clínica de paciente",
            $"""
            <p>Hola Dr(a). {WebUtility.HtmlEncode(doctorName)},</p>
            <p>Se generó una alerta clínica para el paciente {WebUtility.HtmlEncode(patientName)}.</p>
            <p><strong>Severidad:</strong> {WebUtility.HtmlEncode(MapSeverity(severity))}</p>
            <p>Revisa la plataforma para más detalle autenticado.</p>
            """);

    public static string AdminNotification(string message) =>
        Layout(
            "Notificación administrativa",
            $"<p>{WebUtility.HtmlEncode(message)}</p>");

    public static string DoctorApproved(string fullName) =>
        Layout(
            "Cuenta médica aprobada",
            $"""
            <p>Hola Dr(a). {WebUtility.HtmlEncode(fullName)},</p>
            <p>Tu cuenta médica en VirtualMed fue aprobada y ya puedes iniciar sesión.</p>
            """);

    private static string MapSeverity(AlertSeverity severity) =>
        severity switch
        {
            AlertSeverity.Critical => "Crítica",
            AlertSeverity.Warning => "Advertencia",
            _ => "Informativa"
        };
}
