# 🔐 LowLand Coffee - Auth.API Documentation Hub

> **Dịch vụ Xác thực, Phân quyền & Quản lý Người dùng (Authentication & User Management)**  
> **Người phụ trách:** **Khang**  
> **Môn học:** PRN232 - Microservices Architecture with .NET 8

---

## 📌 1. Giới thiệu Tổng quan Dịch vụ

`Auth.API` là dịch vụ xương sống (Core Microservice) chịu trách nhiệm toàn bộ các vấn đề bảo mật, quản lý danh tính và phân quyền người dùng trong hệ sinh thái chuỗi quán cà phê **LowLand Coffee**. Dịch vụ được xây dựng độc lập theo nguyên tắc **Database-per-Service**, quản lý cơ sở dữ liệu riêng biệt `Coffee_AuthDB` trên Microsoft SQL Server 2022.

Mọi yêu cầu từ ứng dụng Frontend (Razor Pages) hoặc Mobile Client gửi tới hệ thống đều có thể đi qua **YARP API Gateway** (Port 5000) hoặc kết nối trực tiếp cổng nội bộ của Auth Service (Port 5001) trong giai đoạn phát triển.

### ⚙️ Thông số Kỹ thuật & Môi trường
- **Nền tảng:** ASP.NET Core Web API (.NET 8.0, C# 12)
- **Cơ sở dữ liệu:** Microsoft SQL Server 2022 (`Coffee_AuthDB`)
- **ORM:** Entity Framework Core 8.0 (Code First / SQL Schema Mapping)
- **Mã hóa & Băm mật khẩu:** `BCrypt.Net-Next` (Salt tự động)
- **Cơ chế Xác thực:** JSON Web Token (JWT Bearer - HMAC-SHA256)
- **Cổng kết nối nội bộ (Host):** `http://localhost:5001`
- **Cổng kết nối Docker Container:** `8080` (Mạng bridge: `lowland-network`)
- **Tuyến định tuyến qua Gateway:** `/api/auth/**` và `/api/users/**`

---

## 🏛️ 2. Kiến trúc Phân tầng (4-Tier Clean Architecture)

Dịch vụ áp dụng mô hình kiến trúc phân tầng 4 lớp tiêu chuẩn, phân định ranh giới trách nhiệm rõ ràng:

```text
[ HTTP Request từ API Gateway / Client ]
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│ 1. Tầng Controllers (Presentation Layer)                    │
│    - Tiếp nhận HTTP Request, Data Annotations Validation    │
│    - Định tuyến chuẩn RESTful, đóng gói ApiResponse<T>      │
│    - Controllers/AuthController.cs, UserController.cs       │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Tầng Services (Business Logic Layer)                     │
│    - Xử lý nghiệp vụ, băm mật khẩu BCrypt, sinh mã OTP      │
│    - Quản lý JWT Token & cơ chế Token Rotation              │
│    - IAuthService, IUserService, ITokenService, ITokenStore │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Tầng Repositories (Data Access Layer)                    │
│    - Tách biệt logic truy vấn DB với Entity Framework Core  │
│    - Hỗ trợ lọc tìm kiếm, phân trang và Xóa cứng            │
│    - IUserRepository, IRoleRepository                       │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Tầng Data / DbContext (Infrastructure Layer)             │
│    - AuthDbContext.cs cấu hình Fluent API chuẩn hóa         │
│    - Ánh xạ đúng 4 bảng cơ sở dữ liệu theo DBML gốc         │
│    - auth_roles, auth_users, auth_permissions,              │
│      auth_role_permissions                                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📂 3. Cấu trúc Thư mục Mã nguồn

```text
src/backend/services/Auth.API/
├── Controllers/                 # RESTful Endpoints Controllers
│   ├── AuthController.cs        # Endpoints đăng ký, đăng nhập, OTP, Profile
│   └── UserController.cs        # Endpoints quản trị Admin (Users, Roles)
│
├── Services/                    # Business Logic Services
│   ├── Interfaces/              # IAuthService, IUserService, ITokenService, ...
│   ├── AuthService.cs           # Nghiệp vụ đăng ký, đăng nhập, đổi mật khẩu
│   ├── UserService.cs           # Nghiệp vụ quản lý người dùng của Admin
│   ├── TokenService.cs          # Sinh JWT Access Token & Refresh Token
│   ├── InMemoryTokenStore.cs    # Lưu trữ Refresh Token & OTP an toàn trong bộ nhớ
│   └── EmailService.cs          # Gửi mã OTP khôi phục mật khẩu (Log dev)
│
├── Repositories/                # Data Access Repositories
│   ├── Interfaces/              # IUserRepository, IRoleRepository
│   ├── UserRepository.cs        # Truy vấn người dùng, tìm kiếm, phân trang, xóa cứng
│   └── RoleRepository.cs        # Truy vấn danh mục vai trò
│
├── Data/                        # EF Core Database Context
│   └── AuthDbContext.cs         # Fluent API mapping 4 bảng SQL Server
│
├── Entities/                    # Entity Models
│   ├── Role.cs                  # Bảng auth_roles
│   ├── User.cs                  # Bảng auth_users
│   ├── Permission.cs            # Bảng auth_permissions
│   └── RolePermission.cs        # Bảng auth_role_permissions
│
├── DTOs/                        # Data Transfer Objects
│   ├── ApiResponse.cs           # Chuẩn hóa Response JSON đồng nhất
│   ├── PagedResult.cs           # Chuẩn hóa cấu trúc phân trang
│   ├── AuthDtos.cs              # Request/Response DTOs cho phân hệ Auth & Profile
│   └── UserDtos.cs              # Request/Response DTOs cho phân hệ Admin User
│
├── docs/                        # 📚 THƯ MỤC TÀI LIỆU HỆ THỐNG
│   ├── README.md                # [Tài liệu hiện tại] Tổng quan kiến trúc & chỉ mục
│   ├── 01-features.md           # Danh sách chi tiết các chức năng đã hoàn thành
│   ├── 02-api-endpoints.md      # Đặc tả chi tiết từng endpoint, ràng buộc fields & mã lỗi
│   ├── 03-business-rules.md     # Quy tắc nghiệp vụ (Business Rules - BR)
│   └── 04-user-guide.md         # Hướng dẫn cài đặt, khởi chạy & kiểm thử
│
├── init_auth_db.sql             # Script khởi tạo 4 bảng Database riêng cho Auth
├── seed_data.sql                # Script nạp sẵn tài khoản Admin, Staff, Customer mẫu
├── Auth.API.http                # Kịch bản kiểm thử API tự động với REST Client
├── appsettings.json             # Cấu hình ConnectionString & JWT Secret
└── Program.cs                   # Khởi tạo DI container, JWT Bearer & Swagger UI
```

---

## 📚 4. Hệ thống Tài liệu Chuyên sâu

Để thuận tiện tra cứu cho từng mục đích phát triển và bàn giao, tài liệu chi tiết được chia thành 4 chuyên đề độc lập:

### 🌟 [01-features.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/docs/01-features.md) - Danh sách Chức năng
- Liệt kê đầy đủ 14 tính năng nghiệp vụ đã hoàn thành.
- Phân loại rõ theo từng đối tượng sử dụng: Khách hàng (Public/Customer), Cá nhân (Profile), và Quản trị viên (Admin).

### 📡 [02-api-endpoints.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/docs/02-api-endpoints.md) - Danh sách & Đặc tả RESTful Endpoints
- Bảng tổng hợp toàn bộ 14 API Endpoints.
- Ràng buộc chi tiết cho từng trường dữ liệu đầu vào (Data types, Max/Min length, Regex, Required).
- **Mã lỗi cụ thể cho từng endpoint:** Liệt kê các kịch bản lỗi `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found` kèm nội dung thông báo lỗi thực tế.
- JSON Request Body và Response mẫu cho từng trường hợp.

### ⚖️ [03-business-rules.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/docs/03-business-rules.md) - Quy tắc Nghiệp vụ (Business Rules)
- Chuẩn hóa 8 quy tắc nghiệp vụ cốt lõi từ **BR-AUTH-01** đến **BR-AUTH-08**:
  - Tiêu chuẩn mật khẩu và thuật toán băm BCrypt.
  - Vòng đời JWT Access Token (60 phút) và Refresh Token (7 ngày).
  - Cơ chế **Token Rotation** phòng chống Replay Attack.
  - Mã OTP ngẫu nhiên 6 số, hiệu lực 15 phút, chống dò quét tài khoản.
  - Ma trận phân quyền 3 vai trò (`Customer`, `Staff`, `Admin`).
  - Cơ chế khóa tài khoản thu hồi phiên đăng nhập tức thì.
  - Cơ chế **Xóa cứng (Hard Delete)** trực tiếp trong database.

### 🚀 [04-user-guide.md](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/docs/04-user-guide.md) - Hướng dẫn Vận hành & Kiểm thử
- Các bước khởi động cơ sở dữ liệu SQL Server với Docker Compose.
- Lệnh chạy service cục bộ với .NET CLI (`dotnet run`).
- Hướng dẫn kiểm thử tương tác qua giao diện Swagger UI (`http://localhost:5001/swagger`).
- Hướng dẫn kiểm thử tự động với tệp [Auth.API.http](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/services/Auth.API/Auth.API.http).
- Hướng dẫn xử lý các sự cố thường gặp (Troubleshooting: lỗi kết nối 4060, lỗi 401 token,...).
