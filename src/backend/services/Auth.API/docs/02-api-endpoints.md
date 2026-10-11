# 📡 02-api-endpoints.md - Danh Sách & Đặc Tả Toàn Diện RESTful Endpoints

> **Dịch vụ Xác thực & Quản lý Người dùng (Auth.API) - Hệ thống LowLand Coffee Microservices**  
> Host Local: `http://localhost:5001` | Gateway: `http://localhost:5000`

---

## 1. Bảng tóm tắt toàn bộ Endpoints

| STT | Method | Endpoint | Quyền hạn | Mục đích |
| :---: | :--- | :--- | :---: | :--- |
| **1** | `POST` | `/api/auth/register` | Public | Đăng ký tài khoản Khách hàng mới |
| **2** | `POST` | `/api/auth/login` | Public | Đăng nhập (Email/SĐT) & cấp phát Token |
| **3** | `POST` | `/api/auth/refresh-token` | Public | Cấp lại Access Token từ Refresh Token |
| **4** | `POST` | `/api/auth/forgot-password` | Public | Gửi mã OTP khôi phục mật khẩu |
| **5** | `POST` | `/api/auth/reset-password` | Public | Đổi mật khẩu mới kèm mã OTP |
| **6** | `GET` | `/api/auth/profile` | Authenticated | Xem thông tin hồ sơ của tài khoản hiện tại |
| **7** | `PUT` | `/api/auth/profile` | Authenticated | Cập nhật thông tin hồ sơ cá nhân |
| **8** | `POST` | `/api/auth/change-password` | Authenticated | Đổi mật khẩu tài khoản hiện tại |
| **9** | `GET` | `/api/users` | Admin | Lấy danh sách toàn bộ người dùng (Paging & Search) |
| **10** | `GET` | `/api/users/{id}` | Admin | Xem chi tiết thông tin một người dùng theo ID |
| **11** | `GET` | `/api/users/roles` | Admin | Lấy danh mục các vai trò (Roles) trong hệ thống |
| **12** | `POST` | `/api/users/staff` | Admin | Cấp tài khoản cho Nhân viên (Staff) |
| **13** | `PUT` | `/api/users/{id}/status` | Admin | Khóa / Mở khóa tài khoản người dùng |
| **14** | `DELETE`| `/api/users/{id}` | Admin | Xóa cứng tài khoản người dùng vĩnh viễn |

---

## 2. Đặc tả Chi tiết từng Endpoint kèm Ví dụ Đầy đủ Mọi Trạng thái Phản hồi

---

### 1. `POST /api/auth/register`
- **Mục đích:** Đăng ký tài khoản thành viên mới cho Khách hàng.
- **Quyền hạn:** `Public`.
- **Headers:** `Content-Type: application/json`

#### 📥 Ràng buộc dữ liệu đầu vào (Request Body Constraints)
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `fullName` | `string` | **Có** | Tối đa 100 ký tự | Họ và tên khách hàng |
| `email` | `string` | **Có** | Tối đa 255 ký tự, định dạng email chuẩn | Email đăng nhập (duy nhất) |
| `phoneNumber` | `string` | Không | Tối đa 20 ký tự, định dạng SĐT | SĐT liên hệ (duy nhất nếu nhập) |
| `password` | `string` | **Có** | Tối thiểu 6 ký tự | Mật khẩu tài khoản |
| `confirmPassword` | `string` | **Có** | Bắt buộc trùng khớp với `password` | Xác nhận mật khẩu |

