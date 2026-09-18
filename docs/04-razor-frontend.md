# 04-razor-frontend.md - Thiết Kế & Quy Chuẩn Frontend Web (ASP.NET Core Razor Pages)

Tài liệu này quy định kiến trúc tầng giao diện **ASP.NET Core Razor Pages** (.NET 8), cơ chế gọi API Gateway, quản lý trạng thái giỏ hàng và các quy chuẩn thiết kế UI/UX cho hệ thống LowLand Coffee.

---

## 1. Nguyên Tắc Cốt Lõi (Architecture Principles)

* **Cổng giao tiếp độc quyền:** Toàn bộ giao tiếp mạng từ Web Frontend bắt buộc hướng về **YARP API Gateway** (`http://localhost:8080`). Trình duyệt hoàn toàn không gọi trực tiếp cổng nội bộ của backend API.
* **Mô hình Razor Pages:** Tận dụng Server-side rendering (SSR) của .NET 8, kết hợp PageModel (`.cshtml` và `.cshtml.cs`) giúp cấu trúc trang gọn gàng, hỗ trợ SEO tối đa.
* **Tách biệt giao diện và logic:** Phân chia rõ ràng giữa tầng hiển thị Razor HTML, Tag Helpers, tầng xử lý PageModel C# và tầng Typed `HttpClient` Services.
* **Quản lý giỏ hàng:** Lưu trữ dữ liệu giỏ hàng linh hoạt thông qua Session hoặc Cookies trên trình duyệt.

---

## 2. Cấu Trúc Thư Mục Chuẩn (`src/LowLandCoffee.Web`)

```text
src/LowLandCoffee.Web/
├── Pages/                               # Các trang giao diện Razor
│   ├── Index.cshtml                     # Trang chủ (Banner, giới thiệu, món nổi bật)
│   ├── Index.cshtml.cs
│   ├── Menu.cshtml                      # Trang thực đơn (Lọc theo danh mục, tìm kiếm)
│   ├── Menu.cshtml.cs
│   ├── ProductDetail.cshtml             # Trang chi tiết món
│   ├── ProductDetail.cshtml.cs
│   ├── Cart.cshtml                      # Trang giỏ hàng & thanh toán
│   ├── Cart.cshtml.cs
│   ├── Error.cshtml
│   ├── Privacy.cshtml
│   ├── _ViewImports.cshtml              # Khai báo Tag Helpers và namespaces
│   ├── _ViewStart.cshtml                # Cấu hình Layout mặc định
│   └── Shared/
│       ├── _Layout.cshtml               # Bố cục website chính (Header, Navbar, Footer)
│       └── _ValidationScriptsPartial.cshtml
├── Services/                            # Typed HttpClient gọi qua Gateway
│   ├── IApiService.cs                   # Định nghĩa các hàm gọi API
│   └── ApiService.cs                    # Triển khai HttpClient gọi YARP Gateway
├── Models/                              # ViewModels & DTOs cho Razor Pages
│   ├── ProductViewModel.cs
│   ├── CategoryViewModel.cs
│   └── CartItemViewModel.cs
├── wwwroot/                             # Tài nguyên tĩnh
│   ├── css/                             # Stylesheet giao diện quán cà phê
│   ├── js/                              # Script tương tác giao diện
│   └── images/                          # Hình ảnh sản phẩm & danh mục
├── appsettings.json                     # Cấu hình địa chỉ API Gateway
└── Program.cs                           # Đăng ký HttpClient, Session, Razor Pages
```

---

## 3. Cơ Chế Đóng Gói Yêu Cầu Mạng (Typed HttpClient & YARP Gateway)

Tất cả các lệnh gọi dữ liệu từ Razor Pages đều thông qua `ApiService` được cấu hình `BaseAddress` trỏ thẳng tới YARP Gateway:

### Đăng ký trong `Program.cs`:
```csharp
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    var gatewayUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(gatewayUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Triển khai trong `Services/ApiService.cs`:
```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductViewModel>> GetProductsAsync(int? categoryId = null)
    {
        var url = categoryId.HasValue ? $"/api/products?categoryId={categoryId}" : "/api/products";
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ProductViewModel>>>(url);
        return response?.Data ?? new List<ProductViewModel>();
    }
}
```

---

## 4. Quản Lý Dữ Liệu Trong PageModel

Ví dụ xử lý dữ liệu trong `Pages/Menu.cshtml.cs`:

```csharp
public class MenuModel : PageModel
{
    private readonly IApiService _apiService;

    public MenuModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    public List<CategoryViewModel> Categories { get; set; } = new();
    public List<ProductViewModel> Products { get; set; } = new();

    public async Task OnGetAsync(int? categoryId)
    {
        Categories = await _apiService.GetCategoriesAsync();
        Products = await _apiService.GetProductsAsync(categoryId);
    }
}
```

---

## 5. Quy Chuẩn Thiết Kế Giao Diện UI/UX

* **Màu sắc chủ đạo:** Tông màu cà phê ấm áp (Nâu trầm, kem be, xanh lá cây đặc trưng đồ uống).
* **Responsive:** Tương thích trên cả điện thoại (Mobile), máy tính bảng (Tablet) và máy tính để bàn (Desktop).
* **Trạng thái giỏ hàng:** Hiển thị số lượng món trên icon giỏ hàng ở thanh Menu Header.