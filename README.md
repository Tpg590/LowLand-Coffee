# ☕ LowLand Coffee - Hệ Thống Web Bán Nước & Cà Phê (PRN232)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet.svg?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED.svg?style=flat-square&logo=docker)](https://www.docker.com/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-CC292B.svg?style=flat-square&logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![SignalR](https://img.shields.io/badge/Realtime-SignalR-orange.svg?style=flat-square)](https://learn.microsoft.com/aspnet/core/signalr)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices-success.svg?style=flat-square)](#-hệ-thống-tài-liệu-chi-tiết-docs-sitemap)

> **Hệ thống web thương mại điện tử chuyên phục vụ chuỗi cà phê và đồ uống LowLand Coffee**, xây dựng theo kiến trúc **Microservices** trên nền tảng **.NET 8**, phân tầng **`Controller ➔ Service ➔ Repository ➔ DbContext`**, **YARP API Gateway (Port 5000)**, giao diện **ASP.NET Core Razor Pages**, thông báo thời gian thực **SignalR**, hệ quản trị cơ sở dữ liệu **Microsoft SQL Server (Database-per-Service)**, và đóng gói vận hành bằng **Docker Compose**.

---

## ⚡ Khởi Chạy Nhanh (Quick Start)

Yêu cầu máy tính đã cài đặt **Docker Desktop** (hoặc Docker Engine):

```bash
# 1. Khởi động toàn bộ cụm container (SQL Server, Gateway, 5 Microservices, Web Razor)
docker compose up -d --build

# 2. Theo dõi log hoạt động thời gian thực
docker compose logs -f

# 3. Dừng toàn bộ hệ thống
docker compose down
```

* 🌐 **Giao diện Web Razor Pages:** `http://localhost:5000`
* 🔌 **API Gateway Swagger/Reverse Proxy:** `http://localhost:5000`
* 🗄️ **SQL Server 2022 Container:** `localhost:1433` (`sa` / `YourPassword123!`)

---

## 👥 Thành Viên & Phụ Trách Module

| Thành Viên | Vai Trò | Module Phụ Trách | Cổng | Database Riêng |
| :--- | :--- | :--- | :--- | :--- |
| **Tuấn** | Leader / Order & Payment | `Order.API` | `5003` | `Coffee_OrderDB` |
| **Khang** | Gateway & Auth | `ApiGateway` & `Auth.API` | `5000` / `5001` | `Coffee_AuthDB` |
| **Phú** | Catalog & Product | `Product.API` | `5002` | `Coffee_ProductDB` |
| **Trí** | Loyalty & Voucher | `Engagement.API` | `5004` | `Coffee_EngagementDB` |
| **Thành** | Notification & Realtime | `Notification.API` | `5005` | `Coffee_NotificationDB` |

👉 Chi tiết phạm vi từng thành viên: [**`09-team-responsibilities.md`**](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/09-team-responsibilities.md)

---

## 📚 Hệ Thống Tài Liệu Chi Tiết (Docs Sitemap)

Toàn bộ tài liệu được phân tách theo nguyên lý đơn nhiệm (**Single Responsibility**), phục vụ tra cứu chi tiết:

### 🏛️ Kiến Trúc & Công Nghệ
* 📐 [01-architecture.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/01-architecture.md) — Kiến trúc tổng thể Microservices & sơ đồ phân tầng.
* 🛠️ [07-tech-stack.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/07-tech-stack.md) — Bảng Tech Stack, thư viện và cơ sở lựa chọn công nghệ.
* 📁 [08-project-structure.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/08-project-structure.md) — Sơ đồ tổ chức thư mục & cấu trúc phân tầng code.

### ⚙️ Thành Phần Hệ Thống & Đặc Tả
* 🚪 [03-api-gateway.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/03-api-gateway.md) — Cấu hình YARP Reverse Proxy, CORS & Routing table.
* 💻 [04-razor-frontend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/04-razor-frontend.md) — Quy chuẩn Razor Pages, Feature Folders & SignalR Client.
* ⚙️ [05-api-backend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/05-api-backend.md) — Chuẩn 4 tầng `Controller ➔ Service ➔ Repository ➔ DbContext`.

### 🚀 Cài Đặt, Vận Hành & Quy Trình
* 💻 [02-setup-environment.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/02-setup-environment.md) — Hướng dẫn cài đặt Visual Studio, SSMS và môi trường Local.
* 🐳 [06-docker-guide.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/06-docker-guide.md) — Sổ tay vận hành multi-container & port mapping.
* 📝 [10-cli-instructions.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/10-cli-instructions.md) — Bảng tra cứu lệnh CLI nhanh (.NET, EF Core, Docker, Git).
* 🤝 [CONTRIBUTING.md](file:///home/bettercallkhang/Github/LowLand-Coffee/CONTRIBUTING.md) — Quy tắc làm việc nhóm, Git Flow & chuẩn commit.