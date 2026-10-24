# ⚙️ Các Cơ Chế Của API Gateway (Gateway Mechanisms & Capabilities)

Tài liệu này giải thích chi tiết **9 cơ chế cốt lõi** được cài đặt và vận hành bên trong **ApiGateway** của hệ thống **LowLand Coffee**.

---

## 1. Cơ Chế Định Tuyến Động (Reverse Proxy & Dynamic Routing)

* **Công nghệ cốt lõi:** **YARP (Yet Another Reverse Proxy 2.1)** do Microsoft phát triển trực tiếp trên HTTP socket pipeline của ASP.NET Core.
* **Cơ chế hoạt động:**
  * **Catch-all Matching:** Sử dụng cú pháp `/api/{service}/{**catch-all}` để bắt trọn vẹn toàn bộ đường dẫn con và query parameters từ client và chuyển tiếp nguyên vẹn tới microservice tương ứng.
  * **Path Transformation:** Cho phép biến đổi URL trên đường truyền. Ví dụ: URL `/swagger-docs/auth/v1/swagger.json` sẽ được tước tiền tố (`PathRemovePrefix: /swagger-docs/auth`) và gán tiền tố mới (`PathPrefix: /swagger`) trước khi gửi đến `Auth.API`.
  * **Hỗ trợ Streaming & WebSocket:** Chuyển tiếp luồng nhị phân và kết nối TCP/WebSocket liên tục hai chiều mà không làm gián đoạn handshake.

---

## 2. Cơ Chế Chống Giả Mạo Header (Perimeter Security & Anti-Spoofing)

* **Vấn đề an ninh:** Trong kiến trúc Microservices, nếu Microservice nội bộ tin tưởng mù quáng vào các header định danh như `X-User-Id` hay `X-User-Role`, kẻ tấn công bên ngoài Internet có thể tự gắn các header này vào request để mạo danh Admin (`Header Injection/Spoofing`).
* **Cơ chế giải quyết:**
  * Tại [JwtValidationMiddleware.cs](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/Middleware/JwtValidationMiddleware.cs), Gateway thực hiện thao tác **tước bỏ triệt để** ngay từ đầu pipeline:
    ```csharp
    context.Request.Headers.Remove("X-User-Id");
    context.Request.Headers.Remove("X-User-Role");
    context.Request.Headers.Remove("X-User-Email");
    ```
  * Chỉ khi nào client cung cấp một JWT hợp lệ và chữ ký được giải mã thành công, Gateway mới tạo mới lại các header này từ Claims chính chủ.

---

## 3. Cơ Chế Xác Thực Chữ Ký JWT & Phân Phối Ngữ Cảnh (Edge Authentication & Context Propagation)

* **Xác thực chữ ký số (Signature Verification):**
  * Gateway sử dụng `TokenValidationParameters` cùng khóa đối xứng bí mật (`JwtSettings:SecretKey`) theo chuẩn thuật toán HMAC-SHA256.
  * Kiểm tra đầy đủ: Tính hợp lệ của chữ ký (`ValidateIssuerSigningKey`), nhà phát hành (`ValidateIssuer`), đối tượng nhận (`ValidateAudience`) và thời hạn sống của token (`ValidateLifetime`).
  * Nếu token hết hạn hoặc bị sửa đổi payload, Gateway từ chối ngay lập tức với mã `401 Unauthorized` (Fail Fast), bảo vệ các microservice phía sau khỏi các cuộc tấn công quét token rác.
* **Hỗ trợ SignalR WebSocket:**
  * Giao thức WebSocket của trình duyệt không cho phép tùy biến header `Authorization` trong quá trình Handshake.
  * Gateway tự động kiểm tra query string `?access_token=...` trên các route `/hubs/*` và đóng gói ngược lại thành header `Authorization: Bearer <token>` để các service downstream nhận diện bình thường.
