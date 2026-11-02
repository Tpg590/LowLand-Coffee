using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Staff;

public class CategoriesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string ApiGatewayBaseUrl { get; private set; } = "http://localhost:8080";

    public CategoriesModel(IHttpClientFactory httpClientFactory)
    {
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
