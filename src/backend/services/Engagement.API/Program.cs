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

// Configure JWT Authentication
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
