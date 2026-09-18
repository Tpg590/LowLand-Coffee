# ☕ LowLand Coffee - Hệ Thống Web Bán Nước & Cà Phê

> **Hệ thống web thương mại điện tử chuyên phục vụ chuỗi cà phê và đồ uống LowLand Coffee**, xây dựng trên nền tảng **.NET 8**, áp dụng mô hình **Repository Pattern**, **YARP API Gateway**, giao diện **ASP.NET Core Razor Pages**, hệ quản trị cơ sở dữ liệu **Microsoft SQL Server**, và đóng gói chạy cục bộ (Local) đồng nhất bằng **Docker & Docker Compose**.

---

## 📑 Mục Lục
1. [Tech Stack & Công Cụ Sử Dụng](#1-tech-stack--công-cụ-sử-dụng)
2. [Sơ Đồ Kiến Trúc Hệ Thống](#2-sơ-đồ-kiến-trúc-hệ-thống)
3. [Cấu Trúc Thư Mục Dự Án](#3-cấu-trúc-thư-mục-dự-án)
4. [Hướng Dẫn Cài Đặt & Khởi Chạy (Local)](#4-hướng-dẫn-cài-đặt--khởi-chạy-local)
   - [Cách 1: Khởi chạy nhanh bằng Docker Compose](#cách-1-khởi-chạy-nhanh-bằng-docker-compose-khuyên-dùng)
   - [Cách 2: Khởi chạy bằng Visual Studio 2022 (Debug)](#cách-2-khởi-chạy-bằng-visual-studio-2022-để-debug)
5. [Cơ Sở Dữ Liệu & Quản Lý Bằng SSMS](#5-cơ-sở-dữ-liệu--quản-lý-bằng-ssms)
6. [Bảng Tra Cứu Lệnh CLI (Windows vs Linux)](#6-bảng-tra-cứu-lệnh-cli-windows-vs-linux)
7. [Tài Liệu Chi Tiết (Docs Sitemap)](#7-tài-liệu-chi-tiết-docs-sitemap)

---

## 1. Tech Stack & Công Cụ Sử Dụng

| Thành Phần | Công Nghệ / Công Cụ | Phiên Bản | Ghi Chú / Vai Trò |
| :--- | :--- | :--- | :--- |
| **Backend API** | ASP.NET Core Web API | .NET 8 (C# 12) | Xử lý logic nghiệp vụ, áp dụng Repository Pattern & Unit of Work |
| **Frontend Web** | ASP.NET Core Razor Pages | .NET 8 | Giao diện Server-side rendering (SSR), Tag Helpers, Model Binding |
| **API Gateway** | YARP (Yet Another Reverse Proxy) | .NET 8 | Điểm tiếp nhận duy nhất (Port 8080), Reverse Proxy, Routing, CORS |
| **Database** | Microsoft SQL Server | 2022 | Lưu trữ dữ liệu danh mục, sản phẩm, đơn hàng |
| **Database GUI** | SQL Server Management Studio (SSMS) / Azure Data Studio | v19+ | Quản lý database, chạy script `database.sql`, kiểm tra dữ liệu |
| **Container** | Docker & Docker Compose | 4.25+ | Đóng gói và cô lập toàn bộ môi trường chạy local |
| **Source Control** | Git | 2.40+ | Quản lý phiên bản mã nguồn theo Git Flow |
| **Code Tool (IDE)** | Visual Studio 2022 (Community / Professional) | v17.8+ | Môi trường lập trình chính (hoặc VS Code / Rider) |

---

## 2. Sơ Đồ Kiến Trúc Hệ Thống

```text
               ┌─────────────────────────────────────────┐
               │    Trình Duyệt Người Dùng (Browser)    │
               └────────────────────┬────────────────────┘
                                    │
                                    │ HTTP (Port 5000)
                                    ▼
               ┌─────────────────────────────────────────┐
               │         LowLandCoffee.Web               │
               │   (ASP.NET Core Razor Pages .NET 8)     │
               └────────────────────┬────────────────────┘
                                    │
                                    │ HTTP / REST (Port 8080)
                                    ▼
               ┌─────────────────────────────────────────┐
               │       LowLandCoffee.ApiGateway          │
               │    (YARP Reverse Proxy - Port 8080)     │
               │  - Single Entry Point                   │
               │  - Centralized CORS & Routing           │
               └────────────────────┬────────────────────┘
                                    │
                                    │ HTTP (Port 8080/5001 - Mạng nội bộ Docker)
                                    ▼
               ┌─────────────────────────────────────────┐
               │           LowLandCoffee.Api             │
               │       (ASP.NET Core Web API .NET 8)     │
               │  - Controllers: Categories, Products... │
               │  - Application: DTOs, Services          │
               │  - Core: Entities, IRepository, IUnitOfWork │
               │  - Infrastructure: EF Core, Repositories │
               └────────────────────┬────────────────────┘
                                    │
                                    │ TCP Port 1433 (EF Core 8)
                                    ▼
               ┌─────────────────────────────────────────┐
               │       Microsoft SQL Server 2022         │
               │       Database: LowLandCoffeeDb         │
               │   (Quản lý qua SSMS tại localhost,1433) │
               └─────────────────────────────────────────┘
```

---

## 3. Cấu Trúc Thư Mục Dự Án

Cấu trúc dự án được phân tách rõ ràng theo từng tầng trách nhiệm:

```text
LowLand-Coffee/
├── docs/                                 # Bộ tài liệu thiết kế & quy chuẩn kỹ thuật
│   ├── 01-architecture.md                # Thiết kế kiến trúc tổng thể & luồng dữ liệu
│   ├── 02-setup-environment.md           # Hướng dẫn chi tiết setup môi trường dev
│   ├── 03-api-gateway.md                 # Quy hoạch định tuyến YARP Reverse Proxy
│   ├── 04-blazor-frontend.md             # Thiết kế giao diện Razor Web Frontend
│   ├── 05-api-backend.md                 # Thiết kế Backend Web API & Repository Pattern
│   └── 06-docker-guide.md                # Vận hành Docker Compose & xử lý sự cố
├── src/                                  # Mã nguồn chính của giải pháp (.NET 8)
│   ├── LowLandCoffee.ApiGateway/         # Dự án API Gateway (YARP Reverse Proxy)
│   │   ├── appsettings.json              # Cấu hình YARP Routes và Clusters
│   │   ├── Program.cs                    # Pipeline đăng ký YARP
│   │   └── Dockerfile                    # Docker build multi-stage cho Gateway
│   ├── LowLandCoffee.Api/                # Dự án Backend Web API (Repository Pattern)
│   │   ├── Controllers/                  # RESTful API Endpoints (Categories, Products...)
│   │   ├── Core/                         # Domain Layer
│   │   │   ├── Entities/                 # BaseEntity, Category, Product, Order...
│   │   │   └── Interfaces/               # IGenericRepository, IProductRepository, IUnitOfWork
│   │   ├── Infrastructure/               # Tầng dữ liệu & Triển khai Repository
│   │   │   ├── Data/                     # AppDbContext, DbInitializer (Seed Data)
│   │   │   └── Repositories/             # GenericRepository, ProductRepository, UnitOfWork
│   │   ├── Application/                  # Tầng ứng dụng
│   │   │   ├── DTOs/                     # Request/Response DTOs, ApiResponse wrapper
│   │   │   └── Services/                 # Business logic services
│   │   ├── Program.cs                    # Đăng ký DI, EF Core, Swagger
│   │   └── Dockerfile                    # Docker build multi-stage cho API
│   └── LowLandCoffee.Web/                # Dự án Frontend Web (ASP.NET Core Razor Pages)
│       ├── Pages/                        # Trang Razor (.cshtml và .cshtml.cs)
│       ├── Services/                     # Typed HttpClient gọi qua YARP Gateway
│       ├── Models/                       # ViewModels & DTOs cho Razor Pages
│       ├── wwwroot/                      # Tài nguyên tĩnh (CSS, JS, Hình ảnh sản phẩm)
│       ├── Program.cs                    # Cấu hình Razor Pages, Session, HttpClient
│       └── Dockerfile                    # Docker build multi-stage cho Web App
├── LowLandCoffee.sln                     # File Visual Studio Solution
├── docker-compose.yml                    # File điều phối toàn bộ container chạy local
├── database.sql                          # Script T-SQL tạo database & dữ liệu mẫu cho SSMS
├── CONTRIBUTING.md                       # Quy chuẩn Git, commit và checklist Pull Request
└── README.md                             # Tài liệu tổng quan dự án (file này)
```

---

## 4. Hướng Dẫn Cài Đặt & Khởi Chạy (Local)

### Yêu Cầu Cài Đặt Ban Đầu
* **.NET 8 SDK** ([Tải về tại đây](https://dotnet.microsoft.com/download/dotnet/8.0))
* **Docker Desktop** (Bật chế độ WSL2 trên Windows)
* **Visual Studio 2022** (Chọn workload: *ASP.NET and web development*) hoặc **VS Code**
* **SQL Server Management Studio (SSMS)** ([Tải về tại đây](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms))

---

### Cách 1: Khởi chạy nhanh bằng Docker Compose (Khuyên Dùng)

Khởi động đồng thời tất cả các dịch vụ (SQL Server, Backend API, YARP Gateway, Razor Pages Web) chỉ với 1 lệnh:

#### Trên Windows (PowerShell / CMD) & Linux (Bash):
```bash
# 1. Clone repository về máy
git clone https://github.com/Tpg590/LowLand-Coffee.git
cd LowLand-Coffee

# 2. Khởi chạy toàn bộ hệ thống ở chế độ background
docker compose up -d --build

# 3. Kiểm tra trạng thái các container
docker compose ps
```

#### 🌐 Địa chỉ truy cập ứng dụng:
* **Giao diện Web (Razor Pages):** `http://localhost:5000`
* **API Gateway (YARP):** `http://localhost:8080`
* **Backend API Swagger (Dev):** `http://localhost:5001/swagger` (nếu chạy trực tiếp) hoặc qua Gateway `http://localhost:8080/api/products`
* **SQL Server:** `localhost,1433` (User: `sa`, Mật khẩu: `YourStrong@Password123!`)

---

### Cách 2: Khởi chạy bằng Visual Studio 2022 (Để Debug)

Khi cần đặt Breakpoint để debug chi tiết luồng xử lý:

1. **Khởi động riêng container SQL Server:**
   ```bash
   docker compose up -d sqlserver
   ```
2. **Mở Solution:** Nhấp đúp vào file `LowLandCoffee.sln` để mở trong Visual Studio 2022.
3. **Cấu hình Multiple Startup Projects:**
   - Nhấp chuột phải vào **Solution 'LowLandCoffee'** $\rightarrow$ chọn **Properties**.
   - Chọn mục **Startup Project** $\rightarrow$ chọn **Multiple startup projects**.
   - Thiết lập **Action** cho 3 dự án:
     - `LowLandCoffee.Api` $\rightarrow$ **Start**
     - `LowLandCoffee.ApiGateway` $\rightarrow$ **Start**
     - `LowLandCoffee.Web` $\rightarrow$ **Start**
   - Nhấn **Apply** $\rightarrow$ **OK**.
4. **Bắt đầu Debug:** Nhấn phím `F5` hoặc nút **Start**. Trình duyệt sẽ tự động mở giao diện tại `http://localhost:5000`.

---

## 5. Cơ Sở Dữ Liệu & Quản Lý Bằng SSMS

File [database.sql](file:///home/bettercallkhang/Github/LowLand-Coffee/database.sql) chứa toàn bộ script T-SQL khởi tạo database `LowLandCoffeeDb`, các bảng, chỉ mục và dữ liệu menu cà phê/đồ uống mẫu.

### Hướng Dẫn Kết Nối & Chạy Script trên SSMS:
1. Mở **SQL Server Management Studio (SSMS)**.
2. Tại màn hình *Connect to Server*:
   - **Server name:** `localhost,1433` (hoặc `127.0.0.1,1433`)
   - **Authentication:** `SQL Server Authentication`
   - **Login:** `sa`
   - **Password:** `YourStrong@Password123!`
   - **Trust server certificate:** Tích chọn `True`
3. Nhấn **Connect**.
4. Chọn **File** $\rightarrow$ **Open** $\rightarrow$ **File...** $\rightarrow$ Chọn file `database.sql` trong thư mục dự án.
5. Nhấn phím `F5` (hoặc nút **Execute**) để nạp toàn bộ cấu trúc bảng và dữ liệu mẫu.

---

## 6. Bảng Tra Cứu Lệnh CLI (Windows vs Linux)

Dưới đây là bảng tổng hợp các câu lệnh thường dùng tương thích cho cả hai hệ điều hành:

| Tác Vụ | Lệnh trên Windows (PowerShell / CMD) | Lệnh trên Linux / macOS (Bash) |
| :--- | :--- | :--- |
| **Kiểm tra .NET SDK** | `dotnet --version` | `dotnet --version` |
| **Restore packages** | `dotnet restore LowLandCoffee.sln` | `dotnet restore LowLandCoffee.sln` |
| **Build toàn bộ solution** | `dotnet build LowLandCoffee.sln` | `dotnet build LowLandCoffee.sln` |
| **Chạy Web API Backend** | `dotnet run --project src\LowLandCoffee.Api` | `dotnet run --project src/LowLandCoffee.Api` |
| **Chạy YARP Gateway** | `dotnet run --project src\LowLandCoffee.ApiGateway` | `dotnet run --project src/LowLandCoffee.ApiGateway` |
| **Chạy Razor Web Frontend**| `dotnet run --project src\LowLandCoffee.Web` | `dotnet run --project src/LowLandCoffee.Web` |
| **Docker Build & Up** | `docker compose up -d --build` | `docker compose up -d --build` |
| **Kiểm tra Containers** | `docker compose ps` | `docker compose ps` |
| **Xem log thời gian thực** | `docker compose logs -f` | `docker compose logs -f` |
| **Xem log riêng Gateway** | `docker compose logs -f lowland-gateway` | `docker compose logs -f lowland-gateway` |
| **Dừng hệ thống Docker** | `docker compose down` | `docker compose down` |
| **Dừng & Xóa sạch Volume**| `docker compose down -v` | `docker compose down -v` |

---

## 7. Tài Liệu Chi Tiết (Docs Sitemap)

Để tìm hiểu sâu hơn về từng thành phần kiến trúc và quy chuẩn, vui lòng đọc các tài liệu chuyên sâu trong thư mục `docs/`:

* 🏛️ [01-architecture.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/01-architecture.md) — Tổng quan kiến trúc hệ thống, phân tầng và luồng dữ liệu.
* 🛠️ [02-setup-environment.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/02-setup-environment.md) — Chi tiết cài đặt môi trường, biến môi trường và troubleshooting.
* 🚪 [03-api-gateway.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/03-api-gateway.md) — Chi tiết cấu hình YARP Reverse Proxy, CORS và Routing table.
* 💻 [04-blazor-frontend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/04-blazor-frontend.md) — Quy chuẩn thiết kế giao diện Razor Web Frontend và gọi API Gateway.
* ⚙️ [05-api-backend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/05-api-backend.md) — Quy chuẩn Backend API, Repository Pattern, Unit of Work và EF Core.
* 🐳 [06-docker-guide.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/06-docker-guide.md) — Sổ tay vận hành Docker Compose, mạng container và quản lý dữ liệu.
* 🤝 [CONTRIBUTING.md](file:///home/bettercallkhang/Github/LowLand-Coffee/CONTRIBUTING.md) — Quy định nhánh Git, chuẩn Conventional Commits và checklist Pull Request.