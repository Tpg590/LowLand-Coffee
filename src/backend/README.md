# ☕ LowLand Coffee - Backend Microservices Architecture (PRN232)

Hệ thống Backend được xây dựng theo kiến trúc **Microservices** trên nền tảng **.NET 8 (C# 12)**, tuân thủ mô hình **Database-per-Service** (5 cơ sở dữ liệu độc lập chạy trên SQL Server), tiếp nhận và định tuyến tập trung qua **YARP API Gateway**.

---

## 📂 Cấu trúc thư mục Backend

```text
Backend/
├── CoffeeShop.Backend.sln               # Solution quản lý toàn bộ các dự án Backend
│
├── Gateways/
│   └── ApiGateway/                      # [Khang] YARP Gateway - Port 5000 (Single Entry Point)
│       ├── Program.cs                   # Pipeline đăng ký YARP Reverse Proxy & CORS
│       ├── yarp.json                    # Cấu hình Routing & Clusters sang 5 Services
│       ├── appsettings.json
│       └── Dockerfile
│
└── Services/
    ├── Auth.API/                        # [Khang] Dịch vụ Xác thực & Quản lý Tài khoản (Port 5001)
    │   ├── Controllers/                 # AuthController.cs, UserController.cs
    │   ├── Services/                    # IAuthService.cs, AuthService.cs, ITokenService.cs
    │   ├── Repositories/                # IUserRepository.cs, UserRepository.cs
    │   ├── Data/                        # AuthDbContext.cs -> Database: Coffee_AuthDB
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    │
    ├── Product.API/                     # [Phú] Dịch vụ Thực đơn & Sản phẩm (Port 5002)
    │   ├── Controllers/                 # ProductController.cs, CategoryController.cs
    │   ├── Services/                    # IProductService.cs, ProductService.cs
    │   ├── Repositories/                # IProductRepository.cs, ProductRepository.cs
    │   ├── Data/                        # ProductDbContext.cs -> Database: Coffee_ProductDB
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    │
    ├── Order.API/                       # [Tuấn] Dịch vụ Đặt hàng & Thanh toán (Port 5003)
    │   ├── Controllers/                 # OrderController.cs, PaymentController.cs
    │   ├── Services/                    # IOrderService.cs, IPaymentService.cs
    │   ├── Repositories/                # IOrderRepository.cs, IPaymentRepository.cs
    │   ├── Data/                        # OrderDbContext.cs -> Database: Coffee_OrderDB
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    │
    ├── Engagement.API/                  # [Trí] Dịch vụ Tích điểm, Voucher & Ưu đãi (Port 5004)
    │   ├── Controllers/                 # VoucherController.cs, LoyaltyController.cs
    │   ├── Services/                    # IVoucherService.cs, LoyaltyService.cs
    │   ├── Repositories/                # IVoucherRepository.cs, LoyaltyRepository.cs
    │   ├── Data/                        # EngagementDbContext.cs -> Database: Coffee_EngagementDB
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── Dockerfile
    │
    └── Notification.API/                # [Thành] Dịch vụ Thông báo Realtime & Nhật ký (Port 5005)
        ├── Controllers/                 # NotificationController.cs
        ├── Hubs/                        # NotificationHub.cs (SignalR)
        ├── Services/                    # INotificationService.cs, NotificationService.cs
        ├── Repositories/                # INotificationRepository.cs, NotificationRepository.cs
        ├── Data/                        # NotificationDbContext.cs -> Database: Coffee_NotificationDB
        ├── Program.cs
        ├── appsettings.json
        └── Dockerfile
```

---

## 👥 Bảng phân công thành viên & Database per Service

| STT | Service | Người phụ trách | Vai trò & Nghiệp vụ chính | Database tương ứng | Cổng nội bộ |
| :---: | :--- | :---: | :--- | :--- | :---: |
| **0** | **ApiGateway** | **Khang** | YARP Reverse Proxy, CORS, Auth Validation, Định tuyến tập trung | *Không có DB* | `5000` |
| **1** | **Auth.API** | **Khang** | Đăng ký, đăng nhập JWT, Reset OTP Email, Profile, Quản lý User | `Coffee_AuthDB` | `5001` |
| **2** | **Product.API** | **Phú** | Thực đơn, danh mục, kích cỡ (S/M/L), topping, bật/tắt Còn/Hết hàng | `Coffee_ProductDB` | `5002` |
| **3** | **Order.API** | **Tuấn** | Đặt món, tính tiền, trạng thái đơn hàng, thanh toán tiền mặt & VNPay QR | `Coffee_OrderDB` | `5003` |
| **4** | **Engagement.API** | **Trí** | Quản lý Voucher, Tích điểm, 4 hạng thẻ (Đồng, Bạc, Vàng, Kim Cương) | `Coffee_EngagementDB` | `5004` |
| **5** | **Notification.API** | **Thành** | SignalR Realtime đơn hàng, chuông POS, Broadcast khuyến mãi, Hộp thư | `Coffee_NotificationDB` | `5005` |

---

## 🧭 Cấu hình định tuyến YARP Gateway (`yarp.json`)

Mọi yêu cầu từ Frontend hoặc Client gửi về Gateway (`http://localhost:5000`) sẽ được chuyển tiếp tương ứng:

* `/api/auth/**` ➡️ `Auth.API` (`http://auth_api:8080`)
* `/api/products/**` & `/api/categories/**` ➡️ `Product.API` (`http://product_api:8080`)
* `/api/orders/**` & `/api/payments/**` ➡️ `Order.API` (`http://order_api:8080`)
* `/api/vouchers/**` & `/api/loyalty/**` ➡️ `Engagement.API` (`http://engagement_api:8080`)
* `/api/notifications/**` & `/hubs/notification/**` ➡️ `Notification.API` (`http://notification_api:8080`)

---

## 🚀 Hướng dẫn lệnh khởi tạo dự án cho từng thành viên (CLI)

> Khi bắt đầu code, mở terminal tại thư mục gốc repository (`LowLand-Coffee/`) và chạy đúng 2 dòng lệnh tương ứng:

### 1. [Khang] Khởi tạo `Auth.API` & `ApiGateway`:
```bash
# 1. Tạo Auth.API
dotnet new webapi -n Auth.API -o src/backend/services/Auth.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Auth.API/Auth.API.csproj

# 2. Tạo ApiGateway
dotnet new web -n ApiGateway -o src/backend/gateways/ApiGateway --framework net8.0
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/gateways/ApiGateway/ApiGateway.csproj
```

### 2. [Phú] Khởi tạo `Product.API`:
```bash
dotnet new webapi -n Product.API -o src/backend/services/Product.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Product.API/Product.API.csproj
```

### 3. [Tuấn] Khởi tạo `Order.API`:
```bash
dotnet new webapi -n Order.API -o src/backend/services/Order.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Order.API/Order.API.csproj
```

### 4. [Trí] Khởi tạo `Engagement.API`:
```bash
dotnet new webapi -n Engagement.API -o src/backend/services/Engagement.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Engagement.API/Engagement.API.csproj
```

### 5. [Thành] Khởi tạo `Notification.API`:
```bash
dotnet new webapi -n Notification.API -o src/backend/services/Notification.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Notification.API/Notification.API.csproj
```

👉 Xem thêm các lệnh cài đặt NuGet, EF Core Migrations tại: [**`10-cli-instructions.md`**](file:///home/bettercallkhang/Github/LowLand-Coffee/docs/10-cli-instructions.md)
