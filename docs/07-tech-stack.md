# 07-tech-stack.md - Tech Stack & Công Cụ Kỹ Thuật (PRN232)

Tài liệu này tổng hợp toàn bộ công nghệ, thư viện, công cụ và lý do lựa chọn kỹ thuật được sử dụng trong hệ thống **LowLand Coffee**.

---

## 1. Bảng Tổng Hợp Công Nghệ (Technology Matrix)

| Phân Vùng | Công Nghệ / Thư Viện | Phiên Bản | Mục Đích / Vai Trò |
| :--- | :--- | :--- | :--- |
| **Backend Framework** | [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core) | `.NET 8` (C# 12) | Xây dựng 5 Microservices độc lập theo kiến trúc phân tầng chuẩn |
| **API Gateway** | [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/) | `.NET 8` | Điểm tiếp nhận duy nhất (Port 5000), Routing, Centralized CORS, Token Validation, WebSocket Forwarding |
| **Frontend Web** | [ASP.NET Core Razor Pages](https://learn.microsoft.com/aspnet/core/razor-pages/) | `.NET 8` | Giao diện Web SSR, tổ chức theo Feature Folders cho 4 Actor (Guest, Customer, Staff, Admin) |
| **Realtime Engine** | [ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/signalr) | `.NET 8` | Đẩy thông báo đơn hàng, cập nhật trạng thái chế biến & chuông POS tức thì |
| **ORM / Data Access** | [Entity Framework Core](https://learn.microsoft.com/ef/core/) | `8.0.x` | Tương tác cơ sở dữ liệu qua Repository Pattern & Unit of Work, Migration tự động |
| **Database Engine** | [Microsoft SQL Server](https://www.microsoft.com/sql-server) | `2022` | Quản trị 5 cơ sở dữ liệu riêng biệt theo mô hình Database-per-Service |
| **Authentication** | [JWT (JSON Web Token)](https://jwt.io/) | `System.IdentityModel.Tokens.Jwt` | Xác thực & phân quyền Bearer Token cho các Actors |
| **Payment Integration**| [VNPay](https://vnpay.vn/) & Cash | API SDK / Sandbox | Thanh toán trực tuyến qua VNPAY-QR và phương thức thanh toán tiền mặt tại quầy |
| **Containerization** | [Docker & Docker Compose](https://www.docker.com/) | Compose v2 (v4.25+) | Đóng gói toàn bộ cụm dịch vụ và cơ sở dữ liệu chạy môi trường đồng nhất |
| **API Documentation** | [Swagger / OpenAPI (Swashbuckle)](https://swagger.io/) | `6.5.x` | Tài liệu hóa & kiểm thử trực tiếp các REST API endpoints |
| **Version Control** | [Git & GitHub](https://git-scm.com/) | `2.40+` | Quản lý mã nguồn theo mô hình Git Flow rút gọn |

---

## 2. Chi Tiết Lựa Chọn Kỹ Thuật (Architecture & Tech Rationales)

### 2.1. Nền tảng .NET 8 & C# 12
* **Hiệu năng cao:** .NET 8 mang lại tốc độ xử lý nhanh, tối ưu hóa bộ nhớ và hỗ trợ native AOT.
* **Đồng nhất ngôn ngữ:** Toàn bộ hệ thống từ Gateway, Backend APIs, đến Frontend Razor Pages đều sử dụng C# và hệ sinh thái .NET.
* **Hỗ trợ Long-Term Support (LTS):** Đảm bảo tính ổn định và bảo mật lâu dài.

### 2.2. API Gateway với YARP
* **Hiệu suất & Tích hợp:** YARP được phát triển bởi Microsoft, tối ưu riêng cho ASP.NET Core.
* **Bảo mật tập trung:** Xác thực token, áp dụng CORS và chuyển hướng request về đúng Microservice mà không phơi lộ cổng nội bộ.
* **WebSocket Streaming:** Hỗ trợ passthrough mượt mà cho kết nối SignalR Hub.

### 2.3. Frontend Razor Pages & SignalR
* **Server-Side Rendering (SSR):** Tối ưu SEO, thời gian phản hồi trang nhanh, dễ bảo trì theo từng Feature Folder.
* **SignalR Realtime Hub:** Cập nhật trạng thái đơn hàng (Đang pha chế, Đã xong, Đã thanh toán) tức thì tới màn hình POS của Staff và trang theo dõi của Customer.

### 2.4. Cơ sở dữ liệu SQL Server (Database per Service)
* **Tính cô lập (Isolation):** Mỗi Microservice sở hữu một Database độc lập, tránh phụ thuộc chéo ở tầng dữ liệu.
* **Bảo toàn dữ liệu:** Dễ dàng backup, restore hoặc scale riêng biệt từng dịch vụ theo tải lượng thực tế.

---

## 3. Liên Kết Tài Liệu Liên Quan
* 🏛️ [01-architecture.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/01-architecture.md) — Kiến trúc tổng thể hệ thống
* 🚪 [03-api-gateway.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/03-api-gateway.md) — Cấu hình YARP Reverse Proxy
* 💻 [04-razor-frontend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/04-razor-frontend.md) — Quy chuẩn Razor Frontend
* ⚙️ [05-api-backend.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/05-api-backend.md) — Quy chuẩn 4 tầng Microservices
* 🐳 [06-docker-guide.md](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/06-docker-guide.md) — Hướng dẫn Docker & Container
