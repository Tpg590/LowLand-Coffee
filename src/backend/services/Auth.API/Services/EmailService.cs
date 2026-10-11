using Auth.API.Services.Interfaces;

namespace Auth.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otpCode, CancellationToken cancellationToken = default)
    {
        // Ghi log mã OTP cho môi trường phát triển / test
        _logger.LogInformation("==================================================");
        _logger.LogInformation("[EMAIL SERVICE] Gửi OTP khôi phục mật khẩu:");
        _logger.LogInformation("Người nhận: {FullName} <{Email}>", fullName, toEmail);
        _logger.LogInformation("Mã OTP xác thực: {OtpCode} (Có hiệu lực trong 15 phút)", otpCode);
        _logger.LogInformation("==================================================");

        return Task.CompletedTask;
    }
}
