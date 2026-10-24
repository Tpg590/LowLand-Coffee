# 📖 Hướng Dẫn Sử Dụng & Vận Hành API Gateway (Developer & User Guide)

Tài liệu này hướng dẫn cách khởi chạy, cấu hình định tuyến, thiết lập hạn mức Rate Limit và kiểm thử các tính năng của **ApiGateway**.

---

## 1. Yêu Cầu Môi Trường & Khởi Chạy

### Cách 1: Khởi chạy trực tiếp bằng .NET CLI (Local Development)

```bash
# Di chuyển vào thư mục của Gateway
cd src/backend/gateways/ApiGateway

# Chạy ứng dụng (Mặc định lắng nghe tại http://localhost:8080)
dotnet run
```

### Cách 2: Khởi chạy cùng hệ thống bằng Docker Compose

```bash
# Tại thư mục gốc của repository (LowLand-Coffee)
docker compose up -d api-gateway

# Hoặc khởi chạy toàn bộ 5 Microservices + Gateway + Database:
docker compose up -d
```

---

## 2. Hướng Dẫn Cấu Hình (Configuration Guide)

### A. Thêm một Route định tuyến mới vào `yarp.json`

Mọi cấu hình định tuyến của YARP nằm độc lập tại file [yarp.json](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/yarp.json). Khi cần định tuyến cho một endpoint hoặc controller mới:

```json
{
  "ReverseProxy": {
    "Routes": {
      "new-service-route": {
        "ClusterId": "new-service-cluster",
        "CorsPolicy": "GatewayCorsPolicy",
        "Match": {
          "Path": "/api/new-service/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "new-service-cluster": {
        "HttpRequest": {
          "Timeout": "00:00:30"
        },
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5006"
          }
        }
      }
    }
  }
}
```

> **Lưu ý:** `yarp.json` được cấu hình `reloadOnChange: true`, nên khi bạn sửa file trong lúc Gateway đang chạy, YARP sẽ tự động cập nhật route mà không cần restart lại ứng dụng.

---

### B. Cấu hình Rate Limit cho từng Service trong `RateLimits/`

Mỗi Microservice có 1 file cấu hình riêng nằm trong thư mục `RateLimits/` (ví dụ `order.ratelimit.json`, `auth.ratelimit.json`).

Ví dụ: Muốn giới hạn endpoint thanh toán `/api/payments/checkout` tối đa **5 request trong 60 giây**:

Mở file [order.ratelimit.json](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/RateLimits/order.ratelimit.json) và thêm rule:

```json
{
  "RateLimiting": {
    "OrderService": [
      {
        "Path": "/api/payments/checkout",
        "Method": "POST",
        "PermitLimit": 5,
        "WindowSeconds": 60,
        "QueueLimit": 0
      }
    ]
  }
}
```

* **`Method`**: `"GET"`, `"POST"`, `"PUT"`, `"DELETE"` hoặc `"*"` cho mọi method.
* **`PermitLimit`**: Số lượng request tối đa được phép gọi trong chu kỳ.
* **`WindowSeconds`**: Chu kỳ thời gian tính bằng giây.
* **`QueueLimit`**: Số lượng request được xếp hàng chờ (đặt `0` để từ chối ngay lập tức khi vượt hạn mức).

---

### C. Cấu hình CORS & JWT Settings trong `appsettings.json`

Tại file [appsettings.json](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/appsettings.json):

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5000",
      "http://localhost:8080"
    ]
  },
  "JwtSettings": {
    "SecretKey": "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key",
    "Issuer": "LowLandCoffee.Auth",
    "Audience": "LowLandCoffee.Clients"
  }
}
```

---

## 3. Hướng Dẫn Kiểm Thử (Verification & Testing)

### A. Kiểm tra trạng thái Gateway (Health & Diagnostics)

```bash
# 1. Health check chuẩn ASP.NET Core
curl -i http://localhost:8080/health
# Kết quả mong đợi: HTTP 200 OK - Healthy

