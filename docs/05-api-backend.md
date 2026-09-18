# 05-api-backend.md - Thiết Kế Backend Web API & Repository Pattern

Tài liệu thiết kế chi tiết kiến trúc, mô hình dữ liệu, **Repository Pattern**, **Unit of Work** và các quy chuẩn triển khai cho dịch vụ `LowLandCoffee.Api` (.NET 8).

---

## 1. Tổng Quan & Tech Stack

* **Runtime:** .NET 8 (C# 12) Web API
* **ORM:** Entity Framework Core 8
* **Database:** Microsoft SQL Server 2022
* **Kiến trúc:** Clean Layered Architecture kết hợp **Repository Pattern & Unit of Work**
* **Tài liệu hóa:** Swagger / OpenAPI
* **Mục tiêu:** Cung cấp RESTful API xử lý toàn bộ logic nghiệp vụ (quản lý món, menu, đơn hàng, danh mục) cho hệ thống LowLand Coffee.

---

## 2. Cấu Trúc Thư Mục Chuẩn (`src/LowLandCoffee.Api`)

```text
src/LowLandCoffee.Api/
├── Controllers/                         # RESTful API Endpoints
│   ├── CategoriesController.cs          # Quản lý danh mục
│   ├── ProductsController.cs            # Quản lý sản phẩm món
│   └── OrdersController.cs              # Quản lý đơn đặt hàng
├── Core/                                # Domain Entities & Interfaces
│   ├── Entities/
│   │   ├── BaseEntity.cs                # Id, CreatedAt, UpdatedAt, IsDeleted
│   │   ├── Category.cs                  # Tên, mô tả, ảnh, thứ tự
│   │   ├── Product.cs                   # Tên, giá, ảnh, trạng thái, danh mục
│   │   ├── Order.cs                     # Khách hàng, địa chỉ, tổng tiền, trạng thái
│   │   └── OrderItem.cs                 # Số lượng, đơn giá, sản phẩm
│   └── Interfaces/
│       ├── IGenericRepository.cs        # Định nghĩa CRUD generic
│       ├── IProductRepository.cs        # Truy vấn sản phẩm mở rộng
│       └── IUnitOfWork.cs               # Quản lý transaction và các repositories
├── Infrastructure/                      # Tầng triển khai dữ liệu & Database
│   ├── Data/
│   │   ├── AppDbContext.cs              # EF Core DbContext & Fluent API
│   │   └── DbInitializer.cs             # Khởi tạo dữ liệu đồ uống mẫu
│   └── Repositories/
│       ├── GenericRepository.cs         # Triển khai CRUD generic với EF Core
│       ├── ProductRepository.cs         # Triển khai truy vấn sản phẩm
│       └── UnitOfWork.cs                # Triển khai Commit/Rollback SaveChangesAsync
├── Application/                         # Tầng ứng dụng & DTOs
│   ├── DTOs/
│   │   ├── ApiResponse.cs               # Wrapper chuẩn hóa JSON trả về
│   │   ├── ProductDtos.cs               # ProductDto, CreateProductDto, UpdateProductDto
│   │   └── CategoryDtos.cs              # CategoryDto, CreateCategoryDto
│   └── Services/                        # Business logic services
├── appsettings.json                     # Chuỗi kết nối DB và cấu hình
└── Program.cs                           # Cấu hình DI, DbContext, Swagger
```

---

## 3. Chi Tiết Triển Khai Repository Pattern & Unit of Work

### 3.1. `IGenericRepository<T>` Interface
```csharp
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetByIdAsync(int id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

### 3.2. `IUnitOfWork` Interface
```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<Order> Orders { get; }
    IGenericRepository<OrderItem> OrderItems { get; }

    Task<int> CompleteAsync();
}
```

### 3.3. Đăng ký Dependency Injection trong `Program.cs`
```csharp
// Đăng ký EF Core với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Repositories & Unit of Work
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## 4. Chuẩn Hóa Định Dạng Phản Hồi (ApiResponse Wrapper)

Mọi API endpoint đều trả về cấu trúc JSON thống nhất:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Lấy danh sách sản phẩm thành công",
  "data": [
    {
      "id": 1,
      "name": "Phin Sữa Đá LowLand",
      "price": 29000,
      "categoryName": "Cà Phê Truyền Thống",
      "isAvailable": true
    }
  ],
  "errors": null
}
```

---

## 5. Danh Sách RESTful API Endpoints Chính

| HTTP Method | Endpoint | Mô Tả Nghiệp Vụ |
| :--- | :--- | :--- |
| `GET` | `/api/categories` | Lấy danh sách danh mục đồ uống |
| `GET` | `/api/categories/{id}` | Lấy thông tin chi tiết một danh mục |
| `POST` | `/api/categories` | Tạo danh mục mới |
| `GET` | `/api/products` | Lấy danh sách sản phẩm (hỗ trợ lọc `categoryId`, `featuredOnly`) |
| `GET` | `/api/products/{id}` | Lấy chi tiết sản phẩm và danh mục |
| `POST` | `/api/products` | Tạo sản phẩm đồ uống mới |
| `PUT` | `/api/products/{id}` | Cập nhật thông tin sản phẩm |
| `DELETE` | `/api/products/{id}` | Xóa mềm sản phẩm (`IsDeleted = true`) |
| `POST` | `/api/orders` | Đặt hàng và lưu thông tin chi tiết |