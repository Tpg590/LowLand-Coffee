# 01-architecture.md - Kiến Trúc Hệ Thống LowLand Coffee (PRN232)

Tài liệu này mô tả chi tiết kiến trúc tổng thể, sơ đồ phân tầng, mô hình Microservices, luồng dữ liệu và cơ chế giao tiếp giữa các thành phần trong hệ thống **LowLand Coffee** (.NET 8 Web API, YARP Gateway, ASP.NET Core Razor Pages, SignalR, SQL Server và Docker).

---

## 1. Sơ Đồ Kiến Trúc Tổng Thể (Microservices Architecture)

Toàn bộ client bên ngoài chỉ tương tác qua **API Gateway (YARP - Port 5000)** hoặc truy cập giao diện từ **Web Razor Pages (Port 5000/5001)**. Toàn bộ 5 Microservices và SQL Server được cô lập trong mạng nội bộ của Docker:

```text
               ┌─────────────────────────────────────────┐
               │    Trình Duyệt Người Dùng (Browser)    │
               │      (Guest, Customer, Staff, Admin)    │
               └────────────────────┬────────────────────┘
                                    │
                                    │ HTTP / HTTPS (Port 5000)
                                    ▼
               ┌─────────────────────────────────────────┐
               │       CoffeeShop.WebRazorPages          │
               │   (ASP.NET Core Razor Pages .NET 8)     │
               │  - Feature Folders (Auth, Menu, Orders) │
               │  - SignalR Client (Nhận tin Realtime)   │
               └────────────────────┬────────────────────┘
                                    │
                                    │ HTTP / REST / WebSocket (Port 5000)
                                    ▼
               ┌─────────────────────────────────────────┐
               │              ApiGateway                 │
               │    (YARP Reverse Proxy - Port 5000)     │
               │  - Single Entry Point                   │
               │  - Centralized CORS & Routing           │
               │  - JWT Validation & WebSocket Forwarding│
               └────────────────────┬────────────────────┘
                                    │
       ┌──────────────┬─────────────┼──────────────┬──────────────┐
       │ (Port 5001)  │ (Port 5002) │ (Port 5003)  │ (Port 5004)  │ (Port 5005)
       ▼              ▼             ▼              ▼              ▼
┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐
│   Auth.API   ││ Product.API  ││  Order.API   ││Engagement.API││Notification  │
│(Khang - Auth)││ (Phú - Menu) ││(Tuấn - Order)││(Trí - Loyalty││    .API      │
│              ││              ││  & Payment)  ││  & Voucher)  ││(Thành-SignalR)
└──────┬───────┘└──────┬───────┘└──────┬───────┘└──────┬───────┘└──────┬───────┘
       │               │               │               │               │
       │ (TCP 1433)    │ (TCP 1433)    │ (TCP 1433)    │ (TCP 1433)    │ (TCP 1433)
       ▼               ▼               ▼               ▼               ▼
┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐┌──────────────┐
│Coffee_AuthDB ││Coffee_Product││Coffee_OrderDB││Coffee_Engage ││Coffee_Notifi │
│              ││      DB      ││              ││    mentDB    ││   cationDB   │
└──────────────┴┴──────────────┴┴──────────────┴┴──────────────┴┴──────────────┘
                       (Chạy trên SQL Server 2022 Container)
```

---

## 2. Các Thành Phần Trong Hệ Thống (System Components)

### 2.1. Frontend Client (`CoffeeShop.WebRazorPages`)
* **Nền tảng:** ASP.NET Core Razor Pages (.NET 8).
* **Mô hình:** Server-side rendering (SSR), tổ chức theo **Feature Folders** (`Pages/Auth`, `Pages/Menu`, `Pages/Orders`, `Pages/Customer`, `Pages/Staff`, `Pages/Admin`).
* **Trách nhiệm:**
  * Hiển thị giao diện cho 4 Actor: Khách vãng lai (Guest), Khách hàng (Customer), Nhân viên (Staff), Quản trị viên (Admin).
  * Gọi API qua tầng `Services/` (`IAuthHttpService`, `IProductHttpService`, `IOrderHttpService`, `IEngagementHttpService`, `INotificationHttpService`) thông qua ApiGateway.
  * Lắng nghe thông báo đơn hàng thời gian thực qua **SignalR Client**.

