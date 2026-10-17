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

## 2. Bảng Tra Cứu Nhanh URL Test Trực Tiếp (Swagger UI & Web)

Dưới đây là danh sách toàn bộ URL đầy đủ để các thành viên copy trực tiếp vào trình duyệt test API riêng của mình:

| Dịch Vụ | Phụ Trách | URL Truy Cập Trực Tiếp (Swagger / Web) | Port Host | URL Mạng Nội Bộ Docker |
| :--- | :---: | :--- | :---: | :--- |
| **Web Frontend** | Cả nhóm | [http://localhost:5000](http://localhost:5000) | `5000` | `http://web-frontend:8080` |
| **API Gateway** | **Khang** | [http://localhost:8080](http://localhost:8080) *(Swagger Tổng)* | `8080` | `http://api-gateway:8080` |
| **Auth.API** | **Khang** | [http://localhost:5001/swagger](http://localhost:5001/swagger) | `5001` | `http://auth-api:8080` |
| **Product.API** | **Phú** | [http://localhost:5002/swagger](http://localhost:5002/swagger) | `5002` | `http://product-api:8080` |
| **Order.API** | **Tuấn** | [http://localhost:5003/swagger](http://localhost:5003/swagger) | `5003` | `http://order-api:8080` |
| **Engagement.API** | **Trí** | [http://localhost:5004/swagger](http://localhost:5004/swagger) | `5004` | `http://engagement-api:8080` |
| **Notification.API** | **Thành** | [http://localhost:5005/swagger](http://localhost:5005/swagger) | `5005` | `http://notification-api:8080` |
| **SQL Server 2022** | Hạ tầng | `localhost:1433` *(User: `sa`, Pass: `YourPassword123!`)* | `1433` | `sqlserver:1433` |
| **db-init** | Tự động | *(Tự động chạy nạp 5 DB khi khởi động, không mở cổng)* | — | — |

> 💡 **Mẹo làm việc độc lập:** Nếu bạn chỉ muốn test riêng module của mình mà không cần bật toàn bộ các service khác, hãy dùng lệnh:
> ```bash
> # Ví dụ: Phú chỉ muốn code & test Product.API + Database:
> docker compose up -d sqlserver db-init product-api
> ```


---

## 3. Lệnh Vận Hành Docker Compose

```bash
# 0. Tạo file .env cấu hình môi trường từ .env.example (nếu chưa có):
cp .env.example .env          # Linux / macOS
# copy .env.example .env      # Windows CMD / PowerShell

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