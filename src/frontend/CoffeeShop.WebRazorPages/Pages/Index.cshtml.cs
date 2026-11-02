using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ApiGatewayBaseUrl { get; private set; } = "http://localhost:8080";

    public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public void OnGet()
    {
        var client = _httpClientFactory.CreateClient("ApiGateway");
        if (client.BaseAddress != null)
        {
            ApiGatewayBaseUrl = client.BaseAddress.ToString().TrimEnd('/');
        }
    }
}