### 2.2. API Gateway (`ApiGateway`)
* **Nền tảng:** YARP (Yet Another Reverse Proxy) trên nền .NET 8 (Port 5000).
* **Trách nhiệm:**
  * **Single Entry Point:** Điểm tiếp nhận duy nhất cho toàn bộ request từ Client.
  * **Định tuyến (Routing):** Phân luồng request đến đúng Microservice tương ứng.
  * **CORS & Bảo mật:** Cấu hình CORS tập trung và kiểm tra tính hợp lệ của Header JWT Token.
  * **WebSocket Proxy:** Chuyển tiếp kết nối SignalR từ Client tới `Notification.API`.

### 2.3. Cụm 5 Microservices Backend (.NET 8 Web API)
Áp dụng mô hình chuẩn 4 lớp: **`Controller ➔ Service ➔ Repository ➔ DbContext`**:

1. **`Auth.API` (Khang - Port 5001):** Quản lý người dùng, đăng ký, đăng nhập JWT, cấp lại Refresh Token, khôi phục mật khẩu qua mã OTP Email, phân quyền 3 Role (`Customer`, `Staff`, `Admin`).
2. **`Product.API` (Phú - Port 5002):** Quản lý danh mục, menu đồ uống, kích cỡ (Size S/M/L), topping, tìm kiếm & bộ lọc, bật/tắt nhanh trạng thái Còn/Hết hàng.
3. **`Order.API` (Tuấn - Port 5003):** Tạo giỏ hàng, đặt món, tính tiền tự động theo size/topping, quản lý vòng đời trạng thái đơn hàng, thanh toán tiền mặt & cổng thanh toán trực tuyến **VNPay QR Code**.
4. **`Engagement.API` (Trí - Port 5004):** Quản lý mã giảm giá (CRUD Voucher), hệ thống tích điểm thưởng (10k = 1 điểm) và quản lý đặc quyền 4 hạng thẻ thành viên (**Đồng, Bạc, Vàng, Kim Cương**).
5. **`Notification.API` (Thành - Port 5005):** Đẩy thông báo thời gian thực (**SignalR**), phát chuông báo POS khi có đơn mới, gửi thông báo khuyến mãi hàng loạt (Broadcast) và lưu nhật ký (Logs).

### 2.4. Cơ Sở Dữ Liệu (Database-per-Service)
* **Hệ quản trị:** Microsoft SQL Server 2022 (chạy Docker Container `sqlserver:1433`).
* **5 Database độc lập:** `Coffee_AuthDB`, `Coffee_ProductDB`, `Coffee_OrderDB`, `Coffee_EngagementDB`, `Coffee_NotificationDB`.

---

## 3. Luồng Xử Lý Nghiệp Vụ Chính (Data Flows)

### 3.1. Luồng Duyệt Menu & Đặt Món
1. Khách hàng truy cập `/Menu` ➔ Web gọi `GET http://localhost:5000/api/products` qua ApiGateway.
2. Gateway forward sang `Product.API` ➔ lấy danh sách món từ `Coffee_ProductDB`.
3. Khách chọn món, size, topping, nhập mã Voucher ➔ Web gửi `POST http://localhost:5000/api/orders`.
4. Gateway forward sang `Order.API` ➔ tính toán tổng tiền, lưu đơn hàng vào `Coffee_OrderDB`.

### 3.2. Luồng Thông Báo Realtime & Tích Điểm
1. Khi đơn hàng đổi trạng thái (*Đã tiếp nhận ➔ Đang pha chế ➔ Hoàn thành*), `Order.API` gọi `Notification.API`.
2. `Notification.API` đẩy thông báo realtime qua **SignalR Hub** tới trình duyệt của Khách hàng và phát chuông cho Nhân viên.
3. Khi đơn hoàn thành, `Order.API` gọi `Engagement.API` để cộng điểm thưởng vào tài khoản khách hàng theo cấp bậc thẻ thành viên.