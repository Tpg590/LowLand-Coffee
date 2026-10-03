# 03-api-gateway.md - Quy Hoạch & Cấu Hình YARP API Gateway (PRN232)

Tài liệu này quy định cấu hình định tuyến (Routing), cơ chế chuyển tiếp (Forwarding), và chính sách CORS tại tầng **YARP API Gateway** (`ApiGateway` - do **Khang** phụ trách) cho hệ thống LowLand Coffee.

---

## 1. Vai Trò Của API Gateway (Core Roles)

API Gateway đóng vai trò là điểm tiếp nhận duy nhất (Single Point of Entry) trung chuyển mọi request từ client `CoffeeShop.WebRazorPages` tới 5 Microservices backend:

* **Định tuyến tập trung (Centralized Routing):** Nhận diện URL từ Frontend Web và ánh xạ chính xác về 5 cụm service backend tương ứng.
* **Bảo vệ mạng nội bộ (Internal Network Isolation):** Giấu toàn bộ địa chỉ mạng container, tên máy chủ nội bộ và port thực tế của các microservices phía sau.
* **Xử lý CORS tập trung:** Quản lý và cho phép các request từ trình duyệt tại một nơi duy nhất.
* **Chuyển tiếp WebSockets / SignalR:** Đảm bảo kết nối thời gian thực thông suốt tới `Notification.API`.

---

## 2. Bảng Quy Hoạch Định Tuyến (Routing Table)

API Gateway lắng nghe duy nhất tại cổng **`5000`**. Toàn bộ request từ phía Razor Web client gửi về: `http://localhost:5000`.

| Route Pattern (Vào Gateway) | Service Đích (Downstream Destination) | Phụ trách | Mục Đích Sử Dụng |
| :--- | :--- | :---: | :--- |
| `/api/auth/{**catch-all}` | `http://auth_api:8080/api/auth/{**catch-all}` | **Khang** | Đăng ký, đăng nhập JWT, OTP reset mật khẩu, hồ sơ cá nhân |
| `/api/users/{**catch-all}` | `http://auth_api:8080/api/users/{**catch-all}` | **Khang** | Quản lý tài khoản người dùng, cấp account nhân viên (Admin) |
| `/api/products/{**catch-all}` | `http://product_api:8080/api/products/{**catch-all}` | **Phú** | Danh sách món, chi tiết món, size, topping, bật/tắt còn/hết hàng |
| `/api/categories/{**catch-all}` | `http://product_api:8080/api/categories/{**catch-all}` | **Phú** | CRUD danh mục thực đơn |
| `/api/orders/{**catch-all}` | `http://order_api:8080/api/orders/{**catch-all}` | **Tuấn** | Đặt hàng, tra cứu đơn, cập nhật trạng thái đơn |
| `/api/payments/{**catch-all}` | `http://order_api:8080/api/payments/{**catch-all}` | **Tuấn** | Khởi tạo QR VNPay, callback IPN, xác nhận tiền mặt |
| `/api/vouchers/{**catch-all}` | `http://engagement_api:8080/api/vouchers/{**catch-all}` | **Trí** | CRUD Voucher, xác thực mã giảm giá khi checkout |
| `/api/loyalty/{**catch-all}` | `http://engagement_api:8080/api/loyalty/{**catch-all}` | **Trí** | Điểm tích lũy, 4 hạng thẻ thành viên, đổi điểm giảm tiền |
| `/api/notifications/{**catch-all}` | `http://notification_api:8080/api/notifications/{**catch-all}` | **Thành** | Hộp thư thông báo cá nhân, broadcast khuyến mãi, xem logs |
| `/hubs/notification/{**catch-all}` | `http://notification_api:8080/hubs/notification/{**catch-all}` | **Thành** | **SignalR Hub** chuyển tiếp kết nối thời gian thực |

---

## 3. Cấu Hình YARP Trong `yarp.json` / `appsettings.json`

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": { "ClusterId": "auth-cluster", "Match": { "Path": "/api/auth/{**catch-all}" } },
      "users-route": { "ClusterId": "auth-cluster", "Match": { "Path": "/api/users/{**catch-all}" } },
      "product-route": { "ClusterId": "product-cluster", "Match": { "Path": "/api/products/{**catch-all}" } },
      "category-route": { "ClusterId": "product-cluster", "Match": { "Path": "/api/categories/{**catch-all}" } },
      "order-route": { "ClusterId": "order-cluster", "Match": { "Path": "/api/orders/{**catch-all}" } },
      "payment-route": { "ClusterId": "order-cluster", "Match": { "Path": "/api/payments/{**catch-all}" } },
      "voucher-route": { "ClusterId": "engagement-cluster", "Match": { "Path": "/api/vouchers/{**catch-all}" } },
      "loyalty-route": { "ClusterId": "engagement-cluster", "Match": { "Path": "/api/loyalty/{**catch-all}" } },
      "notification-route": { "ClusterId": "notification-cluster", "Match": { "Path": "/api/notifications/{**catch-all}" } },
      "notification-hub-route": { "ClusterId": "notification-cluster", "Match": { "Path": "/hubs/notification/{**catch-all}" } }
    },
    "Clusters": {
      "auth-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5001" } } },
      "product-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5002" } } },
      "order-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5003" } } },
      "engagement-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5004" } } },
      "notification-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5005" } } }
    }
  }
}
```

> **Lưu ý trong Docker Compose:** Biến môi trường sẽ tự động trỏ `Address` tới các tên container nội bộ: `http://auth_api:8080`, `http://product_api:8080`, `http://order_api:8080`, `http://engagement_api:8080`, `http://notification_api:8080`.