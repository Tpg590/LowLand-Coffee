using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ApiGatewayBaseUrl { get; private set; } = string.Empty;

    public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public void OnGet()
    {
        var client = _httpClientFactory.CreateClient("ApiGateway");
        ApiGatewayBaseUrl = client.BaseAddress!.ToString().TrimEnd('/');
    }
}
