# 🔐 AUTH-API.md - Auth & User Service Specification

- **Phụ trách:** **Khang**
- **Database:** `Coffee_AuthDB` (SQL Server)
- **Port nội bộ Docker:** `5001`
- **Công nghệ:** ASP.NET Core Web API (.NET 8), Entity Framework Core, ASP.NET Core Identity, JWT

---

## 🎯 Danh sách Chức năng chi tiết (Môn PRN232)

1. **Đăng ký tài khoản (Customer):**
   - Đăng ký tài khoản với Họ tên, Email, SĐT, Mật khẩu.
   - Băm mật khẩu (BCrypt / ASP.NET Identity PasswordHasher).
2. **Đăng nhập & Cấp phát JWT Token:**
   - Đăng nhập bằng Email/SĐT + Mật khẩu.
   - Trả về bộ đôi **JWT Access Token** (thời hạn ngắn: 15-60 phút) và **Refresh Token** (lưu DB, thời hạn dài: 7 ngày).
3. **Quên & Khôi phục mật khẩu (Forgot/Reset Password):**
   - Gửi mã OTP xác nhận hoặc Reset Token qua Email.
   - Xác thực mã OTP và cập nhật lại mật khẩu mới đã băm.
4. **Quản lý Hồ sơ cá nhân (Profile):**
   - Xem thông tin tài khoản, cập nhật Họ tên, Avatar, Số điện thoại, Địa chỉ giao hàng mặc định.
   - Đổi mật khẩu khi đã đăng nhập (kiểm tra Mật khẩu cũ).
5. **Phân quyền người dùng (Role-based Authorization):**
   - 3 Roles: `Customer`, `Staff`, `Admin`.
6. **Chức năng dành cho Quản trị viên (Admin):**
   - Cấp tài khoản cho Nhân viên (Staff).
   - Xem danh sách toàn bộ người dùng, tìm kiếm & phân trang.
   - Cập nhật Role, Khóa / Mở khóa tài khoản (`IsActive`).
   - Xóa mềm tài khoản (`IsDeleted = true`).

---

## 📡 Danh sách RESTful API Endpoints dự kiến

| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | Đăng ký tài khoản khách hàng |
| `POST` | `/api/auth/login` | Public | Đăng nhập & nhận JWT Access/Refresh Token |
| `POST` | `/api/auth/refresh-token` | Public | Cấp lại Access Token mới từ Refresh Token |
| `POST` | `/api/auth/forgot-password` | Public | Gửi mã OTP khôi phục mật khẩu qua email |
| `POST` | `/api/auth/reset-password` | Public | Đổi mật khẩu mới kèm mã OTP |
| `GET` | `/api/auth/profile` | Authenticated | Lấy thông tin tài khoản hiện tại |
| `PUT` | `/api/auth/profile` | Authenticated | Cập nhật thông tin cá nhân |
| `POST` | `/api/auth/change-password` | Authenticated | Đổi mật khẩu |
| `GET` | `/api/users` | Admin | Lấy danh sách toàn bộ người dùng |
| `POST` | `/api/users/staff` | Admin | Tạo tài khoản cho nhân viên |
| `PUT` | `/api/users/{id}/status` | Admin | Khóa / Mở khóa tài khoản |
| `DELETE` | `/api/users/{id}` | Admin | Xóa mềm tài khoản |

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new webapi -n Auth.API -o src/backend/services/Auth.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Auth.API/Auth.API.csproj
dotnet add src/backend/services/Auth.API package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/Auth.API package Microsoft.EntityFrameworkCore.Tools
dotnet add src/backend/services/Auth.API package Microsoft.AspNetCore.Authentication.JwtBearer
```