#### 📄 Request Body Mẫu:
```json
{
  "fullName": "Nguyễn Văn Khách",
  "email": "customer@gmail.com",
  "phoneNumber": "0903344556",
  "password": "Password123!",
  "confirmPassword": "Password123!"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `201 Created` - Đăng ký thành công
```json
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Đăng ký tài khoản thành công",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJjY2NjY2NjYy1jY2NjLWNjY2MtY2NjYy1jY2NjY2NjY2NjY2MiLCJlbWFpbCI6ImN1c3RvbWVyQGdtYWlsLmNvbSIsIm5hbWUiOiJOZ3V54buFbiBWxINuIEtow6FjaCIsInJvbGUiOiJDdXN0b21lciIsImp0aSI6Ijg2OWMzMTUyLWI3NzgtNDJkMC04OTRhLTMwOTcyNTllOWJiMCJ9...",
    "refreshToken": "xW9fG72pQ_8dKlmNop3...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
      "roleId": "33333333-3333-3333-3333-333333333333",
      "roleName": "Customer",
      "email": "customer@gmail.com",
      "fullName": "Nguyễn Văn Khách",
      "phoneNumber": "0903344556",
      "avatarUrl": null,
      "address": null,
      "isActive": true,
      "createdAt": "2026-10-11T13:00:00Z",
      "updatedAt": "2026-10-11T13:00:00Z"
    }
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Dữ liệu không hợp lệ (Validation Failure)
*(Ví dụ: Họ tên rỗng, Email sai format, mật khẩu dưới 6 ký tự hoặc không khớp)*
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu đăng ký không hợp lệ",
  "data": null,
  "errors": [
    "Họ và tên là bắt buộc",
    "Email không hợp lệ",
    "Mật khẩu phải có ít nhất 6 ký tự",
    "Mật khẩu xác nhận không khớp"
  ]
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Trùng lặp Email
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Email đã được sử dụng trong hệ thống",
  "data": null,
  "errors": [
    "Email đã được sử dụng trong hệ thống"
  ]
}
```

##### ❌ Trạng thái 4: `400 Bad Request` - Trùng lặp Số điện thoại
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Số điện thoại đã được sử dụng trong hệ thống",
  "data": null,
  "errors": [
    "Số điện thoại đã được sử dụng trong hệ thống"
  ]
}
```

##### ❌ Trạng thái 5: `500 Internal Server Error` - Lỗi cấu hình hệ thống
```json
{
  "isSuccess": false,
  "statusCode": 500,
  "message": "Không tìm thấy vai trò mặc định của hệ thống",
  "data": null,
  "errors": [
    "Không tìm thấy vai trò mặc định của hệ thống"
  ]
}
```

---

### 2. `POST /api/auth/login`
- **Mục đích:** Đăng nhập bằng Email hoặc Số điện thoại, nhận JWT Token.
- **Quyền hạn:** `Public`.
- **Headers:** `Content-Type: application/json`

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `emailOrPhone` | `string` | **Có** | Không để trống | Email hoặc Số điện thoại tài khoản |
| `password` | `string` | **Có** | Không để trống | Mật khẩu tài khoản |

#### 📄 Request Body Mẫu:
```json
{
  "emailOrPhone": "admin@lowlandcoffee.com",
  "password": "Admin@123"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Đăng nhập thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Đăng nhập thành công",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYWFhYWFhYS1hYWFhLWFhYWEtYWFhYS1hYWFhYWFhYWFhYWEiLCJlbWFpbCI6ImFkbWluQGxvd2xhbmRjb2ZmZWUuY29tIiwibmFtZSI6IlF14bqjbiBUcuG7iyBWacOqbiIsInJvbGUiOiJBZG1pbiIsImp0aSI6ImIzOWM4MTc0LTI1M2YtNGZkMS1iODBhLTgwZDFlMmMyYWRjNyJ9...",
    "refreshToken": "kL8pQ2_vMn89WqP0...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "roleId": "11111111-1111-1111-1111-111111111111",
      "roleName": "Admin",
      "email": "admin@lowlandcoffee.com",
      "fullName": "Quản Trị Viên",
      "phoneNumber": "0901122334",
      "avatarUrl": "https://placehold.co/150",
      "address": "123 Nguyễn Huệ, Q.1, TP.HCM",
      "isActive": true,
      "createdAt": "2026-10-11T13:00:00Z",
      "updatedAt": "2026-10-11T13:00:00Z"
    }
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Dữ liệu đăng nhập trống
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu đăng nhập không hợp lệ",
  "data": null,
  "errors": [
    "Vui lòng nhập Email hoặc Số điện thoại",
    "Vui lòng nhập Mật khẩu"
  ]
}
```

