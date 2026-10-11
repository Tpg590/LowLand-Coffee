# 🚀 04-user-guide.md - Hướng Dẫn Vận Hành & Kiểm Thử Auth.API

> Dịch vụ Xác thực & Quản lý Người dùng - Hệ thống LowLand Coffee Microservices (PRN232)  
> Người phụ trách: **Khang**

---

## 1. Yêu cầu Môi trường (Prerequisites)

Để khởi chạy và phát triển dịch vụ `Auth.API`, máy tính cần cài đặt các công cụ:
- **.NET 8 SDK** (phiên bản `8.0.x` trở lên): Kiểm tra bằng lệnh `dotnet --version`.
- **Docker & Docker Compose**: Để chạy cơ sở dữ liệu Microsoft SQL Server 2022.
- **IDE / Trình soạn thảo:** Visual Studio 2022, JetBrains Rider, hoặc Visual Studio Code (kèm extension C# Dev Kit & REST Client).
- **Công cụ quản lý DB (Tùy chọn):** Azure Data Studio, SQL Server Management Studio (SSMS), hoặc DBeaver.

---

## 2. Cấu hình Dịch vụ (`appsettings.json`)

Tệp cấu hình chính nằm tại: `src/backend/services/Auth.API/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AuthConnection": "Server=localhost,1433;Database=Coffee_AuthDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "LowLandCoffee_Super_Secret_Key_For_PRN232_Assignment_2026_Key",
    "Issuer": "LowLandCoffee.Auth",
    "Audience": "LowLandCoffee.Clients",
    "ExpiryInHours": 24,
    "ExpiryInMinutes": 60
  }
}
```

> **Lưu ý:**
> - Chuỗi kết nối mặc định trỏ về SQL Server chạy trên cổng `localhost,1433`.
> - `SecretKey` phải dài tối thiểu 32 ký tự để thuật toán `HmacSha256` hoạt động an toàn.

---

## 3. Các bước Khởi chạy Dịch vụ

### Bước 1: Khởi động Cơ sở dữ liệu (Docker)
Tại thư mục gốc của repository (`LowLand-Coffee/`), chạy lệnh sau để bật SQL Server và tự động tạo 5 database kèm dữ liệu mẫu:

```bash
docker compose up -d sqlserver db-init
```

Kiểm tra trạng thái container:
```bash
docker compose ps
```
Khi container `lowland-sqlserver` báo trạng thái `Up (healthy)`, cơ sở dữ liệu `Coffee_AuthDB` đã sẵn sàng đón nhận kết nối.

### Bước 2: Khởi chạy Auth.API
Mở terminal và di chuyển đến thư mục của dịch vụ:

```bash
cd src/backend/services/Auth.API
dotnet run
```
Hoặc dùng chế độ Hot Reload khi đang phát triển:
```bash
dotnet watch run
```

Sau khi khởi động thành công, màn hình terminal sẽ xuất hiện thông báo:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

## 4. Hướng dẫn Kiểm thử API (Testing)

### 4.1. Kiểm thử trực quan với Swagger UI
1. Mở trình duyệt web và truy cập địa chỉ:
   👉 **`http://localhost:5001/swagger`**
2. **Quy trình kiểm thử chức năng cần xác thực:**
   - Bước 1: Mở endpoint `POST /api/auth/login`, nhấn **Try it out** và đăng nhập bằng tài khoản Admin:
     ```json
     {
       "emailOrPhone": "admin@lowlandcoffee.com",
       "password": "Admin@123"
     }
     ```
   - Bước 2: Sao chép chuỗi `accessToken` nhận được trong phần Response Body.
   - Bước 3: Cuộn lên đầu trang Swagger, nhấn vào nút **Authorize** (biểu tượng chiếc khóa màu xanh).
   - Bước 4: Nhập token vào ô **Value** theo định dạng:
     ```text
     Bearer <chuỗi_access_token_vừa_copy>
     ```
   - Bước 5: Nhấn **Authorize** và **Close**.
   - Bước 6: Bây giờ bạn có thể gọi thử các endpoint quản trị như `GET /api/users`, `POST /api/users/staff`,... mà không bị lỗi `401 Unauthorized`.

---

### 4.2. Kiểm thử tự động với tệp `.http` (Khuyên dùng)
Trong thư mục dự án có sẵn tệp kịch bản kiểm thử:  
👉 **`src/backend/services/Auth.API/Auth.API.http`**

Nếu sử dụng **Visual Studio** hoặc **VS Code** (cài extension `REST Client`), bạn có thể mở file này và click nút **Send Request** trên từng khối:

1. **Test Đăng ký khách hàng:** Chạy khối `1. POST /api/auth/register`.
2. **Test Đăng nhập Admin:** Chạy khối `2. POST /api/auth/login`. Kết quả trả về sẽ tự động gán token vào biến `@adminToken`.
3. **Test Đăng nhập bằng Số điện thoại:** Chạy khối `2b. POST /api/auth/login` với SĐT `0902233445` (tài khoản Staff).
4. **Test Quên mật khẩu & OTP:**
   - Chạy `POST /api/auth/forgot-password` với email `customer@gmail.com`.
   - Xem mã OTP trả về (hoặc xem log terminal).
   - Điền mã OTP vào `POST /api/auth/reset-password` và chạy thử.
5. **Test Xem Profile & Đổi mật khẩu:** Chạy khối `GET /api/auth/profile` và `PUT /api/auth/profile`.
6. **Test Phân trang & Tìm kiếm Admin:** Chạy `GET /api/users?search=admin&pageNumber=1&pageSize=10`.
7. **Test Cấp tài khoản Staff:** Chạy `POST /api/users/staff`.
8. **Test Khóa tài khoản:** Chạy `PUT /api/users/{id}/status`.
9. **Test Xóa cứng (Hard Delete):** Chạy `DELETE /api/users/{id}` và kiểm tra lại danh sách người dùng.

---

## 5. Xử lý các Lỗi Thường Gặp (Troubleshooting)

### ❌ Lỗi 1: `Cannot open database "Coffee_AuthDB" requested by the login. Error Number: 4060`
- **Nguyên nhân:** Container SQL Server đang chạy nhưng Database `Coffee_AuthDB` chưa được tạo.
- **Cách xử lý:** Chạy lệnh sau tại thư mục gốc repository để container `db-init` tự động chạy script tạo 5 DB:
  ```bash
  docker compose up db-init
  ```

### ❌ Lỗi 2: `401 Unauthorized` khi gọi các endpoint Admin hoặc Profile
- **Nguyên nhân:**
  1. Chưa truyền header `Authorization`.
  2. Truyền thiếu từ khóa `Bearer ` trước chuỗi token.
  3. Token đã hết hạn (sau 60 phút).
- **Cách xử lý:** Gọi lại `POST /api/auth/login` để lấy token mới hoặc dùng `POST /api/auth/refresh-token` để gia hạn.

### ❌ Lỗi 3: `403 Forbidden` khi gọi API quản trị `/api/users`
- **Nguyên nhân:** Token hiện tại thuộc về tài khoản có vai trò `Customer` hoặc `Staff`, không có quyền `Admin`.
- **Cách xử lý:** Đăng nhập bằng tài khoản quản trị viên: `admin@lowlandcoffee.com` / `Admin@123`.

### ❌ Lỗi 4: Xung đột cổng `5001` khi chạy `dotnet run`
- **Nguyên nhân:** Có một tiến trình khác đang chiếm giữ cổng `5001`.
- **Cách xử lý:**
  - Kiểm tra tiến trình đang chạy cổng 5001:
    ```bash
    lsof -i :5001
    ```
  - Hoặc tạm thời tắt tiến trình cũ trước khi chạy lại.
