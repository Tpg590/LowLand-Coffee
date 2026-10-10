# 🌐 Coffee Shop - Frontend Architecture (PRN232)

Dự án Frontend được xây dựng trên nền tảng **ASP.NET Core Razor Pages (.NET 8)** theo mô hình **Feature Folders**, phục vụ cả 4 nhóm tác nhân (Guest, Customer, Staff, Admin) và kết nối với Backend thông qua **ApiGateway**.

---

## 📂 Cấu trúc thư mục chi tiết

```text
Frontend/
└── CoffeeShop.WebRazorPages/            # Dự án Web ASP.NET Core Razor Pages (.NET 8)
    ├── wwwroot/                         # Tài nguyên tĩnh
    │   ├── css/                         # Custom CSS, Themes
    │   ├── js/                          # Javascript, signalr.min.js (Realtime notification)
    │   ├── lib/                         # Bootstrap 5, FontAwesome, jQuery
    │   └── images/                      # Logo, banner, ảnh đồ uống
    │
    ├── Pages/                           # Giao diện UI + PageModel (.cshtml & .cshtml.cs)
    │   ├── Shared/                      # Thành phần layout dùng chung
    │   │   ├── _Layout.cshtml           # Khung layout tổng thể
    │   │   ├── _Header.cshtml           # Navbar, chuông thông báo & dropdown tài khoản
    │   │   ├── _Footer.cshtml           # Chân trang
    │   │   └── _NotificationBell.cshtml # Component hiển thị danh sách thông báo realtime
    │   │
    │   ├── Index.cshtml                 # Trang chủ: Banner, món bán chạy, khuyến mãi
    │   │
    │   ├── Auth/                        # [Khang] Xác thực & Tài khoản
    │   │   ├── Login.cshtml             # Đăng nhập
    │   │   ├── Register.cshtml          # Đăng ký
    │   │   ├── ForgotPassword.cshtml    # Quên mật khẩu (gửi OTP qua Email)
    │   │   ├── ResetPassword.cshtml     # Nhập OTP & Đổi mật khẩu mới
    │   │   └── Profile.cshtml           # Thông tin cá nhân, Avatar, Địa chỉ & Đổi mật khẩu
    │   │
    │   ├── Menu/                        # [Phú] Thực đơn đồ uống
    │   │   ├── Index.cshtml             # Danh sách thực đơn, tìm kiếm & bộ lọc
    │   │   └── Details.cshtml           # Chi tiết món (chọn Size S/M/L, Topping)
    │   │
    │   ├── Orders/                      # [Tuấn] Giỏ hàng, Đặt món & Theo dõi
    │   │   ├── Cart.cshtml              # Xem giỏ hàng & cập nhật số lượng
    │   │   ├── Checkout.cshtml          # Đặt hàng, áp voucher, chọn VNPay QR/Tiền mặt
    │   │   ├── Tracking.cshtml          # Theo dõi đơn realtime (Đang pha, Hoàn thành...)
    │   │   └── History.cshtml           # Lịch sử các đơn cá nhân đã đặt
    │   │
    │   ├── Customer/                    # [Trí] Thành viên & Ưu đãi
    │   │   ├── Loyalty.cshtml           # Điểm tích lũy, 4 hạng thẻ (Đồng, Bạc, Vàng, Kim Cương)
    │   │   └── Vouchers.cshtml          # Kho voucher ưu đãi hiện có
    │   │
    │   ├── Notifications/               # [Thành] Hộp thư thông báo cá nhân
    │   │   └── Index.cshtml             # Xem danh sách thông báo, đánh dấu Đã đọc / Xóa
    │   │
    │   ├── Staff/                       # [Nhân viên / Thu ngân] Màn hình POS & Bếp
    │   │   └── Orders/
    │   │       ├── Index.cshtml         # Danh sách đơn realtime (chuông báo khi có đơn mới)
    │   │       └── Details.cshtml       # Chi tiết đơn, xác nhận thanh toán tiền mặt
    │   │
    │   └── Admin/                       # [Quản trị viên] Trang Quản lý
    │       ├── Dashboard.cshtml         # Thống kê doanh thu & số lượng đơn
    │       ├── Products/                # CRUD Món ăn, Size, Topping, Bật/Tắt còn/hết hàng
    │       ├── Categories/              # CRUD Danh mục thực đơn
    │       ├── Vouchers/                # CRUD Mã giảm giá, điều kiện áp dụng
    │       ├── Users/                   # Quản lý tài khoản khách & Cấp account nhân viên
    │       └── Notifications/           # [Thành] Broadcast thông báo khuyến mãi & Xem Logs
    │           ├── Broadcast.cshtml     # Gửi thông báo hàng loạt tới khách
    │           └── Logs.cshtml          # Xem lịch sử nhật ký gửi thông báo
    │
    ├── Services/                        # Tầng HttpClients gửi request qua ApiGateway
    │   ├── IAuthHttpService.cs & AuthHttpService.cs
    │   ├── IProductHttpService.cs & ProductHttpService.cs
    │   ├── IOrderHttpService.cs & OrderHttpService.cs
    │   ├── IEngagementHttpService.cs & EngagementHttpService.cs
    │   └── INotificationHttpService.cs & NotificationHttpService.cs
    │
    ├── ViewModels/                      # Data Models hứng JSON từ API trả về
    │   ├── Auth/
    │   ├── Product/
    │   ├── Order/
    │   ├── Engagement/
    │   └── Notification/
    │
    ├── appsettings.json                 # Cấu hình GatewayUrl (http://localhost:5000)
    ├── Program.cs                       # Đăng ký DI, HttpClient, Session, Authentication Cookie
    └── Dockerfile                       # Đóng gói Docker container cho Web Frontend
```

---

## 👥 Phân quyền theo Actor

| Actor | Quyền truy cập Pages | Ghi chú |
| :--- | :--- | :--- |
| **Khách vãng lai (Guest)** | `/Index`, `/Menu/**`, `/Auth/Login`, `/Auth/Register`, `/Auth/ForgotPassword` | Xem thực đơn, tìm kiếm món, xem khuyến mãi. Bắt buộc đăng nhập khi checkout. |
| **Khách hàng (Customer)** | Toàn bộ trang Public + `/Orders/**`, `/Customer/**`, `/Auth/Profile`, `/Notifications/**` | Đặt món, chọn Size/Topping, thanh toán VNPay, tích điểm, nhận SignalR realtime. |
| **Nhân viên (Staff)** | `/Staff/Orders/**` | Màn hình POS nhận đơn có chuông báo, cập nhật pha chế, xác nhận tiền mặt. |
| **Quản trị viên (Admin)** | `/Admin/**` | Toàn quyền cấu hình thực đơn, voucher, người dùng, broadcast thông báo. |

---

## 🚀 Hướng dẫn khởi tạo & Chạy dự án (CLI)

> Chạy từ thư mục gốc repository (`LowLand-Coffee/`):

### 1. Khởi tạo Dự án Web Frontend:
```bash
# 1. Tạo project Web Razor Pages:
dotnet new webapp -n CoffeeShop.WebRazorPages -o src/frontend/CoffeeShop.WebRazorPages --framework net8.0

# 2. Thêm project vào Solution tổng:
dotnet sln src/CoffeeShop.sln add src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj --solution-folder "frontend"
```

### 2. Chạy dự án:
```bash
dotnet run --project src/frontend/CoffeeShop.WebRazorPages/CoffeeShop.WebRazorPages.csproj
```