##### ❌ Trạng thái 3: `401 Unauthorized` - Sai thông tin tài khoản hoặc mật khẩu
```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Email/Số điện thoại hoặc mật khẩu không chính xác",
  "data": null,
  "errors": [
    "Email/Số điện thoại hoặc mật khẩu không chính xác"
  ]
}
```

##### ❌ Trạng thái 4: `403 Forbidden` - Tài khoản đã bị khóa bởi Admin
```json
{
  "isSuccess": false,
  "statusCode": 403,
  "message": "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.",
  "data": null,
  "errors": [
    "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."
  ]
}
```

---

### 3. `POST /api/auth/refresh-token`
- **Mục đích:** Cấp lại Access Token mới và xoay vòng Refresh Token (Token Rotation).
- **Quyền hạn:** `Public`.

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `refreshToken` | `string` | **Có** | Chuỗi token không để trống | Mã Refresh Token nhận được khi đăng nhập |

#### 📄 Request Body Mẫu:
```json
{
  "refreshToken": "kL8pQ2_vMn89WqP0..."
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Cấp lại token thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Cấp lại Access Token thành công",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.newAccessToken...",
    "refreshToken": "new_rotated_refresh_token_string_89a...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": { ... }
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Để trống trường `refreshToken`
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "errors": [
    "Refresh Token là bắt buộc"
  ]
}
```

##### ❌ Trạng thái 3: `401 Unauthorized` - Token không hợp lệ / Đã hết hạn / Đã bị thu hồi
```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Refresh Token không hợp lệ hoặc đã hết hạn",
  "data": null,
  "errors": [
    "Refresh Token không hợp lệ hoặc đã hết hạn"
  ]
}
```

##### ❌ Trạng thái 4: `403 Forbidden` - Tài khoản sở hữu token đang bị khóa
```json
{
  "isSuccess": false,
  "statusCode": 403,
  "message": "Tài khoản người dùng không hợp lệ hoặc đã bị khóa",
  "data": null,
  "errors": [
    "Tài khoản người dùng không hợp lệ hoặc đã bị khóa"
  ]
}
```

---

### 4. `POST /api/auth/forgot-password`
- **Mục đích:** Yêu cầu gửi mã OTP đặt lại mật khẩu về email.
- **Quyền hạn:** `Public`.

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `email` | `string` | **Có** | Email đúng định dạng chuẩn | Email tài khoản cần khôi phục |

#### 📄 Request Body Mẫu:
```json
{
  "email": "customer@gmail.com"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Gửi mã OTP thành công (Email tồn tại)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Mã OTP khôi phục mật khẩu đã được gửi tới email của bạn (Hiệu lực trong 15 phút)",
  "data": "782109",
  "errors": null
}
```

