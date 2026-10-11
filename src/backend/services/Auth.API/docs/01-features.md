# 📋 01-features.md - Danh Sách Chức Năng Auth.API

> Dịch vụ Xác thực & Quản lý Người dùng - Hệ thống LowLand Coffee Microservices (PRN232)  
> Người phụ trách: **Khang**

---

## 1. Tổng quan các nhóm chức năng

Hệ thống cung cấp 3 nhóm tính năng nghiệp vụ chính:
1. **Nhóm Xác thực (Authentication):** Đăng ký, đăng nhập đa phương thức, quản lý vòng đời token và khôi phục tài khoản.
2. **Nhóm Hồ sơ Cá nhân (Profile):** Quản lý thông tin tài khoản và an toàn bảo mật người dùng.
3. **Nhóm Quản trị Người dùng (Admin User Management):** Phân quyền, cấp tài khoản nhân viên, kiểm duyệt trạng thái và quản trị dữ liệu.

---

## 2. Chi tiết từng tính năng

### 2.1. Phân hệ Khách hàng & Công khai (Public & Customer)

#### 🔹 Feature 01: Đăng ký tài khoản Khách hàng (Register)
- **Đối tượng sử dụng:** Khách vãng lai, người dùng mới.
- **Mô tả:** Cho phép khách hàng tạo tài khoản thành viên để đặt món tại LowLand Coffee.
- **Đặc điểm:**
  - Nhập Họ tên, Email, Số điện thoại (tùy chọn), Mật khẩu và Xác nhận mật khẩu.
  - Tự động kiểm tra trùng lặp Email và Số điện thoại trong cơ sở dữ liệu.
  - Băm mật khẩu bằng thuật toán an toàn **BCrypt** (salt tự động).
  - Tự động gán quyền vai trò mặc định là **`Customer`**.
  - Sau khi đăng ký thành công, hệ thống tự động cấp phát ngay bộ đôi **JWT Access Token** và **Refresh Token** để người dùng không cần đăng nhập lại.

#### 🔹 Feature 02: Đăng nhập đa phương thức (Flexible Login)
- **Đối tượng sử dụng:** Mọi người dùng (`Customer`, `Staff`, `Admin`).
- **Mô tả:** Đăng nhập vào hệ thống để lấy token truy cập.
- **Đặc điểm:**
  - Hỗ trợ đăng nhập linh hoạt bằng **Email** hoặc **Số điện thoại** kết hợp với Mật khẩu.
  - Kiểm tra trạng thái tài khoản: Nếu tài khoản đang bị khóa (`isActive = false`), từ chối đăng nhập với thông báo rõ ràng.
  - Cấp phát **JWT Access Token** (thời hạn 60 phút) mang đầy đủ các Claims định danh và quyền hạn.
  - Cấp phát **Refresh Token** bảo mật cao (thời hạn 7 ngày).

#### 🔹 Feature 03: Cấp lại phiên đăng nhập (Refresh Token Rotation)
- **Đối tượng sử dụng:** Mọi client/ứng dụng đã đăng nhập khi Access Token hết hạn.
- **Mô tả:** Cấp Access Token mới mà người dùng không cần nhập lại mật khẩu.
- **Đặc điểm:**
  - Sử dụng cơ chế **Token Rotation**: Mỗi lần đổi token thành công, Refresh Token cũ sẽ bị hủy ngay lập tức và một Refresh Token mới được cấp phát.
  - Ngăn ngừa triệt để nguy cơ tấn công phát lại (Replay Attack).

#### 🔹 Feature 04: Quên mật khẩu qua mã xác thực OTP (Forgot Password)
- **Đối tượng sử dụng:** Người dùng quên mật khẩu đăng nhập.
- **Mô tả:** Gửi yêu cầu khôi phục mật khẩu về Email đã đăng ký.
- **Đặc điểm:**
  - Tự động tạo mã xác thực OTP ngẫu nhiên gồm **6 chữ số**.
  - Mã OTP có hiệu lực trong vòng **15 phút**.
  - Tích hợp dịch vụ gửi email `EmailService` (ghi log chi tiết ở môi trường Development).
  - Áp dụng kỹ thuật bảo mật chống dò quét tài khoản (User Enumeration Defense): Thông báo thành công đồng nhất ngay cả khi email không tồn tại.

#### 🔹 Feature 05: Khôi phục mật khẩu mới (Reset Password)
- **Đối tượng sử dụng:** Người dùng đã nhận được mã OTP qua Email.
- **Mô tả:** Xác thực mã OTP và thiết lập mật khẩu đăng nhập mới.
- **Đặc điểm:**
  - Kiểm tra tính hợp lệ và thời hạn của mã OTP.
  - Sau khi xác nhận thành công, mã OTP được đánh dấu đã sử dụng (`isUsed = true`) và không thể tái sử dụng.
  - Cập nhật mật khẩu mới đã băm BCrypt vào Database.
  - Tự động thu hồi toàn bộ Refresh Token của các phiên đăng nhập cũ trên mọi thiết bị nhằm bảo đảm an toàn.

