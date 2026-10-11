using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Hubs;
using Order.API.Middlewares;
using Order.API.Services.Implementations;
using Order.API.Services.Interfaces;
using Order.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Kestrel Payload Limit (Tối đa 1MB chống DoS / Crash app)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB
});

// 2. Add Controllers & FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// 3. Database Context (Coffee_OrderDB)
var connectionString = builder.Configuration.GetConnectionString("OrderConnection")
    ?? Environment.GetEnvironmentVariable("ORDER_DB_CONNECTION")
    ?? "Server=localhost;Database=Coffee_OrderDB;TrustServerCertificate=True;";
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 4. Cấu hình CORS (Hỗ trợ Credentials cho SignalR WebSockets)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 5. Cấu hình Rate Limiting (Global Policy & Strict Policy cho các endpoint nhạy cảm)
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var response = ApiResponse<object>.Fail("Bạn đã gửi quá nhiều yêu cầu trong khoảng thời gian ngắn. Vui lòng thử lại sau.", 429);
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    // Global Rate Limiter: 100 requests / phút
    rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Strict Policy: Tối đa 5 lần thử trong 10-15 phút (chống spam thanh toán QR, brute-force & spam hủy đơn)
    rateLimiterOptions.AddPolicy("StrictPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = httpContext.User.Identity?.Name ?? clientIp;
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{userId}_{httpContext.Request.Path}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            });
    });
});

// 6. Cấu hình SignalR
builder.Services.AddSignalR();

// 7. Cấu hình JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? "LowLandCoffee_Secure_Default_Dev_Secret_Key_2026";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "LowLandCoffee.Auth";
var jwtAudience = builder.Configuration["JwtSettings:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "LowLandCoffee.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };

    // Cho phép lấy token qua QueryString cho SignalR kết nối WebSocket
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 8. Đăng ký Services & HTTP Clients
var productApiUrl = builder.Configuration["Services:ProductApiUrl"] ?? "http://localhost:5002";
builder.Services.AddHttpClient<IProductPricingService, ProductPricingService>(client =>
{
    client.BaseAddress = new Uri(productApiUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 9. Cấu hình Swagger với JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LowLand Coffee - Order & Payment API",
        Version = "v1",
        Description = "Microservice xử lý Giỏ hàng, Đặt món, Thanh toán VNPay/Tiền mặt, SignalR Realtime & Lịch sử đơn hàng"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Bearer token vào ô bên dưới (Ví dụ: Bearer {token})"
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

// 10. Pipeline xử lý HTTP Requests
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1"));
}

app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/order");

app.Run();
