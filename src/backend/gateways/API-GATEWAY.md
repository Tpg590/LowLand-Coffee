# 🚪 API-GATEWAY.md - YARP Reverse Proxy Specification

- **Phụ trách:** **Khang**
- **Port tiếp nhận:** `5000` (Single Entry Point cho toàn bộ hệ thống)
- **Công nghệ:** ASP.NET Core (.NET 8) + **YARP (Yet Another Reverse Proxy)**

---

## 🎯 Mục tiêu & Vai trò
1. Là cổng duy nhất (Single Entry Point) tiếp nhận toàn bộ request từ Client (Web Razor Pages / Mobile).
2. Định tuyến (Routing) và phân luồng request chính xác tới 5 Microservices nội bộ.
3. Cấu hình CORS tập trung, Rate Limiting và kiểm tra tính hợp lệ của Header JWT Token.
4. Chuyển tiếp kết nối WebSockets / SignalR từ Web tới `Notification.API`.

---

## 🧭 Cấu hình Routing chi tiết (`yarp.json`)

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "auth-cluster",
        "Match": { "Path": "/api/auth/{**catch-all}" }
      },
      "product-route": {
        "ClusterId": "product-cluster",
        "Match": { "Path": "/api/products/{**catch-all}" }
      },
      "category-route": {
        "ClusterId": "product-cluster",
        "Match": { "Path": "/api/categories/{**catch-all}" }
      },
      "order-route": {
        "ClusterId": "order-cluster",
        "Match": { "Path": "/api/orders/{**catch-all}" }
      },
      "payment-route": {
        "ClusterId": "order-cluster",
        "Match": { "Path": "/api/payments/{**catch-all}" }
      },
      "engagement-route": {
        "ClusterId": "engagement-cluster",
        "Match": { "Path": "/api/vouchers/{**catch-all}" }
      },
      "loyalty-route": {
        "ClusterId": "engagement-cluster",
        "Match": { "Path": "/api/loyalty/{**catch-all}" }
      },
      "notification-route": {
        "ClusterId": "notification-cluster",
        "Match": { "Path": "/api/notifications/{**catch-all}" }
      },
      "notification-hub-route": {
        "ClusterId": "notification-cluster",
        "Match": { "Path": "/hubs/notification/{**catch-all}" }
      }
    },
    "Clusters": {
      "auth-cluster": { "Destinations": { "destination1": { "Address": "http://auth_api:8080" } } },
      "product-cluster": { "Destinations": { "destination1": { "Address": "http://product_api:8080" } } },
      "order-cluster": { "Destinations": { "destination1": { "Address": "http://order_api:8080" } } },
      "engagement-cluster": { "Destinations": { "destination1": { "Address": "http://engagement_api:8080" } } },
      "notification-cluster": { "Destinations": { "destination1": { "Address": "http://notification_api:8080" } } }
    }
  }
}
```

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new web -n ApiGateway -o src/backend/gateways/ApiGateway --framework net8.0
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/gateways/ApiGateway/ApiGateway.csproj
dotnet add src/backend/gateways/ApiGateway package Yarp.ReverseProxy
```