# 2. Xem Dashboard chẩn đoán của Gateway
curl -s http://localhost:8080/ | jq
# Kết quả: Trả về danh sách Routes, Clusters đang kết nối và Uptime của Gateway
```

### B. Kiểm tra Cổng Swagger UI hợp nhất (Unified Portal)

Mở trình duyệt truy cập:
👉 **`http://localhost:8080/swagger`**

Tại góc trên bên phải, bạn sẽ thấy dropdown chuyển đổi tài liệu giữa 5 Microservices:
1. `1. Auth & Identity API (Khang)`
2. `2. Product & Catalog API (Phú)`
3. `3. Order & Payment API (Tuấn)`
4. `4. Engagement & Loyalty API (Trí)`
5. `5. Notification & Realtime API (Thành)`

---

### C. Kiểm tra Distributed Tracing (`X-Correlation-Id`)

Gửi một request bất kỳ đến Gateway và quan sát header trả về:

```bash
curl -i http://localhost:8080/api/products
```

Kết quả trong Response Headers:
```http
HTTP/1.1 200 OK
X-Correlation-Id: e4f9d8a1c3b24789b910111213141516
Access-Control-Expose-Headers: X-Correlation-Id,Content-Disposition,X-Pagination
```
* Nếu client tự gửi `X-Correlation-Id: my-custom-id`, Gateway sẽ tái sử dụng và chuyển tiếp chính xác ID đó xuống microservice.

---

### D. Kiểm tra Chống Giả Mạo Header (Anti-Spoofing)

Giả sử kẻ xấu cố tình chèn header mạo danh Admin:

```bash
curl -i -H "X-User-Id: hacker-id" -H "X-User-Role: Admin" http://localhost:8080/api/orders
```

* **Kết quả:** Gateway tự động tước bỏ (`Remove`) các header này trước khi chuyển tiếp. Service downstream sẽ chỉ nhận request ở trạng thái Anonymous.

---

### E. Kiểm tra Xác Thực Chữ Ký JWT

1. **Gửi Token hợp lệ:**
   ```bash
   curl -i -H "Authorization: Bearer <token_hợp_lệ>" http://localhost:8080/api/orders/my-orders
   ```
   Gateway xác thực chữ ký -> Bóc tách `userId` và gán vào header `X-User-Id` chuyển tiếp an toàn xuống `Order.API`.

2. **Gửi Token giả mạo hoặc sai chữ ký:**
   ```bash
   curl -i -H "Authorization: Bearer token_tam_bợ_tự_chế" http://localhost:8080/api/orders/my-orders
   ```
   **Kết quả:** Gateway chặn đứng ngay tại cửa với mã `401 Unauthorized`:
   ```json
   {
     "success": false,
     "message": "Token xác thực không hợp lệ hoặc chữ ký không đúng.",
     "statusCode": 401,
     "data": null,
     "errors": []
   }
   ```

---

### F. Kiểm tra Cơ chế Rate Limiting

Dùng lệnh gọi liên tiếp để kiểm tra hạn mức (ví dụ endpoint login chỉ cho phép 5 lần):

```bash
for i in {1..7}; do
  curl -s -o /dev/null -w "Lần $i: HTTP %{http_code}\n" -X POST http://localhost:8080/api/auth/login
done
```

**Kết quả:**
```text
Lần 1: HTTP 200 (hoặc 400 nếu thiếu body)
Lần 2: HTTP 200
Lần 3: HTTP 200
Lần 4: HTTP 200
Lần 5: HTTP 200
Lần 6: HTTP 429  <-- Bị chặn!
Lần 7: HTTP 429  <-- Bị chặn!
```
Response khi bị chặn trả về mã `429 Too Many Requests`:
```json
{
  "success": false,
  "message": "Quá nhiều yêu cầu gửi tới endpoint này cùng lúc. Vui lòng thử lại sau.",
  "statusCode": 429,
  "errors": ["Rate limit exceeded for this endpoint. Please try again later."]
}
```
