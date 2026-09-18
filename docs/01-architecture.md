# 01-architecture.md - Kiến Trúc Hệ Thống LowLand Coffee

Tài liệu này mô tả chi tiết kiến trúc tổng thể, sơ đồ phân tầng, luồng dữ liệu và cơ chế giao tiếp giữa các thành phần trong hệ thống **LowLand Coffee** (.NET 8 Web API, YARP Gateway, ASP.NET Core Razor Pages, SQL Server và Docker).

---

## 1. Sơ Đồ Kiến Trúc Tổng Thể (High-Level Architecture)

Toàn bộ client bên ngoài chỉ tương tác qua **API Gateway (YARP)** hoặc truy cập giao diện từ **Web App (Razor Pages)**. Hệ thống backend và database được cô lập trong mạng nội bộ của Docker:

```text
[ Trình Duyệt / Người Dùng (Browser) ]
                 │
                 │ HTTP (Port 5000)
                 ▼
        ┌──────────────────┐
        │  LowLand WebApp  │ (ASP.NET Core Razor Pages .NET 8)
        └────────┬─────────┘
                 │
                 │ HTTP / REST (Port 8080)
                 ▼
        ┌──────────────────┐
        │   API Gateway    │ (YARP Reverse Proxy - Port 8080)
        │  - Reverse Proxy │
        │  - Central CORS  │
        │  - Route Map     │
        └────────┬─────────┘
                 │
                 │ HTTP (Mạng nội bộ Docker / Localhost Port 5001)
                 ▼
        ┌──────────────────┐
        │  LowLandCoffee   │ (.NET 8 Web API)
        │       .Api       │ - Controllers (Categories, Products, Orders)
        │                  │ - Repository Pattern & Unit of Work
        └────────┬─────────┘
                 │
                 │ TCP Connection (Port 1433) / EF Core 8
                 ▼
        ┌──────────────────┐
        │ Microsoft SQL    │ (Database: LowLandCoffeeDb)
        │   Server 2022    │ - Quản lý bằng SSMS
        └──────────────────┘
```

---

## 2. Các Thành Phần Trong Hệ Thống (System Components)

### 2.1. Frontend Client (`LowLandCoffee.Web`)
* **Nền tảng:** ASP.NET Core Razor Pages (.NET 8).
* **Trách nhiệm:**
  * Render giao diện Server-side rendering (SSR), tối ưu SEO và tốc độ tải trang.
  * Đóng gói request HTTP qua `HttpClient` tập trung gửi về API Gateway (Port 8080).
  * Quản lý giỏ hàng, thông tin đặt hàng qua Session và Cookies.

### 2.2. API Gateway (`LowLandCoffee.ApiGateway`)
* **Nền tảng:** YARP (Yet Another Reverse Proxy) trên nền .NET 8.
* **Trách nhiệm:**
  * **Reverse Proxy & Routing:** Làm điểm tiếp nhận duy nhất (Single Entry Point), định tuyến các request từ Frontend Web tới đúng Backend API.
  * **CORS Management:** Thiết lập chính sách CORS tập trung cho các Web Client.
  * **Network Isolation:** Ẩn toàn bộ địa chỉ IP và port nội bộ của các services phía sau.

### 2.3. Backend API (`LowLandCoffee.Api`)
* **Nền tảng:** ASP.NET Core Web API (.NET 8).
* **Trách nhiệm:**
  * Xử lý toàn bộ logic nghiệp vụ (quản lý món, menu, đơn hàng, giỏ hàng).
  * Triển khai kiến trúc phân tầng kết hợp **Repository Pattern & Unit of Work**.
  * Cung cấp Swagger OpenAPI để kiểm thử API.

### 2.4. Cơ Sở Dữ Liệu (`SQL Server 2022`)
* **Engine:** Microsoft SQL Server 2022 (chạy container trên Docker hoặc bản cài đặt local).
* **Database chính:** `LowLandCoffeeDb`.
* **Công cụ quản trị:** SQL Server Management Studio (SSMS) / Azure Data Studio.

---

## 3. Luồng Xử Lý Nghiệp Vụ Chính (Data Flows)

### 3.1. Luồng Lấy Danh Sách Thực Đơn (Menu Query Flow)
1. Người dùng truy cập trang Thực đơn: `GET http://localhost:5000/Menu`.
2. `LowLandCoffee.Web` gửi request qua `HttpClient`: `GET http://localhost:8080/api/products?categoryId=1`.
3. YARP Gateway tiếp nhận tại port `8080`, kiểm tra định tuyến và forward request sang Backend API (`http://lowland-api:8080/api/products?categoryId=1` trong Docker hoặc `http://localhost:5001` trên local).
4. `LowLandCoffee.Api` xử lý qua `ProductsController` $\rightarrow$ `UnitOfWork` $\rightarrow$ `ProductRepository` $\rightarrow$ truy vấn `AppDbContext` lấy dữ liệu từ `SQL Server`.
5. Kết quả bọc trong `ApiResponse<T>` được trả về Gateway $\rightarrow$ Frontend Web $\rightarrow$ Render giao diện HTML trả về trình duyệt người dùng.

### 3.2. Luồng Đặt Hàng (Checkout Flow)
1. Người dùng bấm "Đặt Hàng" trên giao diện giỏ hàng.
2. Web gửi request `POST http://localhost:8080/api/orders` kèm thông tin khách hàng và danh sách món.
3. Gateway forward request đến Backend API.
4. Backend API thực hiện lưu đơn hàng vào `Orders` và `OrderItems` trong database thông qua transaction của `UnitOfWork`.
5. Backend trả mã đơn hàng và thông báo thành công về cho Web.

---

## 4. Cấu Hình Mạng Container (Docker Networking)

Trong môi trường Docker Compose, toàn bộ container được kết nối thông qua một mạng bridge duy nhất (`lowland-network`):

* **Internal DNS:** Các container giao tiếp với nhau bằng tên service trong `docker-compose.yml` (ví dụ: `http://lowland-api:8080`), không dùng `localhost`.
* **External Exposure:**
  * **Port 5000:** Mở cho trình duyệt truy cập giao diện Web Razor Pages.
  * **Port 8080:** Mở cho truy cập API Gateway.
  * **Port 1433:** SQL Server mở ở môi trường Development để kết nối bằng SSMS.