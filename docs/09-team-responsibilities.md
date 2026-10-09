# 09-team-responsibilities.md - Phân Công Trách Nhiệm Thành Viên (PRN232)

Tài liệu này xác định rõ vai trò, phạm vi trách nhiệm (Single Responsibility), module phụ trách, port và database tương ứng của từng thành viên trong nhóm phát triển **LowLand Coffee**.

---

## 1. Bảng Phân Công Nhiệm Vụ & Phụ Trách Module

| Thành Viên | Vai Trò / Trách Nhiệm Chính | Module / Service Phụ Trách | Cổng Nội Bộ | Database Độc Lập | Đặc Tả Kỹ Thuật |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Tuấn** | **Team Leader & Order / Payment** | • `Order.API` (Cart, Checkout, VNPay, Order Tracking) | `5003` | `Coffee_OrderDB` | [ORDER-API.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Order.API/ORDER-API.md) |
| **Khang** | **Gateway & Auth Specialist** | • `ApiGateway` (YARP Reverse Proxy)<br>• `Auth.API` (JWT, Users, Roles) | `5000`<br>`5001` | `Coffee_AuthDB` | [API-GATEWAY.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/API-GATEWAY.md)<br>[AUTH-API.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/AUTH-API.md) |
| **Phú** | **Catalog & Product Specialist** | • `Product.API` (Category, Size, Topping, Recipe) | `5002` | `Coffee_ProductDB` | [PRODUCT-API.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Product.API/PRODUCT-API.md) |
| **Trí** | **Loyalty & Engagement Specialist**| • `Engagement.API` (Voucher, Loyalty Tier, Points, Rating) | `5004` | `Coffee_EngagementDB`| [ENGAGEMENT-API.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Engagement.API/ENGAGEMENT-API.md) |
| **Thành** | **Notification & Realtime Specialist**| • `Notification.API` (SignalR Hub, POS Sound, System Logs) | `5005` | `Coffee_NotificationDB`| [NOTIFICATION-API.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Notification.API/NOTIFICATION-API.md) |

---

## 2. Chi Tiết Trách Nhiệm Từng Thành Viên

### 2.1. Tuấn (Team Leader / Order & Payment Service)
* Quản trị repo, phân nhánh Git Flow, điều phối tiến độ và thiết lập quy chuẩn dự án.
* Xây dựng **Order.API** (Port 5003) và quản lý `Coffee_OrderDB`.
* Phát triển các tính năng: Giỏ hàng khách hàng, Tạo đơn hàng, Tích hợp cổng thanh toán trực tuyến **VNPay (VNPAY-QR)** và thanh toán tiền mặt tại quầy, Tra cứu lịch sử đơn hàng.

### 2.2. Khang (Gateway & Auth Service)
* Thiết kế & cấu hình **YARP API Gateway** (Port 5000): CORS, Routing, Reverse Proxy, Token forwarding.
* Xây dựng **Auth.API** (Port 5001): Đăng nhập, Đăng ký, Quản lý tài khoản, Cấp phát JWT Bearer Token, Phân quyền 4 Actor (Guest, Customer, Staff, Admin).

### 2.3. Phú (Product Service)
* Xây dựng **Product.API** (Port 5002) và quản lý `Coffee_ProductDB`.
* Phát triển các tính năng: Danh mục đồ uống, Danh sách món, Biến thể kích thước (Size S/M/L), Topping đi kèm, Công thức pha chế và Tình trạng món (còn/hết).

### 2.4. Trí (Engagement & Loyalty Service)
* Xây dựng **Engagement.API** (Port 5004) và quản lý `Coffee_EngagementDB`.
* Phát triển các tính năng: Chương trình tích điểm thành viên (4 hạng thẻ: Bronze, Silver, Gold, Diamond), Quản lý mã giảm giá / Voucher, Đánh giá món nước và phản hồi của khách hàng.

### 2.5. Thành (Notification & Realtime Service)
* Xây dựng **Notification.API** (Port 5005) và quản lý `Coffee_NotificationDB`.
* Thiết lập **SignalR Hub** đẩy thông báo thời gian thực: Cập nhật trạng thái pha chế cho khách hàng, kích hoạt chuông thông báo đơn mới trên màn hình POS của nhân viên pha chế, Lưu trữ nhật ký thông báo hệ thống.

---

## 3. Liên Kết Tài Liệu Liên Quan
* 🏛️ [01-architecture.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/01-architecture.md) — Kiến trúc tổng thể hệ thống
* 📁 [08-project-structure.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/08-project-structure.md) — Cấu trúc thư mục mã nguồn
* 🤝 [CONTRIBUTING.md](file:///home/bettercallkhang/Github/LowLand-Coffee/CONTRIBUTING.md) — Quy tắc làm việc nhóm & Git Flow