##### ✅ Trạng thái 2: `200 OK` - Phản hồi an toàn khi Email không tồn tại (User Enumeration Defense)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Nếu email tồn tại trong hệ thống, mã OTP xác thực sẽ được gửi tới hộp thư của bạn",
  "data": "Mã OTP đã được gửi",
  "errors": null
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Email sai định dạng hoặc để trống
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Email không hợp lệ",
  "data": null,
  "errors": [
    "Email không hợp lệ"
  ]
}
```

---

### 5. `POST /api/auth/reset-password`
- **Mục đích:** Xác thực mã OTP và cập nhật mật khẩu mới.
- **Quyền hạn:** `Public`.

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `email` | `string` | **Có** | Email đúng định dạng chuẩn | Email tài khoản cần đặt lại mật khẩu |
| `otpCode` | `string` | **Có** | Tối đa 10 ký tự | Mã OTP 6 chữ số |
| `newPassword` | `string` | **Có** | Tối thiểu 6 ký tự | Mật khẩu mới |
| `confirmPassword` | `string` | **Có** | Trùng khớp với `newPassword` | Xác nhận mật khẩu mới |

#### 📄 Request Body Mẫu:
```json
{
  "email": "customer@gmail.com",
  "otpCode": "782109",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Đặt lại mật khẩu thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Khôi phục mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới",
  "data": true,
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Dữ liệu không hợp lệ (Validation Failure)
*(Mật khẩu xác nhận không khớp hoặc dưới 6 ký tự)*
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu đặt lại mật khẩu không hợp lệ",
  "data": null,
  "errors": [
    "Mật khẩu xác nhận không khớp"
  ]
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Mã OTP không đúng hoặc đã quá 15 phút
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Mã OTP không chính xác hoặc đã hết hạn",
  "data": false,
  "errors": [
    "Mã OTP không chính xác hoặc đã hết hạn"
  ]
}
```

##### ❌ Trạng thái 4: `400 Bad Request` - Email tài khoản không tồn tại
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Yêu cầu đặt lại mật khẩu không hợp lệ",
  "data": false,
  "errors": [
    "Yêu cầu đặt lại mật khẩu không hợp lệ"
  ]
}
```

---

