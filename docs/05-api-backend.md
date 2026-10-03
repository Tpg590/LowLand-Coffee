# 05-api-backend.md - Thiết Kế Phân Tầng Microservices Web API (PRN232)

Tài liệu thiết kế chi tiết kiến trúc phân tầng chuẩn **`Controller ➔ Service ➔ Repository ➔ DbContext`** áp dụng đồng nhất cho cả **5 Microservices Backend** trên nền tảng .NET 8.

---

## 1. Tổng Quan Kiến Trúc Phân Tầng

Mỗi Microservice được xây dựng độc lập với 4 tầng trách nhiệm rõ ràng:

```text
[ HTTP Request từ API Gateway ]
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│ 1. Tầng Controllers (RESTful API Endpoints)                │
│    - Tiếp nhận HTTP Request, Model Validation               │
│    - Gọi tầng Service và trả về ApiResponse<T>             │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Tầng Services (Business Logic & Workflow)                │
│    - Xử lý nghiệp vụ (tính toán giá, mã hóa mật khẩu, OTP)  │
│    - Gọi Repositories và điều phối các thao tác dữ liệu     │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Tầng Repositories (Data Access Layer)                    │
│    - Thực hiện các truy vấn dữ liệu CRUD qua EF Core        │
│    - Tách biệt logic truy vấn khỏi tầng nghiệp vụ           │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Tầng Data / DbContext (Entity Framework Core 8)          │
│    - Quản lý kết nối tới Database riêng của service đó      │
│    - Cấu hình Entity, Fluent API và Migrations              │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Danh Sách 5 Microservices & Database Tương Ứng

| Microservice | Phụ trách | Cấu trúc Project | Database riêng |
| :--- | :---: | :--- | :--- |
| **Auth.API** | **Khang** | `src/Backend/Services/Auth.API` | `Coffee_AuthDB` |
| **Product.API** | **Phú** | `src/Backend/Services/Product.API` | `Coffee_ProductDB` |
| **Order.API** | **Tuấn** | `src/Backend/Services/Order.API` | `Coffee_OrderDB` |
| **Engagement.API** | **Trí** | `src/Backend/Services/Engagement.API` | `Coffee_EngagementDB` |
| **Notification.API** | **Thành** | `src/Backend/Services/Notification.API` | `Coffee_NotificationDB` |

---

## 3. Quy Chuẩn Đặt Tên & Cấu Trúc Mã Nguồn Trong Mỗi Service

Ví dụ cấu trúc trong dự án `Product.API`:
```text
Product.API/
├── Controllers/                 # ProductController.cs, CategoryController.cs
├── Services/                    # IProductService.cs, ProductService.cs
├── Repositories/                # IProductRepository.cs, ProductRepository.cs
├── Data/                        # ProductDbContext.cs
├── DTOs/                        # ProductDto.cs, CreateProductRequest.cs
├── Entities/                    # Product.cs, Category.cs, ProductSize.cs, Topping.cs
├── Program.cs                   # Đăng ký DI, EF Core DbContext, Swagger
└── appsettings.json             # Chuỗi kết nối Database Coffee_ProductDB
```

---

## 4. Chuẩn Hóa Định Dạng Phản Hồi (ApiResponse Wrapper)

Mọi API endpoint trong 5 service đều chuẩn hóa cấu trúc JSON trả về:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Thao tác thành công",
  "data": { },
  "errors": null
}
```