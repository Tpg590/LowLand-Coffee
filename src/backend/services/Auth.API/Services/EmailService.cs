using System.Net;
using System.Net.Mail;
using Auth.API.Services.Interfaces;

namespace Auth.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otpCode, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
        var smtpPortStr = _configuration["SmtpSettings:Port"] ?? "587";
        int.TryParse(smtpPortStr, out var smtpPort);
        if (smtpPort <= 0) smtpPort = 587;

        var userName = _configuration["SmtpSettings:UserName"];
        var password = _configuration["SmtpSettings:Password"];
        var fromEmail = _configuration["SmtpSettings:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail)) fromEmail = userName ?? "no-reply@lowlandcoffee.com";
        var fromName = _configuration["SmtpSettings:FromName"] ?? "LowLand Coffee Support";
        var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");

        // Luôn ghi log mã OTP cho môi trường phát triển / test
        _logger.LogInformation("==================================================");
        _logger.LogInformation("[EMAIL SERVICE] Gửi OTP khôi phục mật khẩu:");
        _logger.LogInformation("Người nhận: {FullName} <{Email}>", fullName, toEmail);
        _logger.LogInformation("Mã OTP xác thực: {OtpCode} (Có hiệu lực trong 15 phút)", otpCode);
        _logger.LogInformation("==================================================");

        // Nếu chưa cấu hình thông tin tài khoản SMTP thì chỉ log cảnh báo và bỏ qua gửi mail
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("[EMAIL SERVICE] Chưa cấu hình SMTP_USER hoặc SMTP_PASSWORD trong .env/appsettings.json. Hệ thống chạy ở chế độ dev-mock.");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "[LowLand Coffee] Mã xác thực OTP khôi phục mật khẩu",
                IsBodyHtml = true,
                Body = $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 580px; margin: 0 auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 10px; background-color: #ffffff;"">
    <div style=""text-align: center; padding-bottom: 20px; border-bottom: 2px solid #6F4E37;"">
        <h2 style=""color: #6F4E37; margin: 0; font-size: 24px;"">☕ LowLand Coffee</h2>
        <p style=""color: #888888; margin: 6px 0 0 0; font-size: 13px;"">Hương vị cà phê nguyên bản Tây Nguyên</p>
    </div>
    <div style=""padding: 24px 8px;"">
        <p style=""font-size: 15px; color: #333;"">Xin chào <strong>{fullName}</strong>,</p>
        <p style=""font-size: 15px; color: #333; line-height: 1.6;"">
            Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản đăng ký với email <strong>{toEmail}</strong>.
        </p>
        <p style=""font-size: 15px; color: #333;"">Dưới đây là mã xác thực OTP của bạn:</p>
        
        <div style=""text-align: center; margin: 28px 0;"">
            <span style=""display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #6F4E37; background-color: #F8F5F2; padding: 14px 32px; border: 2px dashed #6F4E37; border-radius: 8px;"">
                {otpCode}
            </span>
        </div>

        <p style=""font-size: 14px; color: #d9534f; margin-bottom: 8px;"">
            ⚠️ <em>Mã OTP có hiệu lực trong vòng <strong>15 phút</strong>. Vui lòng không tiết lộ mã này cho bất kỳ ai.</em>
        </p>
        <p style=""font-size: 13px; color: #666666; line-height: 1.5;"">
            Nếu bạn không thực hiện yêu cầu khôi phục mật khẩu này, bạn có thể yên tâm bỏ qua email này. Tài khoản của bạn vẫn được bảo mật an toàn.
        </p>
    </div>
    <div style=""text-align: center; padding-top: 20px; border-top: 1px solid #eeeeee; font-size: 12px; color: #999999;"">
        <p style=""margin: 0;"">© 2026 LowLand Coffee. All rights reserved.</p>
    </div>
</div>"
            };
            mailMessage.To.Add(new MailAddress(toEmail, fullName));

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("[EMAIL SERVICE] Đã gửi email thành công tới {ToEmail} qua SMTP server {Host}:{Port}", toEmail, smtpHost, smtpPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL SERVICE] Lỗi khi gửi email qua SMTP tới {ToEmail}: {Message}", toEmail, ex.Message);
        }
    }
}
