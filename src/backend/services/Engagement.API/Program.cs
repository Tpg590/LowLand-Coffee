using System.Text;
using Engagement.API.Data;
using Engagement.API.Repositories;
using Engagement.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Configure EF Core DbContext with SQL Server (Database First)
var connectionString = builder.Configuration.GetConnectionString("EngagementConnection")
    ?? "Server=localhost,1434;Database=Coffee_EngagementDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;";
builder.Services.AddDbContext<EngagementDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Register Repositories and Services (Dependency Injection)
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<Engagement.API.Mappings.MappingProfile>());
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT Authentication & Gateway Header Forwarding
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "LowLandCoffee.Auth";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "LowLandCoffee.Clients";

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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Hỗ trợ xác thực từ Header X-User-Id & X-User-Role khi gọi qua API Gateway hoặc gọi nội bộ từ Order.API
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                var gwUserId = context.Request.Headers["X-User-Id"].FirstOrDefault();
                var gwUserRole = context.Request.Headers["X-User-Role"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(gwUserId))
                {
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new(System.Security.Claims.ClaimTypes.NameIdentifier, gwUserId),
                        new("sub", gwUserId),
                        new(System.Security.Claims.ClaimTypes.Role, string.IsNullOrWhiteSpace(gwUserRole) ? "Customer" : gwUserRole)
                    };
                    var gwEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(gwEmail))
                    {
                        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, gwEmail));
                    }
                    var identity = new System.Security.Claims.ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                    context.Principal = new System.Security.Claims.ClaimsPrincipal(identity);
                    context.Success();
                }
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = Engagement.API.DTOs.Common.ApiResponse<object>.Failure(
                "Vui lòng đăng nhập để thực hiện thao tác này (401 Unauthorized).",
                StatusCodes.Status401Unauthorized);
            await context.Response.WriteAsJsonAsync(payload);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = Engagement.API.DTOs.Common.ApiResponse<object>.Failure(
                "Tài khoản của bạn không có quyền thực hiện chức năng này (403 Forbidden).",
                StatusCodes.Status403Forbidden);
            await context.Response.WriteAsJsonAsync(payload);
        }
    };
});

builder.Services.AddAuthorization();

// Configure Swagger with JWT Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LowLand Coffee - Engagement & Promotion API",
        Version = "v1",
        Description = "Microservice quản lý Khuyến mãi, Voucher giảm giá & Điểm thưởng Loyalty (PRN232)"
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

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Engagement.API v1"));

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