---

### 2.2. Phân hệ Cá nhân người dùng (Authenticated Profile)

#### 🔹 Feature 06: Xem thông tin hồ sơ (View Profile)
- **Đối tượng sử dụng:** Người dùng đã đăng nhập (Customer, Staff, Admin).
- **Mô tả:** Lấy thông tin cá nhân của chính mình.
- **Đặc điểm:**
  - Trích xuất User ID an toàn từ JWT Bearer token (Claim `sub` / `NameIdentifier`), không cần truyền ID trên đường dẫn URL.
  - Trả về thông tin: Họ tên, Email, Số điện thoại, Vai trò (`RoleName`), Avatar, Địa chỉ mặc định.

#### 🔹 Feature 07: Cập nhật hồ sơ cá nhân (Update Profile)
- **Đối tượng sử dụng:** Người dùng đã đăng nhập.
- **Mô tả:** Cập nhật thông tin liên lạc và giao hàng.
- **Đặc điểm:**
  - Cho phép thay đổi: Họ tên, Số điện thoại, Ảnh đại diện (Avatar URL), Địa chỉ nhận đồ uống.
  - Kiểm tra tính duy nhất nếu cập nhật Số điện thoại mới (tránh trùng tài khoản người khác).

#### 🔹 Feature 08: Đổi mật khẩu chủ động (Change Password)
- **Đối tượng sử dụng:** Người dùng đã đăng nhập.
- **Mô tả:** Thay đổi mật khẩu định kỳ hoặc khi nghi ngờ lộ mật khẩu.
- **Đặc điểm:**
  - Bắt buộc xác thực mật khẩu hiện tại bằng BCrypt.
  - Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ.
  - Tự động đăng xuất/hủy bỏ toàn bộ Refresh Token hiện tại để bắt buộc đăng nhập lại bằng mật khẩu mới.

---

### 2.3. Phân hệ Quản trị viên (Admin Management)

#### 🔹 Feature 09: Xem danh sách người dùng & Phân trang (User Directory)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Quản lý toàn bộ danh sách tài khoản trong hệ thống LowLand Coffee.
- **Đặc điểm:**
  - **Tìm kiếm đa năng (`search`):** Tìm kiếm theo Họ tên, Email hoặc Số điện thoại.
  - **Lọc theo vai trò (`roleName`):** Lọc riêng `Customer`, `Staff` hoặc `Admin`.
  - **Lọc theo trạng thái (`isActive`):** Lọc danh sách tài khoản đang hoạt động hoặc đang bị khóa.
  - **Phân trang chuẩn RESTful:** Trả về cấu trúc `PagedResult<UserDto>` gồm danh sách, tổng số bản ghi, trang hiện tại, kích thước trang và tổng số trang.

#### 🔹 Feature 10: Xem chi tiết tài khoản theo ID (User Detail)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Xem đầy đủ thông tin định danh và vai trò của một tài khoản cụ thể.

#### 🔹 Feature 11: Lấy danh mục Vai trò (Get Roles)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Lấy danh sách các vai trò có trong hệ thống (`Customer`, `Staff`, `Admin`) phục vụ việc hiển thị dropdown trên giao diện quản trị.

#### 🔹 Feature 12: Cấp tài khoản Nhân viên (Create Staff Account)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Tạo tài khoản nội bộ cho Nhân viên thu ngân / pha chế làm việc tại các chi nhánh quán cà phê.
- **Đặc điểm:**
  - Tự động gắn quyền vai trò **`Staff`**.
  - Kiểm tra tính duy nhất của Email và Số điện thoại.
  - Thiết lập mật khẩu ban đầu và kích hoạt tài khoản ngay lập tức.

#### 🔹 Feature 13: Khóa / Mở khóa tài khoản (Toggle Account Status)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Kiểm duyệt hoặc tạm ngưng quyền truy cập của người dùng vi phạm chính sách.
- **Đặc điểm:**
  - Cập nhật trạng thái `isActive = false` (Khóa) hoặc `isActive = true` (Mở khóa).
  - **Cơ chế an toàn tức thì:** Ngay khi tài khoản bị khóa, hệ thống tự động vô hiệu hóa toàn bộ Refresh Token của tài khoản đó, ngăn chặn việc tiếp tục xin cấp mới token.

#### 🔹 Feature 14: Xóa cứng tài khoản (Hard Delete User)
- **Đối tượng sử dụng:** Quản trị viên (`Admin`).
- **Mô tả:** Xóa vĩnh viễn tài khoản người dùng khỏi hệ thống.
- **Đặc điểm:**
  - **Cơ chế Xóa cứng (Hard Delete):** Sử dụng `_context.Users.Remove(user)` để xóa vĩnh viễn bản ghi khỏi bảng `dbo.auth_users` trong cơ sở dữ liệu.
  - Tự động dọn dẹp và thu hồi toàn bộ token liên quan trong hệ thống.
