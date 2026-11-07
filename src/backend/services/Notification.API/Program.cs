
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Notification.API.Hub;
using Notification.API.Model;
using Notification.API.Repository;
using Notification.API.Service;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<CoffeeNotificationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("NotificationConnection")));

// 2. Repository + Service
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// 3. Controllers + SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 4. CORS: cho phép frontend truy cập SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5000",
                "http://localhost:5173",
                "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 5. JWT: các giá trị phải khớp với Auth Service
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key";

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? "LowLandCoffee.Auth";

var jwtAudience = builder.Configuration["JwtSettings:Audience"]
    ?? "LowLandCoffee.Clients";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
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

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecretKey))
        };

        // SignalR WebSocket có thể truyền JWT qua query access_token.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var requestPath = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    requestPath.StartsWithSegments(
                        "/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// 6. Authorization: đăng ký một lần duy nhất
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalService", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// 7. Swagger + JWT
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LowLand Coffee - Notification API",
        Version = "v1",
        Description = "Microservice quản lý thông báo"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token"
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

// 8. Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 9. Endpoints
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
