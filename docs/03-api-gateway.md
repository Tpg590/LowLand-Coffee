# 03-api-gateway.md - Quy Hoạch & Cấu Hình YARP API Gateway

Tài liệu này quy định cấu hình định tuyến (Routing), cơ chế chuyển tiếp (Forwarding), và chính sách CORS tại tầng **YARP API Gateway** (`LowLandCoffee.ApiGateway`) cho hệ thống LowLand Coffee.

---

## 1. Vai Trò Của API Gateway (Core Roles)

API Gateway đóng vai trò là điểm tiếp nhận duy nhất (Single Point of Entry) trung chuyển mọi request từ client `LowLandCoffee.Web` tới backend:

* **Định tuyến tập trung (Centralized Routing):** Nhận diện URL từ Frontend Web và ánh xạ chính xác về các cụm service backend tương ứng.
* **Bảo vệ mạng nội bộ (Internal Network Isolation):** Giấu toàn bộ địa chỉ mạng container, tên máy chủ nội bộ và port thực tế của các microservices phía sau.
* **Xử lý CORS tập trung:** Quản lý và cho phép các request từ trình duyệt tại một nơi duy nhất.
* **Abbreviated URL:** Đơn giản hóa đường dẫn gọi API cho client.

---

## 2. Bảng Quy Hoạch Định Tuyến (Routing Table)

API Gateway lắng nghe duy nhất tại cổng **`8080`**. Toàn bộ request từ phía Razor Web client gửi về: `http://localhost:8080`.

| Route Pattern (Đường dẫn vào Gateway) | Service Đích (Downstream Destination) | HTTP Methods | Mục Đích Sử Dụng |
| :--- | :--- | :--- | :--- |
| `/api/categories/{**catch-all}` | `http://localhost:5001/api/categories/{**catch-all}` | `GET`, `POST`, `PUT`, `DELETE` | Lấy danh mục, thêm sửa xóa danh mục đồ uống |
| `/api/products/{**catch-all}` | `http://localhost:5001/api/products/{**catch-all}` | `GET`, `POST`, `PUT`, `DELETE` | Lấy danh sách món, chi tiết món, tạo mới món |
| `/api/orders/{**catch-all}` | `http://localhost:5001/api/orders/{**catch-all}` | `GET`, `POST`, `PUT` | Đặt hàng, xem trạng thái đơn hàng |

---

## 3. Cấu Hình YARP Trong `appsettings.json`

File `src/LowLandCoffee.ApiGateway/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "apiCluster",
        "Match": {
          "Path": "/api/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "apiCluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001"
          }
        }
      }
    }
  }
}
```

> **Lưu ý:** Khi chạy trên Docker Compose, biến môi trường `ReverseProxy__Clusters__apiCluster__Destinations__destination1__Address=http://lowland-api:8080` sẽ tự động ghi đè để trỏ vào tên container trong mạng nội bộ.

---

## 4. Pipeline Đăng Ký YARP Trong `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký YARP Reverse Proxy từ Configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Cấu hình CORS tập trung
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// 3. Kích hoạt Reverse Proxy Endpoints
app.MapReverseProxy();

app.Run();
```

---

## 5. Kiểm Thử Gateway Bằng CLI

### Trên Windows (PowerShell):
```powershell
# Gọi thử API danh mục qua Gateway
Invoke-RestMethod -Uri "http://localhost:8080/api/categories" -Method Get

# Gọi thử API sản phẩm qua Gateway
Invoke-RestMethod -Uri "http://localhost:8080/api/products" -Method Get
```

### Trên Linux / macOS (Bash):
```bash
# Gọi thử API danh mục qua Gateway
curl -i http://localhost:8080/api/categories

# Gọi thử API sản phẩm qua Gateway
curl -i http://localhost:8080/api/products
```