### 6. `GET /api/auth/profile`
- **Mục đích:** Xem hồ sơ cá nhân của tài khoản đang đăng nhập.
- **Quyền hạn:** `Authenticated` (Yêu cầu `Authorization: Bearer <token>`).

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Lấy thông tin hồ sơ thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Lấy thông tin hồ sơ thành công",
  "data": {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "roleId": "11111111-1111-1111-1111-111111111111",
    "roleName": "Admin",
    "email": "admin@lowlandcoffee.com",
    "fullName": "Quản Trị Viên",
    "phoneNumber": "0901122334",
    "avatarUrl": "https://placehold.co/150",
    "address": "123 Nguyễn Huệ, Q.1, TP.HCM",
    "createdAt": "2026-10-11T13:00:00Z"
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `401 Unauthorized` - Thiếu Token hoặc Token không hợp lệ
```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Không xác định được danh tính người dùng",
  "data": null,
  "errors": [
    "Không xác định được danh tính người dùng"
  ]
}
```

##### ❌ Trạng thái 3: `404 Not Found` - Không tìm thấy người dùng
```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy thông tin người dùng",
  "data": null,
  "errors": [
    "Không tìm thấy thông tin người dùng"
  ]
}
```

---

### 7. `PUT /api/auth/profile`
- **Mục đích:** Cập nhật thông tin hồ sơ của tài khoản đang đăng nhập.
- **Quyền hạn:** `Authenticated`.
- **Headers:** `Authorization: Bearer <token>`, `Content-Type: application/json`

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `fullName` | `string` | **Có** | Tối đa 100 ký tự | Họ và tên mới |
| `phoneNumber` | `string` | Không | Tối đa 20 ký tự | Số điện thoại mới |
| `avatarUrl` | `string` | Không | Chuỗi URL | Ảnh đại diện mới |
| `address` | `string` | Không | Chuỗi văn bản | Địa chỉ giao hàng mặc định |

#### 📄 Request Body Mẫu:
```json
{
  "fullName": "Quản Trị Viên (Cập nhật)",
  "phoneNumber": "0901122334",
  "avatarUrl": "https://placehold.co/200",
  "address": "Tầng 5, Tòa nhà LowLand Landmark 81, TP.HCM"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Cập nhật thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Cập nhật thông tin hồ sơ thành công",
  "data": {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "roleId": "11111111-1111-1111-1111-111111111111",
    "roleName": "Admin",
    "email": "admin@lowlandcoffee.com",
    "fullName": "Quản Trị Viên (Cập nhật)",
    "phoneNumber": "0901122334",
    "avatarUrl": "https://placehold.co/200",
    "address": "Tầng 5, Tòa nhà LowLand Landmark 81, TP.HCM",
    "createdAt": "2026-10-11T13:00:00Z"
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Họ tên rỗng hoặc vượt quá 100 ký tự
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu cập nhật không hợp lệ",
  "data": null,
  "errors": [
    "Họ và tên là bắt buộc"
  ]
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Số điện thoại mới đã bị tài khoản khác sử dụng
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Số điện thoại đã được tài khoản khác sử dụng",
  "data": null,
  "errors": [
    "Số điện thoại đã được tài khoản khác sử dụng"
  ]
}
```

##### ❌ Trạng thái 4: `401 Unauthorized` - Chưa xác thực danh tính
```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Không xác định được danh tính người dùng",
  "data": null,
  "errors": [
    "Không xác định được danh tính người dùng"
  ]
}
```

---

### 8. `POST /api/auth/change-password`
- **Mục đích:** Người dùng tự đổi mật khẩu tài khoản khi đang đăng nhập.
- **Quyền hạn:** `Authenticated`.

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `currentPassword` | `string` | **Có** | Không để trống | Mật khẩu đang sử dụng |
| `newPassword` | `string` | **Có** | Tối thiểu 6 ký tự | Mật khẩu mới |
| `confirmPassword` | `string` | **Có** | Trùng khớp với `newPassword` | Xác nhận mật khẩu mới |

#### 📄 Request Body Mẫu:
```json
{
  "currentPassword": "Admin@123",
  "newPassword": "Admin@NewPass456!",
  "confirmPassword": "Admin@NewPass456!"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Đổi mật khẩu thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Đổi mật khẩu thành công",
  "data": true,
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Mật khẩu xác nhận không khớp
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu đổi mật khẩu không hợp lệ",
  "data": null,
  "errors": [
    "Mật khẩu xác nhận không khớp"
  ]
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Mật khẩu hiện tại không chính xác
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Mật khẩu hiện tại không chính xác",
  "data": false,
  "errors": [
    "Mật khẩu hiện tại không chính xác"
  ]
}
```

##### ❌ Trạng thái 4: `400 Bad Request` - Mật khẩu mới trùng với mật khẩu cũ
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Mật khẩu mới không được trùng với mật khẩu cũ",
  "data": false,
  "errors": [
    "Mật khẩu mới không được trùng với mật khẩu cũ"
  ]
}
```

##### ❌ Trạng thái 5: `401 Unauthorized` - Chưa truyền Bearer Token
```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Không xác định được danh tính người dùng",
  "data": null,
  "errors": [
    "Không xác định được danh tính người dùng"
  ]
}
```

---

### 9. `GET /api/users`
- **Mục đích:** Xem danh sách toàn bộ người dùng, tìm kiếm & phân trang.
- **Quyền hạn:** `Admin` (`[Authorize(Roles = "Admin")]`).

#### 📥 Query Parameters
| Tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Mô tả |
| :--- | :--- | :---: | :---: | :--- |
| `search` | `string` | Không | `null` | Từ khóa tìm kiếm trên tên, email, SĐT |
| `roleName` | `string` | Không | `null` | Lọc theo vai trò (`Customer`, `Staff`, `Admin`) |
| `isActive` | `bool` | Không | `null` | Lọc theo trạng thái hoạt động (`true`/`false`) |
| `pageNumber` | `int` | Không | `1` | Trang hiện tại (>= 1) |
| `pageSize` | `int` | Không | `10` | Kích thước trang (>= 1) |

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Lấy danh sách thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Lấy danh sách người dùng thành công",
  "data": {
    "items": [
      {
        "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "roleId": "11111111-1111-1111-1111-111111111111",
        "roleName": "Admin",
        "email": "admin@lowlandcoffee.com",
        "fullName": "Quản Trị Viên",
        "phoneNumber": "0901122334",
        "avatarUrl": "https://placehold.co/150",
        "address": "123 Nguyễn Huệ, Q.1, TP.HCM",
        "isActive": true,
        "createdAt": "2026-10-11T13:00:00Z",
        "updatedAt": "2026-10-11T13:00:00Z"
      }
    ],
    "totalItems": 1,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `401 Unauthorized` - Chưa truyền Bearer token
```text
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
```

##### ❌ Trạng thái 3: `403 Forbidden` - Người dùng không có vai trò Admin (Role là Customer hoặc Staff)
```text
HTTP/1.1 403 Forbidden
```

---

### 10. `GET /api/users/{id}`
- **Mục đích:** Xem chi tiết một người dùng theo UUID.
- **Quyền hạn:** `Admin`.

#### 📥 Route Parameters
| Tham số | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | **Có** | UUID của tài khoản người dùng cần xem |

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Lấy thông tin thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Lấy thông tin người dùng thành công",
  "data": {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "roleId": "22222222-2222-2222-2222-222222222222",
    "roleName": "Staff",
    "email": "staff@lowlandcoffee.com",
    "fullName": "Thu Ngân 01",
    "phoneNumber": "0902233445",
    "avatarUrl": "https://placehold.co/150",
    "address": "LowLand Landmark 81, Bình Thạnh",
    "isActive": true,
    "createdAt": "2026-10-11T13:00:00Z",
    "updatedAt": "2026-10-11T13:00:00Z"
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `404 Not Found` - ID người dùng không tồn tại
```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy người dùng",
  "data": null,
  "errors": [
    "Không tìm thấy người dùng"
  ]
}
```

---

### 11. `GET /api/users/roles`
- **Mục đích:** Lấy danh sách các vai trò (Roles) trong hệ thống.
- **Quyền hạn:** `Admin`.

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Lấy danh sách vai trò thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Lấy danh sách vai trò thành công",
  "data": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Admin",
      "description": "Quản trị viên toàn quyền hệ thống"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Staff",
      "description": "Nhân viên bán hàng & thu ngân tại quầy"
    },
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "name": "Customer",
      "description": "Khách hàng thành viên đặt đồ uống"
    }
  ],
  "errors": null
}
```

