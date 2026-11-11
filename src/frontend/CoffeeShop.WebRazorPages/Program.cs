using Microsoft.AspNetCore.Authentication.Cookies;
using CoffeeShop.WebRazorPages.Services;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Authorization Policies & Razor Pages Folder Conventions
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Staff", "StaffOnly");
});

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

// Cô lập phạm vi truy cập giao diện theo Role:
// - Admin chỉ được hoạt động trong khu vực /Admin/* (đổi URL sang trang chủ "/", "/Index", "/Order", "/Staff/*" sẽ bị điều hướng về /Admin/Users)
// - Staff chỉ được hoạt động trong khu vực /Staff/* (đổi URL sang trang chủ "/", "/Index", "/Order", "/Admin/*" sẽ bị điều hướng về /Staff/Orders)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    var isSharedSystemPath =
        path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Auth/Logout", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Auth/Profile", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Auth/AccessDenied", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase) ||
        Path.HasExtension(path);

    if (!isSharedSystemPath && context.User.Identity?.IsAuthenticated == true)
    {
        if (context.User.IsInRole("Admin"))
        {
            if (!path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Admin/Users");
                return;
            }
        }
        else if (context.User.IsInRole("Staff"))
        {
            if (!path.StartsWith("/Staff", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Staff/Orders");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapGet("/Staff", () => Results.Redirect("/Staff/Orders"));
app.MapGet("/Admin", () => Results.Redirect("/Admin/Users"));

app.MapRazorPages();
app.MapReverseProxy();

app.Run();
