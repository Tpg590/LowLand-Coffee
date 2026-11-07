
 using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// 5. Configure JWT Authentication
var jwtSecretKey = !string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:SecretKey"])
    ? builder.Configuration["JwtSettings:SecretKey"]!
    : (Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key");
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
});

builder.Services.AddAuthorization();

// 6. Configure Swagger with JWT Bearer Support
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

// 7. Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1"));

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 8. Đảm bảo cột product_name (NVARCHAR) và customer_name tồn tại trong CSDL
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

            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'order_orders' AND COLUMN_NAME = 'customer_name'
            )
            BEGIN
                ALTER TABLE dbo.order_orders ADD customer_name NVARCHAR(150) NULL;
            END;
        ");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Không thể kiểm tra schema phụ trợ cho OrderDB: {Message}", ex.Message);
    }
}

app.Run();
