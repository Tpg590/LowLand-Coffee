# ⚖️ 03-business-rules.md - Quy Tắc Nghiệp Vụ (Business Rules)

> Dịch vụ Xác thực & Quản lý Người dùng - Hệ thống LowLand Coffee Microservices (PRN232)  
> Người phụ trách: **Khang**

---

## 1. Danh sách Business Rules (BR)

| Mã BR | Tên quy tắc nghiệp vụ | Áp dụng cho |
| :--- | :--- | :--- |
| **BR-AUTH-01** | Định danh tài khoản & Tính duy nhất | Đăng ký, Tạo nhân viên, Cập nhật thông tin |
| **BR-AUTH-02** | Tiêu chuẩn Mật khẩu & Thuật toán Băm BCrypt | Đăng ký, Đổi mật khẩu, Khôi phục mật khẩu |
| **BR-AUTH-03** | Quy chuẩn Cấp phát JWT Access Token | Đăng nhập, Đăng ký, Refresh Token |
| **BR-AUTH-04** | Vòng đời Refresh Token & Cơ chế Token Rotation | Đăng nhập, Cấp lại Token |
| **BR-AUTH-05** | Quy tắc Xác thực Mã OTP Khôi phục Mật khẩu | Quên mật khẩu, Đặt lại mật khẩu |
| **BR-AUTH-06** | Ma trận Phân quyền & Phân cấp Vai trò | Kiểm soát truy cập toàn hệ thống |
| **BR-AUTH-07** | Cơ chế Khóa tài khoản & Thu hồi Token tức thì | Quản trị viên (Khóa/Mở khóa) |
| **BR-AUTH-08** | Cơ chế Xóa cứng (Hard Delete) | Quản trị viên (Xóa tài khoản) |

---

## 2. Chi tiết từng Business Rule

### 📌 BR-AUTH-01: Định danh tài khoản & Tính duy nhất
1. **Email:**
   - Là định danh chính của mỗi tài khoản.
   - Không được phép trùng lặp trong toàn bộ hệ thống (`UNIQUE` constraint).
   - Hệ thống luôn chuẩn hóa Email về dạng chữ thường không có khoảng trắng thừa (`email.Trim().ToLower()`) trước khi lưu trữ hoặc so khớp truy vấn.
2. **Số điện thoại (Phone Number):**
   - Không bắt buộc khi đăng ký, nhưng nếu được cung cấp thì phải có tính duy nhất trong hệ thống.
   - Khi người dùng cập nhật hồ sơ cá nhân hoặc Admin cập nhật, số điện thoại mới không được trùng với số điện thoại của tài khoản khác.

---

### 📌 BR-AUTH-02: Tiêu chuẩn Mật khẩu & Thuật toán Băm BCrypt
1. **Độ dài & Định dạng:**
   - Mật khẩu phải có độ dài tối thiểu **6 ký tự**.
   - Trường xác nhận mật khẩu (`confirmPassword`) bắt buộc phải trùng khớp 100% với mật khẩu chính.
2. **Thuật toán Băm (Hashing):**
   - Tuyệt đối không lưu trữ mật khẩu dưới dạng văn bản thô (Plain Text) trong cơ sở dữ liệu.
   - Sử dụng thư viện **`BCrypt.Net-Next`** với cơ chế sinh muối ngẫu nhiên (Salt) tự động tích hợp:
     ```csharp
     string passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);
     bool isValid = BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
     ```
3. **Đổi mật khẩu:**
   - Bắt buộc xác thực mật khẩu hiện tại bằng BCrypt trước khi cho phép cập nhật.
   - Mật khẩu mới không được trùng với mật khẩu hiện tại.

---

### 📌 BR-AUTH-03: Quy chuẩn Cấp phát JWT Access Token
1. **Thuật toán Ký:** Ký bằng khóa đối xứng với thuật toán **`HmacSha256`** (`HS256`).
2. **Thời hạn tồn tại (Expiry):**
   - Mặc định là **60 phút** (cấu hình qua `JwtSettings:ExpiryInMinutes` hoặc `JwtSettings:ExpiryInHours`).
   - Thời hạn ngắn giúp hạn chế tối đa rủi ro nếu token bị lộ trong quá trình truyền tải mạng.
3. **Cấu trúc Claims bắt buộc:**
   - `sub` / `NameIdentifier`: UUID duy nhất của người dùng (`user.Id`).
   - `email`: Địa chỉ email của người dùng.
   - `name`: Họ và tên hiển thị (`user.FullName`).
   - `role`: Tên vai trò của người dùng (`Customer`, `Staff`, hoặc `Admin`).
   - `jti`: UUID ngẫu nhiên định danh phiên token (`Guid.NewGuid()`).
   - `phone_number` / `MobilePhone`: Số điện thoại (nếu có).

---

### 📌 BR-AUTH-04: Vòng đời Refresh Token & Cơ chế Token Rotation
1. **Mục đích:** Duy trì phiên đăng nhập lâu dài của người dùng mà không cần lưu trữ thông tin mật khẩu phía Client.
2. **Thời hạn tồn tại:** **7 ngày** kể từ thời điểm cấp phát.
3. **Cơ chế Token Rotation (Xoay vòng Token):**
   - Mỗi Refresh Token chỉ được sử dụng thành công đúng **01 lần**.
   - Khi Client gửi Refresh Token cũ lên để xin Access Token mới:
     - Hệ thống kiểm tra tính hợp lệ và thời hạn.
     - Hệ thống lập tức thu hồi/hủy hiệu lực của Refresh Token cũ (`isRevoked = true`).
     - Hệ thống phát hành một Refresh Token hoàn toàn mới cùng Access Token mới trả về cho Client.
   - Nếu phát hiện Refresh Token đã bị thu hồi mà vẫn được gửi lên, hệ thống sẽ từ chối để ngăn chặn tấn công phát lại (Replay Attack).

