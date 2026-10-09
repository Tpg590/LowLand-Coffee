# 🧾 ORDER-API.md - Order & Payment Service Specification

- **Phụ trách:** **Tuấn (Team Leader)**
- **Database:** `Coffee_OrderDB` (SQL Server - bao gồm bảng Orders, OrderItems, Payments)
- **Port nội bộ Docker:** `5003`
- **Công nghệ:** ASP.NET Core Web API (.NET 8), Entity Framework Core, VNPay SDK / HTTP Client

---

## 🎯 Danh sách Chức năng chi tiết (Môn PRN232)

1. **Tạo đơn hàng mới (Order Creation):**
   - Tiếp nhận danh sách món khách chọn (Size, Topping, Số lượng, Ghi chú pha chế như 50% đường, ít đá).
   - Tự động tính tổng tiền: `[Giá món + Giá Size + Giá Topping] x Số lượng`.
   - Tiếp nhận mã giảm giá Voucher hoặc Điểm thưởng (từ Engagement.API) để trừ tiền.
2. **Tra cứu & Theo dõi đơn hàng:**
   - **Khách hàng:** Xem lịch sử đơn hàng cá nhân, chi tiết từng món và theo dõi trạng thái tiến trình đơn.
   - **Nhân viên / Thu ngân:** Xem danh sách toàn bộ đơn tại quán trong ca làm việc, lọc đơn theo trạng thái (Pending, Confirmed, Preparing, Ready, Completed, Cancelled).
3. **Quản lý quy trình & Cập nhật trạng thái đơn hàng:**
   - Nhân viên chuyển trạng thái: *Đã tiếp nhận ➔ Đang pha chế ➔ Hoàn thành (Sẵn sàng nhận) ➔ Đã giao*.
   - Hỗ trợ **Hủy đơn**: Khách hủy khi quán chưa pha chế, hoặc Staff hủy kèm lý do (Hết nguyên liệu, Khách đổi ý).
4. **Tích hợp & Xử lý thanh toán (Payment Processing):**
   - **Tiền mặt:** Cập nhật trạng thái *Chờ thanh toán* / *Đã thanh toán* tại quầy.
   - **VNPay QR Code:** Tự động tạo URL thanh toán và sinh mã QR động cho từng đơn.
   - **Xử lý Webhook / IPN Callback:** Lắng nghe phản hồi từ VNPay để tự động cập nhật trạng thái đơn hàng sang *Đã thanh toán*.

---

## 📡 Danh sách RESTful API Endpoints dự kiến

| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/orders` | Customer | Tạo đơn hàng mới |
| `GET` | `/api/orders/my-orders` | Customer | Lấy danh sách lịch sử đơn của khách |
| `GET` | `/api/orders/{id}` | Authenticated | Xem chi tiết 1 đơn hàng |
| `GET` | `/api/orders` | Staff / Admin | Lấy danh sách toàn bộ đơn tại quán (lọc theo status) |
| `PUT` | `/api/orders/{id}/status` | Staff | Cập nhật trạng thái đơn (Pha chế, Hoàn thành...) |
| `POST` | `/api/orders/{id}/cancel` | Customer / Staff | Hủy đơn hàng kèm lý do |
| `POST` | `/api/payments/vnpay/create-url` | Customer | Tạo URL / mã QR thanh toán VNPay |
| `GET` | `/api/payments/vnpay/callback` | Public | Tiếp nhận kết quả trả về từ cổng thanh toán VNPay |
| `PUT` | `/api/payments/{id}/cash-confirm` | Staff | Xác nhận đã thu tiền mặt tại quầy |

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new webapi -n Order.API -o src/backend/services/Order.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Order.API/Order.API.csproj
dotnet add src/backend/services/Order.API package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/Order.API package Microsoft.EntityFrameworkCore.Tools
```
