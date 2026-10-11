using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;
using Order.API.Hubs;

namespace Order.API.Controllers;

[ApiController]
[Route("api/test")]
[Tags("00. Test & Mock Simulation (Dành cho Test Swagger)")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly OrderDbContext _dbContext;
    private readonly IOrderNotificationService _notificationService;

    public TestController(
        IConfiguration configuration,
        OrderDbContext dbContext,
        IOrderNotificationService notificationService)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Giả lập sinh JWT Token để test trên Swagger mà không cần gọi Auth.API
    /// </summary>
    /// <param name="role">Role mong muốn: Customer, Staff, Admin (mặc định: Customer)</param>
    /// <param name="customUserId">Mã UUID của user (tùy chọn, nếu để trống sẽ tự lấy ID mẫu chuẩn)</param>
    [HttpGet("generate-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GenerateMockToken([FromQuery] string role = "Customer", [FromQuery] Guid? customUserId = null)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? "LowLandCoffee_Secure_Default_Dev_Secret_Key_2026";
        var issuer = _configuration["JwtSettings:Issuer"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "LowLandCoffee.Auth";
        var audience = _configuration["JwtSettings:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "LowLandCoffee.Clients";

        var normalizedRole = char.ToUpper(role[0]) + role[1..].ToLower();
        if (normalizedRole != "Customer" && normalizedRole != "Staff" && normalizedRole != "Admin")
        {
            normalizedRole = "Customer";
        }

        var userId = customUserId ?? normalizedRole switch
        {
            "Staff" => Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Admin" => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            _ => Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, $"{normalizedRole.ToLower()}@lowlandcoffee.com"),
            new(ClaimTypes.Name, $"Mock {normalizedRole} User"),
            new(ClaimTypes.Role, normalizedRole),
            new("CustomerId", userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        var result = new
        {
            Role = normalizedRole,
            UserId = userId,
            Token = tokenString,
            TokenWithBearerPrefix = $"Bearer {tokenString}",
            HowToUseInSwagger = "1. Bấm nút [Authorize] (ổ khóa màu xanh) ở góc trên bên phải trang Swagger. 2. Nhập: Bearer " + tokenString + " 3. Bấm Authorize -> Close."
        };

        return Ok(ApiResponse<object>.Success(result, $"Tạo mock token cho Role '{normalizedRole}' thành công."));
    }

    /// <summary>
    /// Giả lập VNPay thanh toán thành công cho đơn hàng mà không cần bấm qua cổng VNPay thật
    /// </summary>
    [HttpPost("simulate-vnpay-success/{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SimulateVnPaySuccess([FromRoute] Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy đơn hàng.", 404));
        }

        var payment = order.OrderPayments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.VNPAY);
        if (payment == null)
        {
            payment = new OrderPayment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.VNPAY,
                Amount = order.FinalAmount,
                PaymentStatus = PaymentStatus.PENDING
            };
            await _dbContext.OrderPayments.AddAsync(payment, cancellationToken);
        }

        payment.PaymentStatus = PaymentStatus.SUCCESS;
        payment.TransactionCode = $"VNPAY_MOCK_TXN_{Random.Shared.Next(10000000, 99999999)}";
        payment.PaidAt = DateTime.UtcNow;

        if (order.Status == OrderStatus.PENDING)
        {
            order.Status = OrderStatus.PROCESSING;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Bắn SignalR realtime
        await _notificationService.NotifyPaymentConfirmedAsync(
            order.Id, order.OrderCode, order.UserId, PaymentMethod.VNPAY, PaymentStatus.SUCCESS, order.FinalAmount);

        if (order.Status == OrderStatus.PROCESSING)
        {
            await _notificationService.NotifyOrderStatusChangedAsync(
                order.Id, order.OrderCode, order.UserId, order.Status);
        }

        return Ok(ApiResponse<object>.Success(new
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            NewOrderStatus = order.Status,
            PaymentStatus = payment.PaymentStatus,
            TransactionCode = payment.TransactionCode,
            PaidAt = payment.PaidAt
        }, "Giả lập thanh toán VNPay thành công. Trạng thái đơn đã chuyển sang PROCESSING và đã bắn SignalR."));
    }
}
