
 using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Mappings;
using Order.API.Repositories;
using Order.API.Services;
using Order.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Xử lý lỗi 400 Bad Request (Validation) tại Microservice theo chuẩn ApiResponse
//    (Lỗi 500 Internal Server Error và Rate Limiting đã do tầng API Gateway đảm nhiệm tập trung)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
                .ToList();

            var firstError = errors.FirstOrDefault() ?? "Dữ liệu đầu vào không hợp lệ (400 Bad Request).";
            var response = ApiResponse<object>.Fail(firstError, StatusCodes.Status400BadRequest, errors);
            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// 2. Configure EF Core DbContext with SQL Server (Coffee_OrderDB)
var connectionString = builder.Configuration.GetConnectionString("OrderConnection")
    ?? Environment.GetEnvironmentVariable("ORDER_DB_CONNECTION")
    ?? "Server=localhost,1433;Database=Coffee_OrderDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;";
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 3. Register Repositories and Services (Dependency Injection: Controller -> Service -> Repository -> DbContext)
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<OrderMappingProfile>());
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(
    client =>
    {
        var notificationUrl =
            builder.Configuration["Services:NotificationApi"];

        client.BaseAddress = new Uri(notificationUrl!);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
var productApiUrl = builder.Configuration["Services:ProductApiUrl"] ?? "http://localhost:5002";
builder.Services.AddHttpClient("ProductApi", client =>
{
    client.BaseAddress = new Uri(productApiUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
var engagementApiUrl = builder.Configuration["Services:EngagementApiUrl"] ?? "http://localhost:5004";
builder.Services.AddHttpClient("EngagementApi", client =>
{
    client.BaseAddress = new Uri(engagementApiUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

// 4. Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Xác thực thông qua API Gateway (Gateway đã xử lý JWT Validation và truyền X-User-Id, X-User-Role, X-User-Email)
builder.Services.AddAuthentication("GatewayHeader")
    .AddScheme<AuthenticationSchemeOptions, GatewayHeaderAuthenticationHandler>("GatewayHeader", null);

builder.Services.AddAuthorization();

// 6. Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LowLand Coffee - Order & Payment API",
        Version = "v1",
        Description = "Microservice xử lý Đặt món, Quản lý đơn hàng & Thanh toán VNPay/Tiền mặt (PRN232)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Bearer token (hoặc truyền qua API Gateway)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 7. Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1"));

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 8. Đảm bảo tất cả các cột văn bản trong Coffee_OrderDB đều là NVARCHAR để hiển thị đúng Tiếng Việt có dấu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_items' AND COLUMN_NAME = 'product_name' AND DATA_TYPE = 'varchar'
            )
            BEGIN
                ALTER TABLE dbo.order_items ALTER COLUMN product_name NVARCHAR(150) NOT NULL;
            END;

            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_items' AND COLUMN_NAME = 'options_summary' AND DATA_TYPE = 'varchar'
            )
            BEGIN
                ALTER TABLE dbo.order_items ALTER COLUMN options_summary NVARCHAR(MAX) NULL;
            END;

            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_orders' AND COLUMN_NAME = 'customer_name'
            )
            BEGIN
                ALTER TABLE dbo.order_orders ADD customer_name NVARCHAR(150) NULL;
            END
            ELSE IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_orders' AND COLUMN_NAME = 'customer_name' AND DATA_TYPE = 'varchar'
            )
            BEGIN
                ALTER TABLE dbo.order_orders ALTER COLUMN customer_name NVARCHAR(150) NULL;
            END;

            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_orders' AND COLUMN_NAME = 'note' AND DATA_TYPE = 'varchar'
            )
            BEGIN
                ALTER TABLE dbo.order_orders ALTER COLUMN note NVARCHAR(MAX) NULL;
            END;

            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_orders' AND COLUMN_NAME = 'cancel_reason' AND DATA_TYPE = 'varchar'
            )
            BEGIN
                ALTER TABLE dbo.order_orders ALTER COLUMN cancel_reason NVARCHAR(MAX) NULL;
            END;
        ");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Không thể kiểm tra schema phụ trợ cho OrderDB: {Message}", ex.Message);
    }
}

app.Run();

/// <summary>
/// Handler đọc thông tin định danh người dùng do API Gateway đã xác thực JWT và gắn vào Header (X-User-Id, X-User-Role, X-User-Email).
/// Hỗ trợ fallback đọc claims từ Bearer token nếu gọi trực tiếp trên Swagger nội bộ.
/// </summary>
public class GatewayHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GatewayHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        var userRole = Request.Headers["X-User-Role"].FirstOrDefault();
        var userEmail = Request.Headers["X-User-Email"].FirstOrDefault();

        // Fallback: Nếu gọi trực tiếp vào port của Order.API qua Swagger mà không đi qua Gateway,
        // đọc payload từ Bearer token (không cần cấu hình JwtBearer validation vì Gateway đã đảm nhiệm).
        if (string.IsNullOrWhiteSpace(userId))
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        userId = jwt.Claims.FirstOrDefault(c =>
                            c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid" || c.Type == "uid" || c.Type == "userId")?.Value;
                        userRole = jwt.Claims.FirstOrDefault(c =>
                            c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                        userEmail = jwt.Claims.FirstOrDefault(c =>
                            c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
                    }
                }
                catch
                {
                    return Task.FromResult(AuthenticateResult.Fail("Token không hợp lệ."));
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Role, string.IsNullOrWhiteSpace(userRole) ? "Customer" : userRole)
        };

        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            claims.Add(new Claim(ClaimTypes.Email, userEmail));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json; charset=utf-8";
        var response = ApiResponse<object>.Fail("Vui lòng đăng nhập để thực hiện thao tác này.", StatusCodes.Status401Unauthorized);
        await Response.WriteAsJsonAsync(response);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json; charset=utf-8";
        var response = ApiResponse<object>.Fail("Tài khoản của bạn không có quyền thực hiện thao tác này.", StatusCodes.Status403Forbidden);
        await Response.WriteAsJsonAsync(response);
    }
}
