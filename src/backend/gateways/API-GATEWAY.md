# 🚪 API-GATEWAY.md - YARP Reverse Proxy Specification & Implementation

- **Phụ trách:** **Khang**
- **Port lắng nghe:** `8080` (Single Entry Point cho toàn bộ hệ thống LowLand Coffee)
- **Công nghệ:** ASP.NET Core (.NET 8) + **YARP (Yet Another Reverse Proxy 2.1)** + **Rate Limiting** + **Swashbuckle Swagger UI**

---

## 🎯 Mục tiêu & Vai trò Của Gateway

1. **Điểm tiếp nhận duy nhất (Single Point of Entry):** Tiếp nhận toàn bộ request từ Client (`CoffeeShop.WebRazorPages` hoặc Mobile/Third-party apps) và định tuyến chính xác tới 5 Microservices backend.
2. **Ẩn giấu mạng nội bộ (Internal Network Isolation):** Khách hàng và frontend không cần biết địa chỉ IP, hostname container hay port thực tế của 5 dịch vụ phía sau.
3. **CORS tập trung (Centralized CORS):** Cho phép truy cập từ frontend, hỗ trợ `AllowCredentials` để truyền tải Cookie và Token kết nối Realtime.
4. **Bảo vệ hệ thống bằng Rate Limiting:** Thiết lập hạn mức request theo địa chỉ IP (chính sách chung 120 req/phút, chính sách Auth 20 req/phút chống Brute-force/Spam).
5. **Kiểm tra JWT Token sớm & Chuyển tiếp Claims:** Kiểm tra tính toàn vẹn và hạn sử dụng của JWT Token ngay tại Gateway; bóc tách `X-User-Id` và `X-User-Role` chuyển tiếp cho các service nội bộ.
6. **Chuyển tiếp WebSockets / SignalR:** Hỗ trợ kết nối thời gian thực thông suốt từ trình duyệt tới `Notification.API` (`/hubs/notification`).
7. **Kiểm vết phân tán (Distributed Tracing):** Tự động sinh hoặc chuyển tiếp `X-Correlation-Id` cho từng request để truy vết toàn trình qua 5 Microservices.
8. **Chuẩn hóa lỗi mạng (Standardized Downstream Error Handling):** Khi Microservice phía sau gặp sự cố (502 Bad Gateway, 503 Unavailable, 504 Timeout), Gateway chuyển đổi thành cấu trúc JSON chuẩn `ApiResponse<T>`.
9. **Cổng Swagger UI hợp nhất (Unified Swagger Portal):** Truy cập `/swagger` để xem và tương tác với tài liệu API của cả 5 Microservices tại một nơi duy nhất.

---

## 📁 Cấu Trúc Mã Nguồn ApiGateway

```text
src/backend/gateways/ApiGateway/
├── RateLimits/                        # Cấu hình Rate Limit độc lập theo từng Microservice
│   ├── default.ratelimit.json         # Hạn mức mặc định hệ thống (100 req/60s)
│   ├── auth.ratelimit.json            # Quy tắc riêng cho Auth (Login: 5, Register: 10, OTP: 3)
│   ├── product.ratelimit.json         # Quy tắc riêng cho Product & Upload ảnh
│   ├── order.ratelimit.json           # Quy tắc riêng cho Đặt món & Thanh toán VNPay
│   ├── engagement.ratelimit.json      # Quy tắc riêng cho Voucher & Loyalty
│   └── notification.ratelimit.json    # Quy tắc riêng cho Broadcast & Thông báo
├── Extensions/
│   └── GatewayServiceExtensions.cs    # Đăng ký CORS, Dynamic Granular Rate Limiting
├── Middleware/
│   ├── CorrelationIdMiddleware.cs     # Gắn & theo dõi X-Correlation-Id
│   ├── RequestLoggingMiddleware.cs    # Ghi log thời gian phản hồi & latency
│   ├── GatewayResponseHandlerMiddleware.cs # Bắt lỗi 502/503/504 chuẩn hóa ApiResponse
│   └── JwtValidationMiddleware.cs     # Kiểm tra tính hợp lệ & hết hạn của JWT
├── Models/
│   ├── ApiResponse.cs                 # Định dạng response chuẩn môn PRN232
│   ├── RateLimitRule.cs               # Model quy tắc Rate Limit chi tiết
│   └── GatewayStatusResponse.cs       # Model trả về trạng thái chẩn đoán Gateway
├── Properties/
│   └── launchSettings.json            # Cấu hình cổng HTTP 8080 cục bộ
├── Dockerfile                         # Multi-stage build cho container
├── appsettings.json                   # Bảng định tuyến YARP & Clusters
├── appsettings.Development.json
├── Program.cs                         # Pipeline khởi tạo dịch vụ & endpoints
└── ApiGateway.csproj
```

