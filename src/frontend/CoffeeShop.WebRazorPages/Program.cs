using Microsoft.AspNetCore.Authentication.Cookies;
using CoffeeShop.WebRazorPages.Services;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure HttpClients pointing to API Gateway
var apiGatewayBaseUrl = builder.Configuration["ApiGateway:BaseUrl"]!;

builder.Services.AddHttpClient("ApiGateway", client =>
{
    client.BaseAddress = new Uri(apiGatewayBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IAuthHttpService, AuthHttpService>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IUserHttpService, UserHttpService>(client =>
{
    client.BaseAddress = new Uri(apiGatewayBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configure Reverse Proxy forwards /api/* and /hubs/* to ApiGateway
var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "api-proxy-route",
        ClusterId = "gateway-cluster",
        Match = new RouteMatch { Path = "/api/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "hubs-proxy-route",
        ClusterId = "gateway-cluster",
        Match = new RouteMatch { Path = "/hubs/{**catch-all}" }
    }
};

var proxyClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "gateway-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "gateway-destination", new DestinationConfig { Address = apiGatewayBaseUrl } }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(proxyRoutes, proxyClusters);

// Register Application Services
builder.Services.AddScoped<IEngagementHttpService, EngagementHttpService>();
builder.Services.AddScoped<IOrderHttpService, OrderHttpService>();

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Cookie Authentication for Razor Pages
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/Staff", () => Results.Redirect("/Staff/Orders"));
app.MapGet("/Admin", () => Results.Redirect("/Admin/Users"));

app.MapRazorPages();
app.MapReverseProxy();

app.Run();
