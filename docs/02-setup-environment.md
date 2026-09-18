# 02-setup-environment.md - Hướng Dẫn Thiết Lập Môi Trường (Local Development)

Tài liệu này hướng dẫn chi tiết quy trình chuẩn bị môi trường phát triển, cấu hình công cụ, nạp cơ sở dữ liệu SQL Server và khởi chạy hệ thống bằng Docker hoặc trực tiếp trên Visual Studio 2022.

---

## 1. Yêu Cầu Tiên Quyết (Prerequisites)

Cài đặt các công cụ và SDK theo phiên bản tối thiểu dưới đây trước khi bắt đầu:

| Công cụ | Phiên bản tối thiểu | Mục đích sử dụng | Link tải về |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `8.0.x` | Biên dịch backend, API Gateway và Razor Pages Web | [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Docker Desktop** | `4.25.x+` (bật WSL 2 trên Windows) | Đóng gói và chạy multi-container | [Download Docker Desktop](https://www.docker.com/products/docker-desktop/) |
| **Visual Studio 2022** | v17.8+ (Community/Pro) | Môi trường lập trình & Debug chính | [Download Visual Studio](https://visualstudio.microsoft.com/vs/) |
| **SSMS** | v19.x+ | Quản lý & kiểm tra dữ liệu SQL Server | [Download SSMS](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) |
| **Git** | `2.40+` | Quản lý mã nguồn | [Download Git](https://git-scm.com/) |

---

## 2. Chuẩn Bị Mã Nguồn (Clone & Restore)

### Trên Windows (PowerShell / CMD):
```powershell
# 1. Clone mã nguồn
git clone https://github.com/Tpg590/LowLand-Coffee.git
cd LowLand-Coffee

# 2. Kiểm tra phiên bản .NET SDK
dotnet --version

# 3. Khôi phục packages cho toàn bộ solution
dotnet restore LowLandCoffee.sln

# 4. Biên dịch thử nghiệm
dotnet build LowLandCoffee.sln
```

### Trên Linux / macOS (Bash):
```bash
# 1. Clone mã nguồn
git clone https://github.com/Tpg590/LowLand-Coffee.git
cd LowLand-Coffee

# 2. Kiểm tra phiên bản .NET SDK
dotnet --version

# 3. Khôi phục packages cho toàn bộ solution
dotnet restore LowLandCoffee.sln

# 4. Biên dịch thử nghiệm
dotnet build LowLandCoffee.sln
```

---

## 3. Thiết Lập Cơ Sở Dữ Liệu SQL Server

### Bước 1: Khởi động container SQL Server
```bash
docker compose up -d sqlserver
```

### Bước 2: Nạp dữ liệu bằng SSMS
1. Mở **SQL Server Management Studio (SSMS)**.
2. Kết nối tới server:
   * **Server name:** `localhost,1433`
   * **Authentication:** `SQL Server Authentication`
   * **Login:** `sa`
   * **Password:** `YourStrong@Password123!`
   * **Trust server certificate:** Tích chọn `True`
3. Mở file `database.sql` trong thư mục gốc dự án.
4. Nhấn **F5** để chạy script tạo database `LowLandCoffeeDb`, các bảng và nạp 11 món nước mẫu.

---

## 4. Cách 1: Khởi Chạy Bằng Docker Compose (Toàn Bộ Hệ Thống)

### Trên Windows (PowerShell / CMD) & Linux (Bash):
```bash
# 1. Khởi động toàn bộ cụm container
docker compose up -d --build

# 2. Kiểm tra trạng thái containers
docker compose ps

# 3. Xem log khi cần theo dõi
docker compose logs -f

# 4. Tắt hệ thống khi xong
docker compose down
```

---

## 5. Cách 2: Khởi Chạy Bằng Visual Studio 2022 (Để Debug)

1. Đảm bảo container `sqlserver` đang chạy (`docker compose up -d sqlserver`).
2. Mở file `LowLandCoffee.sln` trong Visual Studio 2022.
3. Click chuột phải vào **Solution 'LowLandCoffee'** $\rightarrow$ chọn **Properties**.
4. Chọn **Startup Project** $\rightarrow$ chọn **Multiple startup projects**.
5. Cấu hình **Action**:
   * `LowLandCoffee.Api` $\rightarrow$ **Start**
   * `LowLandCoffee.ApiGateway` $\rightarrow$ **Start**
   * `LowLandCoffee.Web` $\rightarrow$ **Start**
6. Nhấn **F5** để khởi chạy cả 3 dự án và bắt đầu debug.

---

## 6. Xử Lý Các Sự Cố Thường Gặp (Troubleshooting)

| Lỗi gặp phải | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Port 1433 is already in use** | Máy tính đã cài SQL Server local chạy ngầm chiếm cổng. | Dừng service SQL Server trong `services.msc` (Windows) hoặc đổi port trong `docker-compose.yml` thành `1434:1433`. |
| **502 Bad Gateway trên Gateway** | Backend API chưa khởi động xong hoặc sai hostname trong YARP. | Kiểm tra log của Gateway (`docker compose logs lowland-gateway`) và kiểm tra địa chỉ cấu hình trong `appsettings.json`. |
| **SQL Server container bị Exit (1)** | Mật khẩu `sa` không đạt chuẩn bảo mật Microsoft. | Đảm bảo mật khẩu có tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. |