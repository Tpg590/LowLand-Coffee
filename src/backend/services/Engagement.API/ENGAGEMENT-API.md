# 🎁 ENGAGEMENT-API.md - Engagement / Promotion Service Specification

- **Phụ trách:** **Trí**
- **Database:** `Coffee_EngagementDB` (SQL Server)
- **Port nội bộ Docker:** `5004`
- **Công nghệ:** ASP.NET Core Web API (.NET 8), Entity Framework Core

---

## 🎯 Danh sách Chức năng chi tiết (Môn PRN232)

1. **Quản lý Mã giảm giá (CRUD Voucher - Admin):**
   - Tạo mã Voucher với các loại: Giảm phần trăm (%), Giảm số tiền cố định.
   - Thiết lập điều kiện: Giá trị đơn tối thiểu, Mức giảm tối đa, Ngày bắt đầu / Hết hạn, Giới hạn số lượt dùng.
   - Sửa, gia hạn, vô hiệu hóa / xóa mã giảm giá.
2. **Hệ thống Tích điểm & Nâng hạng thành viên (Loyalty & Membership Tier):**
   - Tự động tích lũy điểm khi đơn hoàn thành: `10.000 VNĐ = 1 điểm`.
   - Quản lý 4 cấp bậc hạng thẻ (Áp dụng riêng từng hạng, không kế thừa):
     - 🥉 **Hạng Đồng (Member):** Tích điểm chuẩn (10k = 1đ), tặng voucher giảm 5% vào tháng sinh nhật.
     - 🥈 **Hạng Bạc (Silver):** Tích điểm x1.1, giảm 10% tháng sinh nhật, miễn phí 1 topping cho đơn > 100k.
     - 🥇 **Hạng Vàng (Gold):** Tích điểm x1.25, giảm 15% tháng sinh nhật, miễn phí 2 topping cho đơn > 130k.
     - 💎 **Hạng Kim Cương (Diamond):** Tích điểm x1.5, giảm 20% tháng sinh nhật, tặng 1 voucher -100% món giá thấp nhất menu (hạn 1 ngày), miễn phí 3 topping cho đơn > 150k.
3. **Kiểm tra & Áp dụng Mã giảm giá / Điểm thưởng:**
   - Cung cấp API kiểm tra tính hợp lệ của mã Voucher khi khách nhập mã tại màn hình Checkout.
   - Cung cấp API quy đổi điểm thưởng thành tiền giảm giá trực tiếp vào tổng hóa đơn.

---

## 📡 Danh sách RESTful API Endpoints dự kiến

| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/vouchers` | Public | Xem danh sách Voucher công khai |
| `POST` | `/api/vouchers` | Admin | Tạo Voucher mới |
| `PUT` | `/api/vouchers/{id}` | Admin | Sửa thông tin / gia hạn Voucher |
| `DELETE` | `/api/vouchers/{id}` | Admin | Vô hiệu hóa / Xóa Voucher |
| `POST` | `/api/vouchers/validate` | Customer | Kiểm tra mã voucher hợp lệ cho đơn hàng |
| `GET` | `/api/loyalty/my-tier` | Customer | Xem điểm tích lũy & hạng thẻ thành viên |
| `POST` | `/api/loyalty/add-points` | Internal / Order.API | Cộng điểm khi hoàn thành đơn hàng |
| `POST` | `/api/loyalty/redeem` | Customer | Quy đổi điểm tích lũy trừ tiền đơn hàng |

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new webapi -n Engagement.API -o src/backend/services/Engagement.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Engagement.API/Engagement.API.csproj
dotnet add src/backend/services/Engagement.API package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/Engagement.API package Microsoft.EntityFrameworkCore.Tools
```