##### ❌ Trạng thái 2: `401 Unauthorized` / `403 Forbidden` - Không đủ thẩm quyền
*(Nếu không có token hoặc không phải quyền Admin)*

---

### 12. `POST /api/users/staff`
- **Mục đích:** Admin tạo tài khoản cho Nhân viên mới (`Staff`).
- **Quyền hạn:** `Admin`.
- **Headers:** `Authorization: Bearer <admin_token>`, `Content-Type: application/json`

#### 📥 Ràng buộc dữ liệu đầu vào
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `fullName` | `string` | **Có** | Tối đa 100 ký tự | Họ và tên nhân viên |
| `email` | `string` | **Có** | Tối đa 255 ký tự, email hợp lệ | Email nhân viên (duy nhất) |
| `phoneNumber` | `string` | Không | Tối đa 20 ký tự | SĐT nhân viên (duy nhất nếu có) |
| `password` | `string` | **Có** | Tối thiểu 6 ký tự | Mật khẩu ban đầu |
| `address` | `string` | Không | Chuỗi văn bản | Địa chỉ / Chi nhánh làm việc |
| `avatarUrl` | `string` | Không | Chuỗi URL | Ảnh đại diện |

#### 📄 Request Body Mẫu:
```json
{
  "fullName": "Nhân Viên Pha Chế Mới",
  "email": "barista02@lowlandcoffee.com",
  "phoneNumber": "0911223344",
  "password": "Staff@123456",
  "address": "LowLand Coffee Chi Nhánh Thảo Điền",
  "avatarUrl": "https://placehold.co/150"
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `201 Created` - Tạo tài khoản Staff thành công
```json
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Tạo tài khoản nhân viên thành công",
  "data": {
    "id": "77777777-7777-7777-7777-777777777777",
    "roleId": "22222222-2222-2222-2222-222222222222",
    "roleName": "Staff",
    "email": "barista02@lowlandcoffee.com",
    "fullName": "Nhân Viên Pha Chế Mới",
    "phoneNumber": "0911223344",
    "avatarUrl": "https://placehold.co/150",
    "address": "LowLand Coffee Chi Nhánh Thảo Điền",
    "isActive": true,
    "createdAt": "2026-10-11T13:00:00Z",
    "updatedAt": "2026-10-11T13:00:00Z"
  },
  "errors": null
}
```

##### ❌ Trạng thái 2: `400 Bad Request` - Dữ liệu rỗng hoặc sai định dạng
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Dữ liệu không hợp lệ",
  "data": null,
  "errors": [
    "Họ và tên là bắt buộc",
    "Email không hợp lệ",
    "Mật khẩu phải có ít nhất 6 ký tự"
  ]
}
```