* **Mô hình Hybrid (Defense-in-Depth):**
  * Sau khi xác thực, Gateway **vẫn giữ nguyên** header `Authorization: Bearer <token>` khi forward xuống downstream. Điều này cho phép các microservice nội bộ vừa có thể đọc nhanh `X-User-Id`, vừa có thể sử dụng các Policy/Role attribute chuẩn của ASP.NET Core (`[Authorize(Roles = "Admin")]`).

---

## 4. Cơ Chế Giới Hạn Tần Suất Đa Phân Vùng (Granular Partitioned Rate Limiting)

* **Công nghệ:** `System.Threading.RateLimiting` của .NET 8 kết hợp thuật toán **Fixed-Window Limiter**.
* **Đa phân vùng độc lập (Partitioning):**
  * Hạn mức không bị chia sẻ chung chung giữa toàn server, mà được phân vùng riêng biệt theo bộ khóa:
    $$\text{PartitionKey} = \text{Client IP} + \text{HTTP Method} + \text{Target Path}$$
  * Điều này đảm bảo: Một client spam liên tục vào endpoint `/api/auth/login` sẽ bị khóa tạm thời endpoint đó, nhưng **vẫn có thể** duyệt xem danh sách sản phẩm `/api/products` bình thường.
* **Cấu hình động phân tán (Dynamic Config):**
  * Gateway quét và nạp toàn bộ các file `*.ratelimit.json` từ thư mục `RateLimits/` khi khởi động.
  * Từng service có quyền định nghĩa quota riêng mà không gây xung đột code (Merge Conflict) giữa các thành viên.
* **Phản hồi chuẩn khi vượt hạn mức:**
  * Tự động trả về mã `429 Too Many Requests` với thông điệp rõ ràng theo định dạng `ApiResponse<T>` chuẩn của dự án.

---

## 5. Cơ Chế Chống Chịu Lỗi & Chuẩn Hóa Lỗi Downstream (Resilience & Error Normalization)

* **HttpRequest Timeout (Chống sập dây chuyền - Cascading Failure):**
  * Cấu hình thời gian chờ tối đa `Timeout: "00:00:30"` (30 giây) cho tất cả các cluster trong [yarp.json](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/yarp.json).
  * Nếu một microservice bị treo do deadlock database hoặc nghẽn mạng, kết nối sẽ bị ngắt sau 30 giây để giải phóng Connection Pool cho Gateway, tránh làm treo cả hệ thống.
* **Bắt lỗi chuyển tiếp YARP (`IForwarderErrorFeature`):**
  * Tại [GatewayResponseHandlerMiddleware.cs](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/Middleware/GatewayResponseHandlerMiddleware.cs), Gateway bắt các mã lỗi chuyển tiếp và chuyển đổi thành thông điệp thân thiện:
    * `NoAvailableDestinations` $\rightarrow$ **HTTP 503 Service Unavailable**: "Không có instance nào của Microservice đang khả dụng."
    * `RequestTimedOut` $\rightarrow$ **HTTP 504 Gateway Timeout**: "Yêu cầu tới Microservice nội bộ đã hết hạn thời gian (Timeout)."
    * Lỗi kết nối / Service chết $\rightarrow$ **HTTP 502 Bad Gateway**: "Không thể kết nối đến máy chủ Microservice nội bộ."
  * Tất cả đều được trả về dưới dạng JSON `ApiResponse<object>` có cấu trúc đồng nhất, giúp Client dễ dàng bắt lỗi và hiển thị thông báo thân thiện cho người dùng.

---

## 6. Cơ Chế Kiểm Vết Phân Tán (Distributed Tracing với Correlation ID)

