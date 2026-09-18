# Contribution Guidelines - LowLand Coffee

Tài liệu này định nghĩa quy chuẩn làm việc, quy tắc quản lý nhánh Git, quy ước commit và quy trình kiểm thử trước khi tích hợp mã nguồn vào hệ thống **LowLand Coffee**.

---

### 1. Chiến Lược Phân Nhánh (Git Branching Strategy)

Dự án áp dụng mô hình **Git Flow rút gọn**:

| Nhánh | Nguồn tạo (Base) | Merge vào | Mô tả & Quy tắc |
| :--- | :--- | :--- | :--- |
| **`main`** | — | — | Mã nguồn Production/Demo ổn định nhất. **Nghiêm cấm push code trực tiếp.** |
| **`develop`** | `main` | `main` | Nhánh tích hợp chính. Mọi tính năng đều hội tụ tại đây trước khi release. |
| **`feature/*`** | `develop` | `develop` | Phát triển chức năng mới.<br>• *Cú pháp:* `feature/<ten-tinh-nang>`<br>• *Ví dụ:* `feature/menu-page`, `feature/gateway-routing`, `feature/product-repository` |
| **`bugfix/*`** | `develop` | `develop` | Sửa lỗi phát sinh khi tích hợp trên `develop`.<br>• *Cú pháp:* `bugfix/<ten-loi>`<br>• *Ví dụ:* `bugfix/cors-gateway`, `bugfix/cart-session-issue` |
| **`hotfix/*`** | `main` | `main` & `develop` | Xử lý sự cố khẩn cấp trên Production.<br>• *Cú pháp:* `hotfix/<ten-su-co>` |

---

### 2. Quy Chuẩn Commit (Conventional Commits)

Định dạng bắt buộc:

```text
<type>(<scope>): <mô tả ngắn gọn bằng tiếng Anh hoặc tiếng Việt>
```

#### Types quy định:
* `feat`: Bổ sung tính năng mới.
* `fix`: Sửa lỗi logic hoặc giao diện.
* `refactor`: Tái cấu trúc mã nguồn (không đổi logic, không thêm tính năng).
* `docs`: Cập nhật tài liệu, Markdown.
* `chore`: Cập nhật dependency, build tool, cấu hình CI/CD.
* `docker`: Chỉnh sửa Dockerfile, `docker-compose.yml` hoặc container env.

#### Scopes đề xuất:
`web`, `gateway`, `api`, `repository`, `domain`, `db`, `compose`, `docs`

> **Ví dụ hợp lệ:**
> * `feat(web): add menu razor page with category filtering`
> * `fix(gateway): resolve CORS error for local razor client`
> * `refactor(repository): implement unit of work pattern in product service`
> * `docker(compose): update sql server healthcheck configuration`

---

### 3. Quy Chuẩn Viết Mã (Coding Standards)

#### C# / .NET 8
* **Naming Convention:** Tuân thủ chuẩn Microsoft: `PascalCase` cho tên Class, Interface (`I...`), Method, Property; `_camelCase` cho private readonly fields.
* **Async-First:** Tận dụng `async`/`await` xuyên suốt từ Controller xuống Service và Repository (`Task<T>`).
* **Bảo mật:** Tuyệt đối không hardcode Connection Strings hay API Keys. Dùng `appsettings.json` kết hợp **User Secrets** (local) hoặc **Environment Variables** (Docker).

#### Razor Web Frontend
* **Mô hình PageModel:** Tách riêng file `.cshtml` (giao diện HTML + Tag Helpers) và `.cshtml.cs` (logic PageModel C#).
* **Routing API:** Luôn inject `HttpClient` đã cấu hình `BaseAddress` trỏ qua **API Gateway (YARP)** (`http://localhost:8080`), không gọi trực tiếp endpoint nội bộ của Backend API.

#### Repository Pattern
* **Phân tách trách nhiệm:** Tách biệt rõ giữa `Core` (Entities, Interfaces), `Infrastructure` (Data, Repositories), `Application` (DTOs, Services), và `Controllers` (API Endpoints).

---

### 4. Checklist Trước Khi Tạo Pull Request (PR)

Lập trình viên cần tự kiểm tra và tích chọn hoàn tất các mục sau trước khi tạo PR vào `develop`:

- [ ] **Build:** Đã chạy `dotnet build LowLandCoffee.sln` đạt `0 errors`, `0 warnings` nghiêm trọng.
- [ ] **Docker Local:** Đã chạy `docker compose up -d --build`, toàn bộ containers khởi động ở trạng thái `running` hoặc `healthy`.
- [ ] **Testing:** API mới đã được verify thành công trên Swagger / Postman / Browser.
- [ ] **Bảo mật:** Không commit nhầm file nhạy cảm (`.env`, `secrets.json`, thư mục bin/obj).
- [ ] **Đồng bộ mã nguồn:** Đã kéo code mới nhất từ `develop` về và giải quyết xung đột (conflict):

#### Lệnh đồng bộ trên Windows (PowerShell / CMD) & Linux (Bash):
```bash
git checkout develop
git pull origin develop
git checkout feature/<ten-nhanh>
git merge develop
```

---

### 5. Quy Trình Review & Merge Pull Request

1. **Review:** Bắt buộc có ít nhất **1 approval** từ thành viên khác trong nhóm mới đủ điều kiện merge.
2. **Nội dung mô tả PR:**
   * Tóm tắt tính năng vừa thêm hoặc lỗi vừa xử lý.
   * Hướng dẫn test cụ thể (Endpoint cần gọi, body mẫu, trang web cần mở).
3. **Chiến lược Merge:** Sử dụng **Squash and Merge** để giữ lịch sử commit trên nhánh `develop` luôn gọn gàng và dễ theo dõi.