# 11-ai-review.md - Tiêu Chuẩn & Prompt Hướng Dẫn AI Review / Viết Code (LowLand Coffee)

Tài liệu cung cấp các bộ tiêu chuẩn kỹ thuật chi tiết (System Prompts) dùng để hướng dẫn AI (ChatGPT, Claude, Gemini, GitHub Copilot, Antigravity) review code hoặc sinh mã nguồn đúng chuẩn kiến trúc của dự án **LowLand Coffee (PRN232)**.

---

## 📑 Mục Lục
1. [Bộ Tiêu Chuẩn Chung (Core Principles)](#1-bộ-tiêu-chuẩn-chung-core-principles)
2. [Bộ Tiêu Chuẩn Backend: .NET 8 Microservices Web API (C# 12)](#2-bộ-tiêu-chuẩn-backend-net-8-microservices-web-api-c-12)
3. [Bộ Tiêu Chuẩn Frontend: ASP.NET Core Razor Pages (.NET 8)](#3-bộ-tiêu-chuẩn-frontend-aspnet-core-razor-pages-net-8)
4. [Bộ Tiêu Chuẩn Mobile App: Flutter (Dart)](#4-bộ-tiêu-chuẩn-mobile-app-flutter-dart)

---

## 1. Bộ Tiêu Chuẩn Chung (Core Principles)

```text
Hãy review / viết code theo các tiêu chuẩn kỹ thuật sau:

1. Kiến trúc & Thiết kế:
- Tuân thủ nghiêm ngặt nguyên lý SOLID, DRY, KISS, YAGNI.
- Phân tách rõ ràng trách nhiệm (Separation of Concerns), cấu trúc module/hàm mạch lạc, tránh coupling cao.

2. Clean Code & Tối ưu:
- Tuyệt đối không hard-code (dùng hằng số/enum/config rõ ràng cho Magic Numbers, Magic Strings).
- Đặt tên định danh (hàm, biến, class) rõ nghĩa, thể hiện đúng ngữ cảnh và ý định, tự giải thích code (Self-documenting).
- Xử lý triệt để các edge cases, exception handling chặt chẽ, an toàn luồng và tối ưu hiệu năng/bộ nhớ.

3. Quy chuẩn Comment:
- Không giải thích "How" hoặc dịch lại cú pháp hiển nhiên.
- Tập trung làm rõ "Why" (lý do thiết kế, quyết định kỹ thuật, edge case phức tạp) và "What" (mục tiêu tổng quát/hợp đồng giao tiếp của module/hàm).

Yêu cầu đầu ra: Cung cấp code hoàn thiện, chuẩn convention của ngôn ngữ và giải thích ngắn gọn các điểm refactor/lựa chọn thiết kế quan trọng.
```

---

## 2. Bộ Tiêu Chuẩn Backend: .NET 8 Microservices Web API (C# 12)

```text
Hãy review / viết code C# (.NET 8 Web API) cho hệ thống Microservices LowLand Coffee theo các tiêu chuẩn kỹ thuật sau:

1. Kiến trúc & Phân tầng (4-Layer Pattern):
- Tuân thủ nghiêm ngặt mô hình 4 tầng độc lập:
  • Controller: Chỉ tiếp nhận HTTP Request, validate ModelState và chuyển tiếp cho Service. Trả về chuẩn ApiResponse<T>. Không chứa logic nghiệp vụ.
  • Service: Xử lý toàn bộ Business Logic, Workflow, Transaction, mapping DTO <-> Entity, tương tác qua IRepository.
  • Repository: Đảm nhiệm truy vấn dữ liệu CRUD với Database qua EF Core DbContext. Tách biệt hoàn toàn với Controller.
  • DbContext & Entity: Quản lý cấu hình Entity, Fluent API, Database-per-service (Mỗi service sở hữu 1 database riêng, không join chéo database).
- Toàn bộ dependencies bắt buộc inject qua Dependency Injection (Constructor Injection), ưu tiên Interface-based programming.

2. Clean Code C# 12 & Hiệu năng EF Core 8:
- Bất đồng bộ (Async/Await): Sử dụng async/await chuẩn chỉ từ Controller xuống Database (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`). Tuyệt đối không dùng `.Result` hoặc `.Wait()` gây deadlock.
- Tối ưu EF Core:
  • Bắt buộc dùng `.AsNoTracking()` cho toàn bộ truy vấn chỉ đọc (Read-only queries) để tránh overhead tracking.
  • Tránh lỗi N+1 Query: Sử dụng `.Include()` / `.ThenInclude()` hợp lý hoặc chiếu thẳng sang DTO qua `.Select()`.
  • Phân trang bắt buộc: Áp dụng `.Skip().Take()` cho toàn bộ API lấy danh sách dữ liệu.
- C# 12 Idiomatic: Tận dụng Primary Constructors, File-scoped Namespaces, Record Types cho DTOs/Requests, Pattern Matching và Nullable Reference Types (`#nullable enable`).
- Không hard-code: Đặt tên bảng, hằng số cấu hình, role, status trong `Constants/`, `Enums/` hoặc lấy từ `IConfiguration`.
- Xử lý lỗi & Validation:
  • Sử dụng Middleware xử lý Exception tập trung (Global Exception Handler / ProblemDetails), không try-catch bừa bãi chỉ để swallow lỗi.
  • Chuẩn hóa dữ liệu đầu ra: Toàn bộ API response bọc trong wrapper `ApiResponse<T>` (`isSuccess`, `statusCode`, `message`, `data`, `errors`).

3. Bảo mật & Giao tiếp Gateway:
- Bảo mật API: Xác thực JWT Bearer Token, phân quyền theo Role/Claims (`[Authorize(Roles = "...")]`). Mã hóa mật khẩu bằng BCrypt/Argon2 (không dùng MD5/SHA1).
- Tương thích YARP Gateway: Controller route tuân theo chuẩn định tuyến của Gateway (ví dụ: `api/auth`, `api/products`, `api/orders`).

4. Quy chuẩn Comment:
- Dùng XML Documentation Comments (`/// <summary>`) cho tất cả public Interfaces, Services, Repositories và Controller Action methods để làm rõ mục đích ("What"), tham số, mã lỗi HTTP và kiểu trả về.
- Dùng inline comment (`//`) để giải thích nguyên nhân ("Why") tại các thuật toán tính toán giá, chiết khấu voucher, xử lý concurrency hoặc mã hóa token.

Yêu cầu đầu ra: Code C# hoàn chỉnh, tuân thủ C# Coding Conventions, tối ưu hiệu năng EF Core và giải thích ngắn gọn các quyết định kiến trúc/bảo mật.
```

---

## 3. Bộ Tiêu Chuẩn Frontend: ASP.NET Core Razor Pages (.NET 8)

```text
Hãy review / viết code ASP.NET Core Razor Pages (.NET 8) cho giao diện Web LowLand Coffee theo các tiêu chuẩn kỹ thuật sau:

1. Kiến trúc & Tổ chức Mã nguồn (Feature Folders):
- Tổ chức theo Feature Folders trong thư mục `Pages/`:
  • `Pages/Auth/`: Login, Register, ForgotPassword, Profile.
  • `Pages/Menu/`: Index, Details.
  • `Pages/Orders/`: Cart, Checkout, Tracking, History.
  • `Pages/Customer/`: Loyalty, Vouchers.
  • `Pages/Notifications/`: Index.
  • `Pages/Staff/`: POS Orders realtime.
  • `Pages/Admin/`: Dashboard, Quản lý Menu/Vouchers/Users.
- Phân tách vai trò rõ ràng:
  • File View (`.cshtml`): Chỉ chứa HTML5 ngữ nghĩa, Bootstrap 5 / Vanilla CSS, Tag Helpers, Render logic đơn giản. Tuyệt đối không viết mã C# logic phức tạp trong View.
  • File PageModel (`.cshtml.cs`): Tiếp nhận request, bind thuộc tính `[BindProperty]`, kiểm tra `ModelState.IsValid`, gọi `Typed HttpClient Services` và trả về `Page()`, `RedirectToPage()`, hoặc `JsonResult`.
  • Tầng Services (`Services/`): Toàn bộ thao tác gửi HTTP request sang Backend bắt buộc thông qua Typed `HttpClient` (`IAuthHttpService`, `IProductHttpService`, v.v.) hướng về YARP API Gateway (`http://localhost:5000`). Tuyệt đối không kết nối trực tiếp DB ở tầng Frontend.

2. Clean Code Razor Pages & Trải nghiệm Người dùng (UX):
- Form Validation: Sử dụng Data Annotations trong ViewModel (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[Compare]`) kết hợp Client-side validation (`jquery.validate.unobtrusive`) và Server-side validation.
- Quản lý State & Xác thực: Quản lý đăng nhập bằng Cookie Authentication (`CookieAuthenticationDefaults`), lưu trữ Claims (UserId, FullName, Role, Token), giỏ hàng tạm lưu trong Session hoặc Encrypted Cookie.
- Realtime SignalR: Tách logic kết nối SignalR (`signalr.min.js`) vào file JS riêng trong `wwwroot/js/`, lắng nghe các event đơn hàng/thông báo và hiển thị Toast/chuông thông báo linh hoạt.
- Tái sử dụng giao diện: Tách nhỏ UI dùng chung thành Partial Views (`_Layout.cshtml`, `_Header.cshtml`, `_Footer.cshtml`, `_NotificationBell.cshtml`) hoặc View Components.
- Không hard-code URL: Sử dụng Tag Helpers `asp-page`, `asp-route-*`, `asp-page-handler` để tạo URL an toàn.

3. Quy chuẩn Comment:
- Dùng XML Comments (`///`) cho các methods trong Typed HttpClients và PageModel Handlers (`OnGetAsync`, `OnPostAsync`).
- Dùng Razor Comment `@* ... *@` trong file `.cshtml` để giải thích mục đích phân khối layout/UI phức tạp.

Yêu cầu đầu ra: Cung cấp mã nguồn `.cshtml` và `.cshtml.cs` hoàn chỉnh, chuẩn ngữ nghĩa, UI/UX hiện đại và giải thích ngắn gọn luồng tương tác với API Gateway.
```

---

## 4. Bộ Tiêu Chuẩn Mobile App: Flutter (Dart)

```text
Hãy review / viết code Flutter (Dart) theo các tiêu chuẩn kỹ thuật sau:

1. Kiến trúc & Thiết kế:
- Tuân thủ nguyên lý SOLID, DRY, KISS và YAGNI.
- Áp dụng phân tầng sạch (Data - Domain - Presentation / Feature-first), phân tách triệt để UI layout và Business Logic.
- Tách nhỏ Widget (dưới 100-150 dòng), ưu tiên composition hơn inheritance, dùng const constructor mọi nơi có thể để tối ưu rebuild pipeline.

2. Clean Code Dart & Hiệu năng:
- Tuyệt đối không hard-code (dùng constants, AppSizes, AppColors, TextStyle tokens, Localization / .arb).
- Đặt tên chuẩn Dart Style Guide: camelCase cho member/variable, PascalCase cho class/enum/typedef, snake_case cho file name.
- Quản lý State rõ ràng (immutability, unidirectional data flow), dispose triệt để Controllers/Streams/Listeners để tránh memory leak.
- Xử lý bất đồng bộ chặt chẽ: bắt buộc handle full case (loading, success, failure/error), không swallow exception trong try-catch, dùng typed Result/Either hoặc Custom Exceptions.

3. Quy chuẩn Comment:
- Dùng Doc Comments (///) cho public API/Widget/Method để mô tả "What" (mục đích, tham số, kết quả).
- Dùng inline comment (//) chỉ khi cần giải thích "Why" (lý do chọn giải pháp kỹ thuật, workaround cho bug framework, hoặc xử lý edge case đặc biệt). Không mô tả lại code làm như thế nào (How).

Yêu cầu đầu ra: Cung cấp code Dart hoàn chỉnh, đúng convention, tối ưu hiệu năng và giải thích ngắn gọn các quyết định kỹ thuật quan trọng.
```