using System.Security.Cryptography;
using AutoMapper;
using Auth.API.DTOs;
using Auth.API.Entities;
using Auth.API.Repositories.Interfaces;
using Auth.API.Services.Interfaces;

namespace Auth.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ITokenStore _tokenStore;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService,
        IEmailService emailService,
        ITokenStore tokenStore,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _tokenStore = tokenStore;
        _mapper = mapper;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, null, cancellationToken);
        if (emailExists)
        {
            return ApiResponse<AuthResponse>.Fail("Email đã được sử dụng trong hệ thống", 400);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneExists = await _userRepository.ExistsByPhoneAsync(request.PhoneNumber.Trim(), null, cancellationToken);
            if (phoneExists)
            {
                return ApiResponse<AuthResponse>.Fail("Số điện thoại đã được sử dụng trong hệ thống", 400);
            }
        }

        var customerRole = await _roleRepository.GetByNameAsync("Customer", cancellationToken);
        if (customerRole == null)
        {
            customerRole = await _roleRepository.GetByIdAsync(Guid.Parse("33333333-3333-3333-3333-333333333333"), cancellationToken);
        }

        if (customerRole == null)
        {
            return ApiResponse<AuthResponse>.Fail("Không tìm thấy vai trò mặc định của hệ thống", 500);
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = _mapper.Map<User>(request);
        user.Id = Guid.NewGuid();
        user.RoleId = customerRole.Id;
        user.Role = customerRole;
        user.PasswordHash = hashedPassword;

        await _userRepository.AddAsync(user, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (refreshTokenStr, expiresAt) = _tokenService.GenerateRefreshToken();

        _tokenStore.SaveRefreshToken(refreshTokenStr, user.Id, expiresAt);

        var authResponse = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _tokenService.GetTokenExpiryMinutes() * 60,
            User = _mapper.Map<UserDto>(user)
        };

        return ApiResponse<AuthResponse>.Success(authResponse, "Đăng ký tài khoản thành công", 201);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailOrPhoneAsync(request.EmailOrPhone, cancellationToken);
        if (user == null)
        {
            return ApiResponse<AuthResponse>.Fail("Email/Số điện thoại hoặc mật khẩu không chính xác", 401);
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.", 403);
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return ApiResponse<AuthResponse>.Fail("Email/Số điện thoại hoặc mật khẩu không chính xác", 401);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (refreshTokenStr, expiresAt) = _tokenService.GenerateRefreshToken();

        _tokenStore.SaveRefreshToken(refreshTokenStr, user.Id, expiresAt);

        var authResponse = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _tokenService.GetTokenExpiryMinutes() * 60,
            User = _mapper.Map<UserDto>(user)
        };

        return ApiResponse<AuthResponse>.Success(authResponse, "Đăng nhập thành công");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenRecord = _tokenStore.GetRefreshToken(request.RefreshToken);
        if (tokenRecord == null || tokenRecord.IsRevoked || tokenRecord.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.Fail("Refresh Token không hợp lệ hoặc đã hết hạn", 401);
        }

        var user = await _userRepository.GetByIdAsync(tokenRecord.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Tài khoản người dùng không hợp lệ hoặc đã bị khóa", 403);
        }

        // Thu hồi token cũ (Token Rotation)
        _tokenStore.RevokeRefreshToken(request.RefreshToken);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var (newRefreshTokenStr, expiresAt) = _tokenService.GenerateRefreshToken();

        _tokenStore.SaveRefreshToken(newRefreshTokenStr, user.Id, expiresAt);

        var authResponse = new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenStr,
            TokenType = "Bearer",
            ExpiresIn = _tokenService.GetTokenExpiryMinutes() * 60,
            User = _mapper.Map<UserDto>(user)
        };

        return ApiResponse<AuthResponse>.Success(authResponse, "Cấp lại Access Token thành công");
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
        {
            return ApiResponse<string>.Success(
                "Mã OTP đã được gửi",
                "Nếu email tồn tại trong hệ thống, mã OTP xác thực sẽ được gửi tới hộp thư của bạn");
        }

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _tokenStore.SaveOtp(user.Email, otpCode, DateTime.UtcNow.AddMinutes(15));

        await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, otpCode, cancellationToken);

        return ApiResponse<string>.Success(
            otpCode,
            "Mã OTP khôi phục mật khẩu đã được gửi tới email của bạn (Hiệu lực trong 15 phút)");
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("Yêu cầu đặt lại mật khẩu không hợp lệ", 400);
        }

        var isOtpValid = _tokenStore.ValidateAndConsumeOtp(user.Email, request.OtpCode);
        if (!isOtpValid)
        {
            return ApiResponse<bool>.Fail("Mã OTP không chính xác hoặc đã hết hạn", 400);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _tokenStore.RevokeUserTokens(user.Id);

        return ApiResponse<bool>.Success(true, "Khôi phục mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy thông tin người dùng", 404);
        }

        return ApiResponse<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user), "Lấy thông tin hồ sơ thành công");
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng", 404);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneClean = request.PhoneNumber.Trim();
            var phoneInUse = await _userRepository.ExistsByPhoneAsync(phoneClean, user.Id, cancellationToken);
            if (phoneInUse)
            {
                return ApiResponse<UserProfileDto>.Fail("Số điện thoại đã được tài khoản khác sử dụng", 400);
            }
        }

        _mapper.Map(request, user);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ApiResponse<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user), "Cập nhật thông tin hồ sơ thành công");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy người dùng", 404);
        }

        var isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isOldPasswordCorrect)
        {
            return ApiResponse<bool>.Fail("Mật khẩu hiện tại không chính xác", 400);
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return ApiResponse<bool>.Fail("Mật khẩu mới không được trùng với mật khẩu cũ", 400);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _tokenStore.RevokeUserTokens(user.Id);

        return ApiResponse<bool>.Success(true, "Đổi mật khẩu thành công");
    }
}
