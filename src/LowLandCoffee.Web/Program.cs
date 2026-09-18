var builder = WebApplication.CreateBuilder(args);

// Đăng ký Razor Pages
builder.Services.AddRazorPages();

// Cấu hình HttpClient gọi qua API Gateway
builder.Services.AddHttpClient("ApiGateway", client =>
{
    var gatewayUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(gatewayUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
