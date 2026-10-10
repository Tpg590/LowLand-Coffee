# 04-razor-frontend.md - Thiết Kế & Quy Chuẩn Frontend Web (PRN232)

Tài liệu này quy định kiến trúc tầng giao diện **ASP.NET Core Razor Pages** (.NET 8), cơ chế gọi API Gateway, quản lý trạng thái giỏ hàng, lắng nghe thông báo SignalR và các quy chuẩn thiết kế UI/UX theo mô hình Feature Folders cho 4 nhóm tác nhân (Guest, Customer, Staff, Admin).

---

## 1. Nguyên Tắc Cốt Lõi (Architecture Principles)

* **Cổng giao tiếp duy nhất:** Toàn bộ giao tiếp mạng từ Web Frontend bắt buộc gửi về **YARP API Gateway** (`http://localhost:8080`). Trình duyệt không gọi trực tiếp cổng nội bộ của backend API.
* **Mô hình Feature Folders:** Tổ chức các trang `.cshtml` & `.cshtml.cs` theo từng cụm chức năng nghiệp vụ độc lập.
* **Kết nối thời gian thực (SignalR):** Nhận thông báo trạng thái đơn hàng (Customer) và chuông báo đơn mới (Staff) qua WebSocket trung chuyển từ ApiGateway.
* **Tách biệt tầng HTTP Service:** Mỗi Microservice Backend có một Typed `HttpClient` Service riêng biệt trong thư mục `Services/`.

---

## 2. Cấu Trúc Thư Mục Feature Folders (`CoffeeShop.WebRazorPages`)

```text
src/Frontend/CoffeeShop.WebRazorPages/
├── Pages/                               # Các trang giao diện Razor
│   ├── Index.cshtml                     # Trang chủ (Banner, món nổi bật, ưu đãi)
│   ├── Auth/                            # [Khang] Login, Register, Forgot/Reset Password, Profile
│   ├── Menu/                            # [Phú] Thực đơn, Tìm kiếm, Chi tiết món (Size/Topping)
│   ├── Orders/                          # [Tuấn] Cart, Checkout, Tracking Realtime, Lịch sử
│   ├── Customer/                        # [Trí] Thẻ tích điểm thành viên, Kho Voucher
│   ├── Notifications/                   # [Thành] Hộp thư cá nhân (Đã đọc / Chưa đọc / Xóa)
│   ├── Staff/                           # [Staff] POS Orders board, Xác nhận tiền mặt
│   ├── Admin/                           # [Admin] Dashboard, Quản lý Menu, Voucher, User, Broadcast
│   └── Shared/
│       ├── _Layout.cshtml               # Khung layout tổng thể
│       ├── _Header.cshtml               # Navbar & Chuông thông báo
│       └── _Footer.cshtml               # Chân trang
│
├── Services/                            # Typed HttpClients gọi qua ApiGateway
│   ├── IAuthHttpService.cs & AuthHttpService.cs
│   ├── IProductHttpService.cs & ProductHttpService.cs
│   ├── IOrderHttpService.cs & OrderHttpService.cs
│   ├── IEngagementHttpService.cs & EngagementHttpService.cs
│   └── INotificationHttpService.cs & NotificationHttpService.cs
│
├── ViewModels/                          # Data Models hứng JSON từ API
│   ├── Auth/, Product/, Order/, Engagement/, Notification/
│├── wwwroot/                             # CSS, JS (signalr.js), Images, Bootstrap 5
├── appsettings.json                     # Cấu hình "GatewayUrl": "http://localhost:8080"
└── Program.cs                           # Đăng ký HttpClient, Cookie Auth, Session
```

---

## 3. Cơ Chế Đăng Ký Typed HttpClients Trong `Program.cs`

```csharp
var gatewayUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? "http://localhost:8080";

// Đăng ký các HttpClient Services hướng về Gateway
builder.Services.AddHttpClient<IAuthHttpService, AuthHttpService>(c => c.BaseAddress = new Uri(gatewayUrl));
builder.Services.AddHttpClient<IProductHttpService, ProductHttpService>(c => c.BaseAddress = new Uri(gatewayUrl));
builder.Services.AddHttpClient<IOrderHttpService, OrderHttpService>(c => c.BaseAddress = new Uri(gatewayUrl));
builder.Services.AddHttpClient<IEngagementHttpService, EngagementHttpService>(c => c.BaseAddress = new Uri(gatewayUrl));
builder.Services.AddHttpClient<INotificationHttpService, NotificationHttpService>(c => c.BaseAddress = new Uri(gatewayUrl));
```

---

## 4. Tích Hợp SignalR Realtime Client Trong Razor Pages

Tại file `wwwroot/js/notification-client.js`:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:8080/hubs/notification")
    .withAutomaticReconnect()
    .build();

// Lắng nghe sự kiện thông báo đơn hàng
connection.on("ReceiveOrderNotification", function (message) {
    // Hiển thị toast thông báo hoặc phát âm thanh chuông cảnh báo cho Nhân viên/Khách
    showToastNotification(message);
});

connection.start().catch(err => console.error("SignalR Connection Error: ", err));
```