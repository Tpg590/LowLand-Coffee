# ☕ PRODUCT-API.md - Product / Catalog Service Specification

- **Phụ trách:** **Phú**
- **Database:** `Coffee_ProductDB` (SQL Server)
- **Port nội bộ Docker:** `5002`
- **Công nghệ:** ASP.NET Core Web API (.NET 8), Entity Framework Core, Redis Cache (tùy chọn)

---

## 🎯 Danh sách Chức năng chi tiết (Môn PRN232)

1. **Duyệt & Xem Thực đơn (Menu):**
   - Lấy danh sách sản phẩm theo từng Danh mục (Cà phê, Trà, Bánh, Topping).
   - Xem chi tiết sản phẩm: Tên món, ảnh, mô tả, giá niêm yết, danh sách Size (S/M/L) và danh sách Topping đi kèm.
2. **Tìm kiếm & Lọc sản phẩm:**
   - Tìm kiếm theo từ khóa tên món.
   - Lọc theo khoảng giá, danh mục, hoặc nhãn nổi bật (`Best Seller`, `Món Mới`).
3. **Quản lý Danh mục (CRUD Category - Admin):**
   - Thêm, sửa, xóa (chỉ xóa danh mục không chứa món) hoặc ẩn/hiện danh mục.
4. **Quản lý Sản phẩm & Biến thể (CRUD Product - Admin/Staff):**
   - Tạo/chỉnh sửa sản phẩm: Đặt tên, chọn danh mục, tải ảnh, mô tả, giá cơ sở.
   - Cấu hình Size & Giá chênh lệch: (Size M +0k, Size L +10k...).
   - Cấu hình danh sách Topping và giá cộng thêm của từng topping.
   - **Bật/Tắt trạng thái kinh doanh nhanh (Stock Toggle):** Chuyển đổi trạng thái `Còn hàng` / `Hết hàng` ngay trong ca làm việc.
   - Xóa mềm sản phẩm khỏi Menu (`IsDeleted = true`).

---

## 📡 Danh sách RESTful API Endpoints dự kiến

| Method | Endpoint | Quyền | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/categories` | Public | Lấy danh sách danh mục món |
| `POST` | `/api/categories` | Admin | Thêm danh mục mới |
| `PUT` | `/api/categories/{id}` | Admin | Sửa tên danh mục, thứ tự hiển thị |
| `DELETE` | `/api/categories/{id}` | Admin | Xóa danh mục rỗng |
| `GET` | `/api/products` | Public | Lấy danh sách món (hỗ trợ search, filter, pagination) |
| `GET` | `/api/products/{id}` | Public | Xem chi tiết 1 món kèm Size và Topping |
| `POST` | `/api/products` | Admin | Tạo món mới |
| `PUT` | `/api/products/{id}` | Admin | Cập nhật thông tin món, giá, ảnh |
| `PATCH` | `/api/products/{id}/toggle-stock` | Staff / Admin | Bật/tắt nhanh Còn hàng / Hết hàng |
| `DELETE` | `/api/products/{id}` | Admin | Xóa mềm món |

---

## 🚀 Lệnh tạo dự án khi bắt đầu code:
Đứng tại thư mục gốc repository (`LowLand-Coffee/`):
```bash
dotnet new webapi -n Product.API -o src/backend/services/Product.API --framework net8.0 --use-controllers
dotnet sln src/backend/CoffeeShop.backend.sln add src/backend/services/Product.API/Product.API.csproj
dotnet add src/backend/services/Product.API package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/backend/services/Product.API package Microsoft.EntityFrameworkCore.Tools
```
