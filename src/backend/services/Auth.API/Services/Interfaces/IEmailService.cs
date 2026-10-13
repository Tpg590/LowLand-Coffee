namespace Auth.API.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otpCode, CancellationToken cancellationToken = default);
}
