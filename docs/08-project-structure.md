# 08-project-structure.md - Cấu Trúc Thư Mục & Tổ Chức Dự Án (PRN232)

Tài liệu này mô tả chi tiết sơ đồ thư mục, cấu trúc solution, quy chuẩn phân tầng mã nguồn và vai trò của từng thư mục trong dự án **LowLand Coffee**.

---

## 1. Sơ Đồ Cấu Trúc Thư Mục Tổng Thể

```text
LowLand-Coffee/
├── docs/                                # Bộ tài liệu thiết kế & quy chuẩn kỹ thuật
│   ├── 01-architecture.md               # Kiến trúc tổng thể & luồng dữ liệu
│   ├── 02-setup-environment.md          # Hướng dẫn cài đặt môi trường
│   ├── 03-api-gateway.md                # Quy hoạch định tuyến YARP Reverse Proxy
│   ├── 04-razor-frontend.md             # Thiết kế giao diện Razor Web Frontend
│   ├── 05-api-backend.md                # Thiết kế Microservices 4 tầng chuẩn
│   ├── 06-docker-guide.md               # Vận hành Docker Compose
│   ├── 07-tech-stack.md                 # Danh mục Tech Stack & công nghệ
│   ├── 08-project-structure.md          # Chi tiết cấu trúc thư mục dự án
│   ├── 09-team-responsibilities.md      # Phân công trách nhiệm nhóm
│   └── 10-cli-instructions.md           # Bảng tra cứu lệnh CLI nhanh (.NET, NuGet, EF, Docker, Git)
│
├── database/                            # Scripts khởi tạo & dữ liệu mẫu SQL Server
│   └── init-scripts/                    # Script tạo 5 database tự động
│
├── .env.example                         # File mẫu cấu hình biến môi trường Docker (DB, Ports)
├── docker-compose.yml                   # Cấu hình container SQL Server, Gateway, 5 Services, Web
├── CONTRIBUTING.md                      # Quy định nhánh Git Flow & chuẩn commit
├── README.md                            # Trang tài liệu trung tâm (Landing Page)
│
├── src/
│   ├── CoffeeShop.sln                   # Solution hợp nhất quản lý toàn bộ Backend + Frontend
│   │
│   ├── backend/                         # Backend (Microservices + Gateway)
│   │   ├── README.md                    # Tổng quan Backend & lệnh tạo dự án
│   │   │
│   │   ├── gateways/
│   │   │   ├── ApiGateway/              # YARP Reverse Proxy Service (Port 5000)
│   │   │   └── API-GATEWAY.md           # [Khang] Đặc tả YARP Gateway
│   │   │
│   │   └── services/
│   │       ├── Auth.API/                # [Khang] Authentication & Account Service (Port 5001)
│   │       │   └── AUTH-API.md
│   │       ├── Product.API/             # [Phú] Catalog & Product Service (Port 5002)
│   │       │   └── PRODUCT-API.md
│   │       ├── Order.API/               # [Tuấn] Order, Cart & Payment Service (Port 5003)
│   │       │   └── ORDER-API.md
│   │       ├── Engagement.API/          # [Trí] Promotion, Voucher & Loyalty Service (Port 5004)
│   │       │   └── ENGAGEMENT-API.md
│   │       └── Notification.API/        # [Thành] Notification & SignalR Hub Service (Port 5005)
│   │           └── NOTIFICATION-API.md
│   │
│   └── frontend/                        # Frontend (Razor Pages Web App)
        ├── README.md                    # Tổng quan Frontend
        ├── WEB-RAZOR-PAGES.md           # Đặc tả Web Razor Pages (Feature Folders & 4 Actor)
        └── CoffeeShop.WebRazorPages/    # Dự án ASP.NET Core Razor Pages
            ├── Pages/                   # Tổ chức theo Feature Folders
            │   ├── Auth/                # Đăng nhập, Đăng ký, Đổi mật khẩu
            │   ├── Menu/                # Xem Menu, Lọc danh mục, Chi tiết món
            │   ├── Orders/              # Giỏ hàng, Đặt món, Checkout, Tracking
            │   ├── Customer/            # Điểm thưởng, Hạng thẻ, Voucher ví
            │   ├── Staff/               # Màn hình POS quầy, Quản lý đơn chế biến
            │   └── Admin/               # Quản trị món, Doanh thu, Khuyến mãi
            ├── wwwroot/                 # Static files (CSS, JS, Images, Icons)
            └── Program.cs               # Cấu hình Web, DI, SignalR Hub Client
```

---

## 2. Quy Chuẩn Phân Tầng Microservice Backend (4 Layers)

Mỗi dịch vụ tại `src/backend/services/<Service>.API/` tuân thủ kiến trúc phân tầng đơn nhiệm:

```text
<Service>.API/
├── Controllers/                         # Tầng Controller (Endpoints REST API, Validation)
├── Services/                            # Tầng Business Logic (Nghiệp vụ, DTO mapping)
│   └── Interfaces/                      # Interface định nghĩa nghiệp vụ (IService)
├── Repositories/                        # Tầng Data Access (EF Core Queries, CRUD)
│   └── Interfaces/                      # Interface định nghĩa truy vấn (IRepository)
├── Data/                                # Tầng Database Context & Migrations
│   ├── <Service>DbContext.cs
│   └── Migrations/
└── Models/                              # Tầng Entities & DTOs
    ├── Entities/                        # Database Entities
    └── DTOs/                            # Data Transfer Objects (Request/Response)
```

---

## 3. Liên Kết Tài Liệu Liên Quan
* 🏛️ [01-architecture.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/01-architecture.md) — Kiến trúc tổng thể hệ thống
* ⚙️ [05-api-backend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/05-api-backend.md) — Quy chuẩn Microservice 4 tầng
* 💻 [04-razor-frontend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/04-razor-frontend.md) — Quy chuẩn Razor Pages
* 👥 [09-team-responsibilities.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/09-team-responsibilities.md) — Phân công nhiệm vụ theo module
