var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS policy for Razor Pages and clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Root Health & Info endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "LowLand Coffee YARP API Gateway",
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    routes = new[]
    {
        "/api/auth/**",
        "/api/products/**",
        "/api/orders/**",
        "/api/vouchers/**",
        "/api/notifications/**",
        "/hubs/notification/**"
    }
}));

app.MapReverseProxy();

app.Run();