* **Chuẩn định danh:** `X-Correlation-Id` (định dạng GUID không dấu gạch nối `N`).
* **Cơ chế hoạt động:**
  1. Khi một request đi vào Gateway, [CorrelationIdMiddleware.cs](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/Middleware/CorrelationIdMiddleware.cs) kiểm tra xem client có gửi kèm header `X-Correlation-Id` hay không.
  2. Nếu không có, Gateway tự động sinh một mã ngẫu nhiên duy nhất (ví dụ: `8a7b9c6d4e5f...`).
  3. Gắn mã này vào Request Header để YARP chuyển tiếp xuống các Microservice phía sau.
  4. Gắn mã này vào Response Header để trả về cho Client.
  5. Bao bọc luồng ghi nhật ký bằng `_logger.BeginScope(new Dictionary { ["CorrelationId"] = ... })`, giúp mọi dòng log sinh ra trong suốt vòng đời của request đều chứa sẵn Correlation ID, cực kỳ thuận tiện khi tra cứu lỗi trên Docker/K8s logs.

---

## 7. Cơ Chế Quản Lý CORS Tập Trung (Centralized CORS Management)

* **Vấn đề:** Thay vì cấu hình CORS riêng lẻ ở 5 Microservice (dễ gây thiếu sót hoặc sai lệch chính sách), Gateway đóng vai trò là điểm duy nhất kiểm soát CORS.
* **Cấu hình chuyên sâu:**
  * Đọc danh sách tên miền được phép từ `Cors:AllowedOrigins` (hỗ trợ cả `localhost:5000`, `localhost:8080` của Razor Pages).
  * Kích hoạt `AllowCredentials()`: Bắt buộc để truyền tải Cookie và cho phép kết nối WebSocket/SignalR hoạt động trơn tru.
  * Kích hoạt `WithExposedHeaders`: Cho phép trình duyệt của Client có thể đọc được các Header phản hồi đặc biệt như `X-Correlation-Id`, `Content-Disposition` (tải file) và `X-Pagination` (phân trang).

---

## 8. Cơ Chế Cổng Tài Liệu OpenAPI Hợp Nhất (Unified Swagger UI Portal)

* **Vấn đề:** Với 5 Microservices chạy trên 5 port nội bộ khác nhau (`5001` đến `5005`), người dùng hoặc lập trình viên frontend rất bất tiện khi phải mở 5 tab trình duyệt khác nhau để xem tài liệu.
* **Cơ chế hợp nhất:**
  * Gateway kích hoạt `UseSwaggerUI` tại đường dẫn `/swagger`.
  * Đăng ký 5 Endpoint tài liệu JSON thông qua đường dẫn đại diện của Gateway:
    * `/swagger-docs/auth/v1/swagger.json`
    * `/swagger-docs/product/v1/swagger.json`
    * `/swagger-docs/order/v1/swagger.json`
    * `/swagger-docs/engagement/v1/swagger.json`
    * `/swagger-docs/notification/v1/swagger.json`
  * YARP chịu trách nhiệm âm thầm lấy các file `swagger.json` từ từng service nội bộ và truyền về giao diện chung, cho phép người dùng chuyển đổi qua lại giữa các service chỉ bằng một dropdown menu duy nhất.

---

## 9. Cơ Chế Đo Lường Độ Trễ & Giám Sát Trạng Thái (Diagnostics & Latency Monitoring)

* **Đo lường thời gian xử lý (Latency Tracking):**
  * [RequestLoggingMiddleware.cs](file:///home/bettercallkhang/Github/LowLand-Coffee/src/backend/gateways/ApiGateway/Middleware/RequestLoggingMiddleware.cs) sử dụng `Stopwatch` đo chính xác từng mili-giây (ms) từ khi request tới Gateway đến khi nhận được byte cuối cùng từ Microservice downstream.
  * Phân loại log theo màu/cấp độ cảnh báo: Response `2xx/3xx` (Information), `4xx` (Warning), `5xx` (Error).
* **Endpoints Giám Sát:**
  * **`/health`**: Cung cấp trạng thái Liveness/Readiness theo chuẩn ASP.NET Core Health Checks (phù hợp cho Docker/Kubernetes health probing).
  * **`/` (Root Diagnostic Dashboard)**: Trả về trạng thái chi tiết của Gateway: Phiên bản, môi trường, thời gian uptime, danh sách đầy đủ các Routes đang hoạt động và địa chỉ các Clusters kết nối.
