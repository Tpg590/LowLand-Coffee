# 02-setup-environment.md - Hướng Dẫn Thiết Lập Môi Trường (PRN232)

Tài liệu này hướng dẫn chi tiết quy trình chuẩn bị môi trường phát triển, cấu hình công cụ, nạp 5 cơ sở dữ liệu SQL Server và khởi chạy hệ thống bằng Docker hoặc trực tiếp trên Visual Studio 2022 / Rider.

---

## 1. Yêu Cầu Tiên Quyết (Prerequisites)

| Công cụ | Phiên bản tối thiểu | Mục đích sử dụng | Link tải về |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `8.0.x` | Biên dịch 5 Microservices, API Gateway và Razor Pages Web | [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Docker Desktop** | `4.25.x+` (bật WSL 2 trên Windows) | Chạy SQL Server, Redis, RabbitMQ và các container service | [Download Docker Desktop](https://www.docker.com/products/docker-desktop/) |
| **Visual Studio 2022** / **Rider** | v17.8+ (Community/Pro) | Môi trường lập trình & Debug chính | [Download Visual Studio](https://visualstudio.microsoft.com/vs/) |
| **SSMS** / Azure Data Studio | v19.x+ | Quản trị 5 Database trên SQL Server | [Download SSMS](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) |
| **Git** | `2.40+` | Quản lý mã nguồn theo Git Flow | [Download Git](https://git-scm.com/) |

---

## 2. Chuẩn Bị Mã Nguồn (Clone & Restore)

```bash
# 1. Clone mã nguồn
git clone https://github.com/Tpg590/LowLand-Coffee.git
cd LowLand-Coffee

# 2. Kiểm tra phiên bản .NET SDK (yêu cầu 8.0.x)
dotnet --version

# 3. Khôi phục packages cho Backend Solution
dotnet restore src/Backend/CoffeeShop.Backend.sln
```

---

## 3. Thiết Lập Cơ Sở Dữ Liệu SQL Server

### Bước 1: Khởi động container SQL Server
```bash
docker compose up -d sqlserver
```

### Bước 2: Quản lý 5 Cơ sở dữ liệu bằng SSMS
1. Mở **SQL Server Management Studio (SSMS)**.
2. Kết nối tới server:
   * **Server name:** `localhost,1433`
   * **Authentication:** `SQL Server Authentication`
   * **Login:** `sa`
   * **Password:** `YourPassword123!` (hoặc password cấu hình trong `docker-compose.yml`)
   * **Trust server certificate:** Tích chọn `True`
3. Hệ thống sẽ tự động tạo hoặc áp dụng Migration cho 5 Database độc lập:
   - `Coffee_AuthDB` (Auth & User)
   - `Coffee_ProductDB` (Product & Catalog)
   - `Coffee_OrderDB` (Order & Payment)
   - `Coffee_EngagementDB` (Voucher & Loyalty)
   - `Coffee_NotificationDB` (Notifications & Logs)

---

## 4. Cách 1: Khởi Chạy Nhanh Bằng Docker Compose

```bash
# 1. Khởi động toàn bộ hạ tầng & microservices
docker compose up -d --build

# 2. Kiểm tra trạng thái containers
docker compose ps

# 3. Xem log thời gian thực của toàn bộ hệ thống
docker compose logs -f

# 4. Tắt hệ thống khi kết thúc ca làm việc
docker compose down
```

---

## 5. Cách 2: Khởi Chạy Bằng Visual Studio 2022 (Để Debug)

1. Đảm bảo container `sqlserver` đang chạy (`docker compose up -d sqlserver`).
2. Mở file `src/Backend/CoffeeShop.Backend.sln` trong Visual Studio 2022.
3. Click chuột phải vào **Solution 'CoffeeShop.Backend'** $\rightarrow$ chọn **Properties**.
4. Chọn **Startup Project** $\rightarrow$ chọn **Multiple startup projects**.
5. Cấu hình **Action** thành **Start** cho các project bạn đang phát triển (ví dụ: `ApiGateway` + `Auth.API` + `Product.API`).
6. Nhấn **F5** để khởi chạy các dự án cùng lúc và bắt đầu đặt breakpoint debug.

---

## 6. Xử Lý Sự Cố Thường Gặp (Troubleshooting)

| Lỗi gặp phải | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Port 1433 is already in use** | Máy tính đã cài SQL Server local chạy ngầm chiếm cổng. | Tạm dừng SQL Server trong `services.msc` (Windows) hoặc đổi port ngoài thành `1434:1433`. |
| **502 Bad Gateway trên Gateway** | Service Backend chưa khởi động xong hoặc sai địa chỉ trong `yarp.json`. | Kiểm tra log Gateway và kiểm tra URL đích trong `yarp.json`. |
| **SignalR không kết nối được** | Thiếu cấu hình WebSocket forwarding trên Gateway hoặc CORS. | Kiểm tra rule routing `/hubs/notification/**` trong `yarp.json` và bật `AllowCredentials()`. |