# 06-docker-guide.md - Sổ Tay Vận Hành Container & Docker Compose (PRN232)

Tài liệu này quy định kiến trúc ảo hóa multi-container, chiến lược mạng nội bộ, cơ chế lưu trữ dữ liệu bền vững và quy trình vận hành toàn bộ hệ thống **LowLand Coffee Microservices** qua Docker Compose.

---

## 1. Kiến Trúc Ảo Hóa Hệ Thống (Containerization Architecture)

Hệ thống được đóng gói thành các container độc lập hoạt động trong mạng nội bộ `lowland-network`:

```text
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Mạng Nội Bộ Docker: lowland-network (Bridge)                                           │
│                                                                                        │
│   ┌────────────────────────┐                ┌──────────────────────────────────────┐   │
│   │      web_frontend      │ (Port 5000)    │             api_gateway              │   │
│   │  (Razor Pages .NET 8)  │ ─────────────> │       (YARP - Port 5000:8080)        │   │
│   └────────────────────────┘                └──────────────────┬───────────────────┘   │
│                                                                │                       │
│        ┌───────────────────┬───────────────────┬───────────────┴───┬───────────────┐   │
│        │ (Port 5001)       │ (Port 5002)       │ (Port 5003)       │ (Port 5004)   │   │
│        ▼                   ▼                   ▼                   ▼               ▼   │
│   ┌───────────┐       ┌───────────┐       ┌───────────┐       ┌───────────┐   ┌───────────┐
│   │ auth_api  │       │product_api│       │ order_api │       │engagement │   │notifica   │
│   │           │       │           │       │           │       │   _api    │   │  tion_api │
│   └─────┬─────┘       └─────┬─────┘       └─────┬─────┘       └─────┬─────┘   └─────┬─────┘
│         │                   │                   │                   │               │   │
│         └───────────────────┼───────────────────┴───────────────────┼───────────────┘   │
│                             │ (TCP 1433)                                                │
│                             ▼                                                           │
│                   ┌───────────────────┐                                                 │
│                   │     sqlserver     │ (MSSQL 2022 - Port 1433:1433)                   │
│                   │ (5 Databases riêng│ Volume: sqlserver-data                          │
│                   └───────────────────┘                                                 │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Kiểm Soát Phơi Lộ Cổng (Port Mapping)

| Service Container | Phụ trách | Port Host | Port Container | Mục đích |
| :--- | :---: | :---: | :---: | :--- |
| `web_frontend` | Cả nhóm | **`5000`** | `8080` | Trình duyệt người dùng truy cập giao diện web |
| `api_gateway` | **Khang** | **`5000`** / `8080`| `8080` | Cổng tiếp nhận duy nhất cho toàn bộ API & SignalR |
| `auth_api` | **Khang** | **`5001`** | `8080` | Mở tạm thời để test Swagger Auth |
| `product_api` | **Phú** | **`5002`** | `8080` | Mở tạm thời để test Swagger Product |
| `order_api` | **Tuấn** | **`5003`** | `8080` | Mở tạm thời để test Swagger Order |
| `engagement_api` | **Trí** | **`5004`** | `8080` | Mở tạm thời để test Swagger Voucher |
| `notification_api` | **Thành** | **`5005`** | `8080` | Mở tạm thời để test Swagger Notification |
| `sqlserver` | Hạ tầng | **`1433`** | `1433` | Quản lý 5 Database bằng SSMS |
| `db-init` | Tự động | *Không mở* | — | Tự động tạo 5 DB & nạp dữ liệu mẫu khi khởi động |

---

## 3. Lệnh Vận Hành Docker Compose

```bash
# 1. Khởi chạy toàn bộ hệ thống ngầm:
docker compose up -d --build

# 2. Hoặc chỉ khởi chạy riêng SQL Server để code & debug local:
docker compose up -d sqlserver

# 3. Kiểm tra danh sách container và trạng thái:
docker compose ps

# 4. Xem log thời gian thực:
docker compose logs -f

# 5. Dừng hệ thống (giữ nguyên dữ liệu database):
docker compose down

# 6. Dừng và xóa sạch toàn bộ dữ liệu Volume:
docker compose down -v
```