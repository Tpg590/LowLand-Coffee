# 🔔 NOTIFICATION-API.md - Notification Service Specification

- **Phụ trách:** **Thành**
- **Database:** `Coffee_NotificationDB` (SQL Server)
- **Port nội bộ Docker:** `5005`
- **Công nghệ:** ASP.NET Core Web API (.NET 8), Entity Framework Core, **SignalR**

---

## 🎯 Danh sách Chức năng chi tiết (Môn PRN232)

1. **Thông báo đơn hàng thời gian thực (Real-time Order Notifications):**
   - Sử dụng **SignalR Hub** (`/hubs/notification`) để đẩy thông báo realtime tới Client mà không cần F5/tải lại trang.
   - **Đối với Khách hàng:** Nhận thông báo tức thì khi trạng thái đơn hàng thay đổi (*"Đơn hàng #102 của bạn đang được pha chế"*, *"Mời bạn nhận món"*).
   - **Đối với Nhân viên:** Phát âm thanh chuông cảnh báo và pop-up tức thì trên màn hình bán hàng (POS/KDS) khi có khách tạo đơn mới.
2. **Gửi thông báo Khuyến mãi & Sự kiện (Marketing Notifications):**
   - Hỗ trợ Admin gửi thông báo hàng loạt (Broadcast) tới toàn bộ Khách hàng về các chiến dịch ưu đãi, món mới ra mắt.
   - Tự động gửi thông báo chúc mừng sinh nhật kèm Voucher đặc xóm cho khách hàng thân thiết.
3. **Quản lý Hộp thư & Nhật ký thông báo (Notification Center & Logs):**
   - **Hộp thư cá nhân (Notification Center):** Lưu trữ danh sách thông báo của từng tài khoản trên Frontend Razor Pages; cho phép đánh dấu Đã đọc / Chưa đọc (`Unread` ➔ `Read`) hoặc xóa thông báo.
   - **Nhật ký hệ thống (Notification Logs):** Lưu lịch sử gửi thông báo (thời gian gửi, người nhận, trạng thái thành công/thất bại) cho Admin tra cứu và kiểm vết.

---

## 📡 Danh sách RESTful API & SignalR Hub dự kiến

| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `WS` | `/hubs/notification` | Authenticated | SignalR Hub kết nối thời gian thực |
| `GET` | `/api/notifications/my-notifications` | Customer | Lấy danh sách thông báo trong Hộp thư cá nhân |
| `PUT` | `/api/notifications/{id}/read` | Customer | Đánh dấu thông báo đã đọc |
| `DELETE` | `/api/notifications/{id}` | Customer | Xóa thông báo khỏi hộp thư cá nhân |
| `POST` | `/api/notifications/order-event` | Internal / Order.API | Tạo thông báo trạng thái đơn & đẩy SignalR |
| `POST` | `/api/notifications/broadcast` | Admin | Gửi thông báo Marketing hàng loạt tới khách |
| `GET` | `/api/notifications/logs` | Admin | Tra cứu nhật ký lịch sử gửi thông báo |

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new webapi -n Notification.API -o src/backend/services/Notification.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Notification.API/Notification.API.csproj
dotnet add src/backend/services/Notification.API package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/Notification.API package Microsoft.EntityFrameworkCore.Tools
```