---

## 🧭 Bảng Quy Hoạch Định Tuyến (Routing Table)

| Route ID | Route Pattern (Vào Gateway) | Cluster Đích | Rate Limit Policy | Mô Tả Nghiệp Vụ |
| :--- | :--- | :--- | :---: | :--- |
| `auth-route` | `/api/auth/{**catch-all}` | `auth-cluster` | `auth-policy` | Đăng ký, đăng nhập JWT, OTP reset mật khẩu |
| `users-route` | `/api/users/{**catch-all}` | `auth-cluster` | `auth-policy` | Quản lý tài khoản người dùng, cấp quyền nhân viên |
| `product-route` | `/api/products/{**catch-all}` | `product-cluster` | `general-policy` | Danh mục món nước, size, topping, trạng thái còn/hết |
| `category-route` | `/api/categories/{**catch-all}` | `product-cluster` | `general-policy` | Quản lý danh mục thực đơn |
| `order-route` | `/api/orders/{**catch-all}` | `order-cluster` | `general-policy` | Đặt hàng, tra cứu đơn, cập nhật trạng thái đơn |
| `payment-route` | `/api/payments/{**catch-all}` | `order-cluster` | `general-policy` | Tạo URL thanh toán VNPay QR, xử lý IPN, tiền mặt |
| `voucher-route` | `/api/vouchers/{**catch-all}` | `engagement-cluster`| `general-policy` | Quản lý mã giảm giá, áp voucher checkout |
| `loyalty-route` | `/api/loyalty/{**catch-all}` | `engagement-cluster`| `general-policy` | Tích điểm thành viên, 4 hạng thẻ, đổi điểm thưởng |
| `notification-route`| `/api/notifications/{**catch-all}`| `notification-cluster`| `general-policy` | Hộp thư thông báo cá nhân, broadcast tin tức |
| `notification-hub-route`| `/hubs/notification/{**catch-all}`| `notification-cluster`| `-` | **SignalR WebSockets Hub** đẩy thông báo Realtime |
| `swagger-*-route` | `/swagger-docs/{service}/{**catch-all}` | Tương ứng | `-` | Proxy Swagger JSON cho Gateway Swagger UI |

---

## 🛡️ Cấu Hình Địa Chỉ Cụm Dịch Vụ (Clusters)

### Khi chạy Local Development (`yarp.json`):
- `auth-cluster` ➔ `http://localhost:5001`
- `product-cluster` ➔ `http://localhost:5002`
- `order-cluster` ➔ `http://localhost:5003`
- `engagement-cluster` ➔ `http://localhost:5004`
- `notification-cluster` ➔ `http://localhost:5005`

### Khi chạy Docker Compose (`docker-compose.yml`):
Tự động ghi đè qua các biến môi trường:
```yaml
ReverseProxy__Clusters__auth-cluster__Destinations__destination1__Address=http://auth-api:8080
ReverseProxy__Clusters__product-cluster__Destinations__destination1__Address=http://product-api:8080
ReverseProxy__Clusters__order-cluster__Destinations__destination1__Address=http://order-api:8080
ReverseProxy__Clusters__engagement-cluster__Destinations__destination1__Address=http://engagement-api:8080
ReverseProxy__Clusters__notification-cluster__Destinations__destination1__Address=http://notification-api:8080
```

---

## 🔍 Các Endpoints Tiện Ích Cung Cấp Bởi Gateway

- **`GET /`**: Trả về trạng thái chẩn đoán hệ thống (Uptime, Server time, danh sách Routes & Clusters đang kết nối).
- **`GET /health`**: Endpoint Health Check chuẩn ASP.NET Core trả về `200 Healthy`.
- **`GET /swagger`**: Cổng Swagger UI hợp nhất hiển thị tài liệu OpenAPI của toàn bộ 5 Microservices.