##### ❌ Trạng thái 3: `400 Bad Request` - Email đã được sử dụng
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Email đã được sử dụng",
  "data": null,
  "errors": [
    "Email đã được sử dụng"
  ]
}
```

##### ❌ Trạng thái 4: `400 Bad Request` - Số điện thoại đã được sử dụng
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Số điện thoại đã được sử dụng",
  "data": null,
  "errors": [
    "Số điện thoại đã được sử dụng"
  ]
}
```

---

### 13. `PUT /api/users/{id}/status`
- **Mục đích:** Khóa hoặc Mở khóa tài khoản người dùng (`isActive`).
- **Quyền hạn:** `Admin`.

#### 📥 Route & Body Parameters
- **Route:** `id` (`Guid`, bắt buộc) - UUID của người dùng cần cập nhật.
- **Body Constraints:**
| Trường | Kiểu dữ liệu | Bắt buộc | Ràng buộc / Validation | Mô tả |
| :--- | :--- | :---: | :--- | :--- |
| `isActive` | `bool` | **Có** | `true` hoặc `false` | `false` = Khóa tài khoản, `true` = Mở khóa |

#### 📄 Request Body Mẫu:
```json
{
  "isActive": false
}
```

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Khóa tài khoản thành công
*(Đồng thời hệ thống tự động hủy toàn bộ Refresh Token của tài khoản)*
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Khóa tài khoản thành công",
  "data": {
    "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "roleId": "33333333-3333-3333-3333-333333333333",
    "roleName": "Customer",
    "email": "customer@gmail.com",
    "fullName": "Nguyễn Văn Khách",
    "phoneNumber": "0903344556",
    "avatarUrl": null,
    "address": null,
    "isActive": false,
    "createdAt": "2026-10-11T13:00:00Z",
    "updatedAt": "2026-10-11T13:00:00Z"
  },
  "errors": null
}
```

##### ✅ Trạng thái 2: `200 OK` - Mở khóa tài khoản thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Mở khóa tài khoản thành công",
  "data": {
    "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "isActive": true,
    ...
  },
  "errors": null
}
```

##### ❌ Trạng thái 3: `404 Not Found` - Không tìm thấy người dùng
```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy người dùng",
  "data": null,
  "errors": [
    "Không tìm thấy người dùng"
  ]
}
```

---

### 14. `DELETE /api/users/{id}`
- **Mục đích:** Xóa cứng tài khoản người dùng vĩnh viễn khỏi Database.
- **Quyền hạn:** `Admin`.

#### 📥 Route Parameters
| Tham số | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `id` | `Guid` | **Có** | UUID của người dùng cần xóa vĩnh viễn |

#### 📤 Chi tiết các Trạng thái Trả về (Responses):

##### ✅ Trạng thái 1: `200 OK` - Xóa cứng thành công
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Xóa tài khoản người dùng thành công (xóa vĩnh viễn)",
  "data": true,
  "errors": null
}
```

##### ❌ Trạng thái 2: `404 Not Found` - Không tìm thấy tài khoản cần xóa
```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy người dùng",
  "data": null,
  "errors": [
    "Không tìm thấy người dùng"
  ]
}
```

##### ❌ Trạng thái 3: `401 Unauthorized` / `403 Forbidden` - Không đủ thẩm quyền Admin
*(Nếu không có token hoặc token thuộc tài khoản không phải Admin)*
