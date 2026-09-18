# 06-docker-guide.md - Sổ Tay Vận Hành Container & Docker Compose

Tài liệu này quy định kiến trúc ảo hóa container, chiến lược mạng nội bộ, cơ chế lưu trữ dữ liệu bền vững và quy trình vận hành toàn bộ hệ thống LowLand Coffee qua Docker Compose.

---

## 1. Kiến Trúc Ảo Hóa Hệ Thống (Containerization Architecture)

Hệ thống được đóng gói thành 4 container độc lập hoạt động trong mạng nội bộ `lowland-network`:

```text
┌────────────────────────────────────────────────────────────────────────┐
│ Mạng Nội Bộ Docker: lowland-network (Bridge)                           │
│                                                                        │
│   ┌──────────────────┐               ┌───────────────────────────┐     │
│   │   lowland-web    │ (Port 5000:8080)                          │     │
│   │  (Razor Pages)   │ ────────────> │      lowland-gateway      │     │
│   └──────────────────┘               │   (YARP - Port 8080:8080) │     │
│                                      └─────────────┬─────────────┘     │
│                                                    │                   │
│                                                    ▼                   │
│                                      ┌───────────────────────────┐     │
│                                      │        lowland-api        │     │
│                                      │ (.NET 8 - Port 5001:8080) │     │
│                                      └─────────────┬─────────────┘     │
│                                                    │                   │
│                                                    ▼                   │
│                                      ┌───────────────────────────┐     │
│                                      │     lowland-sqlserver     │     │
│                                      │ (MSSQL 2022 - Port 1433)  │     │
│                                      └───────────────────────────┘     │
└────────────────────────────────────────────────────────────────────────┘
```

* **Client Container (`lowland-web`):** Chạy ứng dụng ASP.NET Core Razor Pages .NET 8.
* **Gateway Container (`lowland-gateway`):** YARP Reverse Proxy tiếp nhận lưu lượng từ client và điều phối vào backend.
* **API Container (`lowland-api`):** Xử lý nghiệp vụ và kết nối với cơ sở dữ liệu.
* **Database Container (`lowland-sqlserver`):** Microsoft SQL Server 2022 lưu trữ dữ liệu bền vững qua volume `sqlserver-data`.

---

## 2. Kiểm Soát Phơi Lộ Cổng (Port Mapping)

| Service | Port Host | Port Container | Mô Tả |
| :--- | :--- | :--- | :--- |
| `lowland-web` | **`5000`** | `8080` | Mở cho trình duyệt người dùng truy cập giao diện |
| `lowland-gateway` | **`8080`** | `8080` | Mở cho client gọi REST API |
| `lowland-api` | **`5001`** | `8080` | Mở tạm thời để test Swagger nếu cần |
| `lowland-sqlserver`| **`1433`** | `1433` | Mở để kết nối kiểm tra dữ liệu bằng SSMS ngoài máy host |

---

## 3. Quản Lý Dữ Liệu Bền Vững (Volumes)

* Dữ liệu của SQL Server được ánh xạ vào **Docker Named Volume**: `sqlserver-data`.
* Dữ liệu tồn tại liên tục ngay cả khi bạn tắt (`docker compose down`) hoặc build lại các container.
* Khi muốn **xóa sạch và reset dữ liệu hoàn toàn**, thêm cờ `-v`:
  ```bash
  docker compose down -v
  ```

---

## 4. Sức Khỏe Dịch Vụ & Khởi Động Tuần Tự (Healthcheck)

SQL Server cần 10–20 giây để khởi động engine. Cấu hình `healthcheck` trong `docker-compose.yml` đảm bảo Backend API chỉ kích hoạt sau khi SQL Server đã sẵn sàng nhận kết nối:

```yaml
healthcheck:
  test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Password123!' -C -Q 'SELECT 1' || exit 1"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 15s
```

---

## 5. Bảng Lệnh Vận Hành Chi Tiết (Windows & Linux)

### 5.1. Khởi Chạy Hệ Thống

#### Trên Windows (PowerShell / CMD):
```powershell
# Build lại toàn bộ image và khởi chạy ngầm
docker compose up -d --build

# Hoặc chỉ khởi chạy riêng SQL Server để code local
docker compose up -d sqlserver
```

#### Trên Linux / macOS (Bash):
```bash
# Build lại toàn bộ image và khởi chạy ngầm
docker compose up -d --build

# Hoặc chỉ khởi chạy riêng SQL Server để code local
docker compose up -d sqlserver
```

### 5.2. Theo Dõi & Xem Log

#### Trên Windows & Linux:
```bash
# Kiểm tra danh sách container và trạng thái sức khỏe
docker compose ps

# Xem log theo thời gian thực của toàn bộ hệ thống
docker compose logs -f

# Xem riêng log của Backend API
docker compose logs -f lowland-api

# Xem riêng log của API Gateway
docker compose logs -f lowland-gateway
```

### 5.3. Dừng Hệ Thống

#### Trên Windows & Linux:
```bash
# Dừng container (giữ nguyên dữ liệu database)
docker compose down

# Dừng và xóa sạch toàn bộ volume dữ liệu
docker compose down -v
```