---

### 📌 BR-AUTH-05: Quy tắc Xác thực Mã OTP Khôi phục Mật khẩu
1. **Cấu trúc OTP:** Chuỗi ngẫu nhiên gồm đúng **6 chữ số** (từ `100000` đến `999999`) sinh bởi bộ sinh số ngẫu nhiên mật mã an toàn `RandomNumberGenerator.GetInt32(...)`.
2. **Thời hạn hiệu lực:** Đúng **15 phút** kể từ thời điểm tạo.
3. **Tính chất sử dụng:**
   - Mỗi mã OTP chỉ có hiệu lực **01 lần duy nhất**. Sau khi đặt lại mật khẩu thành công, mã OTP bị đánh dấu `isUsed = true`.
4. **Bảo mật chống dò quét tài khoản (User Enumeration Defense):**
   - Khi người dùng gửi yêu cầu quên mật khẩu, ngay cả khi email không tồn tại trong DB, API vẫn trả về thông điệp thành công chung để kẻ tấn công không thể dò biết danh sách email người dùng của quán.
5. **Vô hiệu hóa phiên cũ:**
   - Ngay sau khi đặt lại mật khẩu mới qua OTP thành công, toàn bộ Refresh Token của tài khoản đó sẽ bị hủy ngay lập tức để buộc tất cả các thiết bị phải đăng nhập lại với mật khẩu mới.

---

### 📌 BR-AUTH-06: Ma trận Phân quyền & Phân cấp Vai trò
Hệ thống LowLand Coffee chia thành 3 vai trò phân cấp rõ ràng:

| Quyền hạn / Thao tác | Public | Customer | Staff | Admin |
| :--- | :---: | :---: | :---: | :---: |
| Đăng ký tài khoản khách hàng | ✅ | ❌ | ❌ | ❌ |
| Đăng nhập (Email/SĐT) | ✅ | ✅ | ✅ | ✅ |
| Cấp lại Token (Refresh Token) | ✅ | ✅ | ✅ | ✅ |
| Quên mật khẩu & OTP | ✅ | ✅ | ✅ | ✅ |
| Xem & Cập nhật hồ sơ cá nhân | ❌ | ✅ | ✅ | ✅ |
| Đổi mật khẩu cá nhân | ❌ | ✅ | ✅ | ✅ |
| Xem danh sách người dùng & phân trang | ❌ | ❌ | ❌ | ✅ |
| Xem chi tiết người dùng theo ID | ❌ | ❌ | ❌ | ✅ |
| Cấp tài khoản cho Nhân viên (Staff) | ❌ | ❌ | ❌ | ✅ |
| Khóa / Mở khóa tài khoản | ❌ | ❌ | ❌ | ✅ |
| Xóa cứng tài khoản người dùng | ❌ | ❌ | ❌ | ✅ |

- **Customer:** Vai trò mặc định của khách hàng đặt nước uống.
- **Staff:** Nhân viên quầy thu ngân và pha chế (chỉ được tạo bởi Admin).
- **Admin:** Quản trị viên toàn quyền hệ thống.

---

### 📌 BR-AUTH-07: Cơ chế Khóa tài khoản & Thu hồi Token tức thì
1. **Hành vi Khóa (`isActive = false`):**
   - Khi Admin khóa tài khoản của người dùng, hệ thống sẽ:
     1. Cập nhật `isActive = false` trong cơ sở dữ liệu.
     2. **Thu hồi ngay lập tức** toàn bộ Refresh Token của tài khoản đó (`RevokeUserTokens`).
2. **Hành vi khi Người dùng đăng nhập:**
   - Nếu tài khoản có `isActive = false`, API đăng nhập sẽ trả về `403 Forbidden` kèm thông báo: *"Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."*
3. **Hành vi khi Người dùng gia hạn Token:**
   - Khi Access Token hiện tại hết hạn và Client dùng Refresh Token để gia hạn, hệ thống kiểm tra thấy tài khoản bị khóa sẽ từ chối cấp token mới ngay lập tức.

---

### 📌 BR-AUTH-08: Cơ chế Xóa cứng (Hard Delete)
1. **Khái niệm:**
   - Theo đúng yêu cầu thiết kế của dự án, hệ thống áp dụng cơ chế **Xóa cứng (Hard Delete)** trực tiếp thay vì xóa mềm (không sử dụng cờ `is_deleted`).
2. **Hành vi thực thi:**
   - Khi Admin thực hiện xóa tài khoản qua `DELETE /api/users/{id}`:
     ```csharp
     _context.Users.Remove(user);
     await _context.SaveChangesAsync();
     ```
   - Bản ghi của tài khoản bị xóa vĩnh viễn khỏi bảng `dbo.auth_users`.
   - Toàn bộ Refresh Token và OTP còn lại của người dùng đó bị hủy bỏ khỏi bộ nhớ lưu trữ phiên.
   - Tài khoản bị xóa sẽ không thể khôi phục lại và email/SĐT đó có thể được sử dụng để đăng ký tài khoản mới trong tương lai.
