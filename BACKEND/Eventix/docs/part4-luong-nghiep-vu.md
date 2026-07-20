# TÀI LIỆU ĐẶC TẢ HỆ THỐNG EVENTIX
## Phần 4: Luồng Nghiệp Vụ, Bảo Mật & Hạ Tầng

---

## 1. Luồng Nghiệp Vụ Chính

### 1.1 Luồng Đăng Ký & Xác Thực Email

```
[User]
  │
  ├─ POST /api/auth/register (email, password, fullName, phone)
  │       │
  │       ├─ Validate: email unique, phone unique, password ≥ 6 chars
  │       ├─ Hash password bằng BCrypt
  │       ├─ Tạo User với Status=INACTIVE, EmailVerified=false
  │       ├─ Assign Role = "Customer"
  │       └─ Gửi email OTP 6 số qua MailKit
  │
  ├─ POST /api/auth/verify-otp (email, otp)
  │       │
  │       ├─ Kiểm tra OTP hợp lệ và chưa hết hạn
  │       ├─ Set EmailVerified=true, Status=ACTIVE
  │       └─ Trả về JWT + RefreshToken (đăng nhập ngay)
  │
  └─ POST /api/auth/login (email, password)
          │
          ├─ Kiểm tra email + verify BCrypt hash
          ├─ Kiểm tra EmailVerified=true (nếu chưa → 400)
          ├─ Generate JWT (claims: userId, email, roles)
          ├─ Generate RefreshToken (lưu vào UserRefreshToken)
          └─ Trả về AccessToken + RefreshToken
```

### 1.2 Luồng Trở Thành Organizer

```
[User đã đăng nhập]
  │
  ├─ POST /api/OrganizerProfile/create
  │       { organizationName, description, contactEmail, contactPhone }
  │       └─ Tạo OrganizerProfile với Status=Pending
  │
[Admin]
  │
  ├─ GET /api/OrganizerProfile/organizer-profiles?status=Pending
  │       └─ Xem danh sách chờ duyệt
  │
  ├─ PATCH /api/OrganizerProfile/{id}/approve
  │       └─ Status → Approved, lưu ApprovedBy + ApprovedAt
  │
  └─ PATCH /api/OrganizerProfile/{id}/reject
          └─ Status → Rejected
```

### 1.3 Luồng Tạo Sự Kiện (Event Wizard)

```
[Organizer - Eventix.Web]
  │
  Step 1: Nhập thông tin sự kiện
  │   title, description, startTime, endTime, categoryId
  │   → Lưu vào Session["EventWizard_Info"]
  │
  Step 2: Chọn Venue
  │   → GET /api/venue/venues
  │   → Lưu venueId vào Session
  │
  Step 3: Cấu hình Zones
  │   → GET/POST /api/VenueZone/venue/{venueId}
  │   → Tạo/chỉnh sửa các Zone màu sắc, có ghế hay không
  │
  Step 4: Import/Generate Seats
  │   → POST /api/Seat/{venueId}/import-excel  (upload .xlsx)
  │   hoặc POST /api/Seat/venue/{venueId}/generate
  │
  Step 5: Tạo TicketTypes
  │   → POST /api/OrganizerProfile/events/{eventId}/ticket-types
  │   { name, price, quantity, saleStartTime, saleEndTime, isSeatRequired }
  │
  Step 6: Gán TicketType vào Zone/Seats
  │   → Gán VenueZoneId cho TicketType
  │
  Step 7: Review & Submit
      → POST /api/events/create
      { title, venueId, categoryId, startTime, endTime, status, ... }
      └─ Validate: Organizer approved, Venue exists, Category exists,
                   no time conflict at Venue
         → Tạo Event với Status=Draft
         → Slug tự động sinh (slugify(title) + suffix)
```

### 1.4 Luồng Publish Sự Kiện

```
[Organizer]
  │
  ├─ POST /api/events/{id}/publish
  │       └─ Set PublishedAt = now (hoặc thời điểm tương lai)
  │
[EventStatusJob - mỗi 1 phút]
  │
  └─ Kiểm tra: PublishedAt <= now && Status=Draft
       → Status = Published
       → SignalR broadcast: EventStatusChanged
```

### 1.5 Luồng Tự Động Cập Nhật Trạng Thái Sự Kiện

`EventStatusJob` chạy mỗi 1 phút, áp dụng logic sau:

```
Input: Event entity + current DateTime

IF EndTime <= now               → Completed
IF StartTime <= now < EndTime   → Ongoing
IF Status == Draft
    IF PublishedAt <= now       → Published
    ELSE                        → Draft (giữ nguyên)
IF tất cả TicketType hết vé     → SoldOut
IF có TicketType đang trong sale window → OnSale
ELSE                            → Published

Nếu status thay đổi:
    → Cập nhật DB
    → SignalR broadcast tới tất cả clients
```

### 1.6 Luồng Đặt Vé (Dự kiến)

```
[Customer]
  │
  1. Xem sự kiện: GET /api/events/{id}/booking
  │   → Trả về danh sách TicketType + ghế khả dụng
  │
  2. Thêm vào giỏ hàng (CartItem)
  │   → Có thể chọn ghế nếu IsSeatRequired=true
  │
  3. Tạo Reservation (giữ chỗ tạm, ~15 phút)
  │   → TicketType.ReservedQuantity += quantity
  │   → EventSeatStatus = Reserved
  │
  4. Áp dụng Coupon (tùy chọn)
  │   → Validate: code valid, in time range, usageLimit chưa đạt
  │
  5. Tạo Order
  │   → SubTotal, ServiceFee, DiscountAmount, TotalAmount
  │   → Status = Pending, ExpiresAt = now + 15 phút
  │
  6. Thanh toán: POST /api/Payments
  │   → Redirect tới Payment Gateway (VNPay/MoMo)
  │
  7. Webhook callback từ Gateway
  │   → Xác nhận thanh toán thành công
  │   → Order.Status = Paid, Order.PaidAt = now
  │   → TicketType.SoldQuantity += quantity
  │   → Tạo Ticket (phát hành vé) với QrToken unique
  │   → CouponUsage được ghi nhận
  │
  8. Xem vé: GET /api/Tickets
      → Download PDF vé hoặc hiển thị QR
```

### 1.7 Luồng Check-In

```
[Staff tại sự kiện]
  │
  ├─ Quét QR code trên vé
  │
  ├─ POST /api/CheckIn
  │   { qrToken, eventId, method: "QR" }
  │   │
  │   ├─ Validate: QrToken tồn tại, Ticket thuộc eventId
  │   ├─ Validate: Ticket.Status = Active (chưa dùng)
  │   ├─ Validate: Sự kiện đang ở trạng thái OnSale/Ongoing
  │   ├─ Set Ticket.Status = Used, CheckedInAt = now
  │   └─ Tạo CheckInLog { ticketId, eventId, checkedInBy, method }
  │
  └─ Trả về kết quả check-in (tên người, loại vé, số ghế)
```

### 1.8 Luồng Hoàn Tiền

```
[Customer]
  │
  ├─ POST /api/Refunds
  │   { orderId, reason, refundType: "Full/Partial" }
  │   └─ Tạo RefundRequest với Status=Pending
  │
[Admin]
  │
  ├─ PATCH /api/Refunds/{id}/approve
  │   └─ Status = Approved, ReviewedBy, ReviewedAt
  │   └─ Gọi Payment Gateway để hoàn tiền
  │   └─ Order.Status = Refunded
  │   └─ Ticket.Status = Cancelled
  │
  └─ PATCH /api/Refunds/{id}/reject
      └─ Status = Rejected
```

---

## 2. Bảo Mật

### 2.1 Authentication - JWT Bearer

**Cấu hình validation:**
- `ValidateIssuerSigningKey = true` — Xác thực chữ ký
- `ValidateIssuer = true` — Xác thực Issuer
- `ValidateAudience = true` — Xác thực Audience
- `ValidateLifetime = true` — Kiểm tra expiry
- `ClockSkew = TimeSpan.Zero` — Không có tolerance

**Token lifecycle:**
- AccessToken: thời hạn ngắn (cấu hình trong `JwtSettings.ExpiresInMinutes`)
- RefreshToken: thời hạn dài hơn, lưu DB, có thể revoke khi logout

**Claims trong JWT:**
- `ClaimTypes.NameIdentifier` → UserId (Guid)
- `ClaimTypes.Email` → Email
- `ClaimTypes.Role` → danh sách vai trò

### 2.2 Authorization - Role-based & Policy-based

```csharp
// Role-based (sử dụng trong controller)
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Organizer")]

// Policy-based
[Authorize(Policy = "OrganizerOnly")]
[Authorize(Policy = "AdminOrOrganizer")]
```

**Policy định nghĩa:**
```
AdminOnly       = RequireRole("Admin")
OrganizerOnly   = RequireRole("Organizer")
CustomerOnly    = RequireRole("Customer")
AdminOrOrganizer = RequireRole("Admin") OR RequireRole("Organizer")
```

### 2.3 Xử Lý Lỗi Tập Trung

`GlobalExceptionHandlerMiddleware` bắt tất cả exception và trả về response chuẩn:

| Exception type | HTTP Status | Mô tả |
|---|---|---|
| `BadRequestException` | 400 | Lỗi validation nghiệp vụ |
| `UnauthorizedException` | 401 | Chưa xác thực |
| `ForbiddenException` | 403 | Không có quyền |
| `NotFoundException` | 404 | Không tìm thấy resource |
| `ApiException` (base) | Theo code | Lỗi nghiệp vụ có code cụ thể |
| `Exception` (unhandled) | 500 | Lỗi server |

**Response format lỗi:**
```json
{
  "code": "EMAIL_ALREADY_EXISTS",
  "message": "Email đã được sử dụng",
  "data": null
}
```

### 2.4 Password Security

- Mật khẩu không bao giờ được lưu plain text.
- Băm bằng **BCrypt** (`BCrypt.Net-Next v4.2.0`) với salt tự động.
- Xác thực: `BCrypt.Verify(plainPassword, hash)`.
- Yêu cầu tối thiểu: ≥ 6 ký tự.

### 2.5 OTP Security

- OTP 6 số, gửi qua email.
- Có thời hạn (lưu `ExpiresAt` trong `EmailOtp`).
- OTP sau khi dùng bị đánh dấu là đã dùng.
- Mỗi `purpose` (Register, ResetPassword) được tách riêng.

### 2.6 CORS Configuration

API chỉ cho phép origin được whitelist (`https://localhost:7240`):
```csharp
policy.WithOrigins("https://localhost:7240")
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();  // Cần cho SignalR
```

### 2.7 Input Validation

- Sử dụng Data Annotations trên DTO/Request classes.
- `ModelState.IsValid` được kiểm tra ở MVC frontend.
- Custom validation trong Service layer (throw `BadRequestException`).

---

## 3. Hạ Tầng & Infrastructure

### 3.1 Database - EF Core + SQL Server

- **ORM:** Entity Framework Core 8.0.2
- **Database:** SQL Server
- **Migrations:** EF Core Code-First migrations
- **Context:** `AppDbContext` với 30+ DbSet
- **Connection:** Cấu hình trong `appsettings.json` → `ConnectionStrings:DB`

**Đặc điểm:**
- Sử dụng `AsNoTracking()` cho các query read-only.
- Transaction (`BeginTransactionAsync`) cho các operation phức tạp.
- `Include()` cho eager loading quan hệ.

### 3.2 Email Service (MailKit)

```
Infrastructure/Email/
├── IEmailService.cs    ← Interface
└── EmailService.cs     ← Implementation (MailKit SMTP)
```

**Cấu hình:**
```json
"EmailSettings": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "noreply@eventix.com",
  "Password": "...",
  "FromEmail": "noreply@eventix.com",
  "FromName": "Eventix"
}
```

**Dùng cho:** Gửi OTP đăng ký, OTP reset mật khẩu.

### 3.3 Background Jobs (Quartz.NET)

**Job:** `EventStatusJob`
- **Schedule:** Mỗi 1 phút (SimpleSchedule, RepeatForever)
- **Chức năng:** Tự động cập nhật `Status` của Event dựa trên thời gian hiện tại và trạng thái TicketType.
- **Tích hợp:** Sau khi cập nhật, broadcast qua SignalR `EventHub`.
- **Hosted Service:** `QuartzHostedService` với `WaitForJobsToComplete = true`.

### 3.4 Real-time (SignalR)

```
Infrastructure/Hubs/
└── EventHub.cs    ← Extends Hub
```

- **Endpoint:** `/hubs/events`
- **Sự kiện:** `EventStatusChanged`
- **Use case:** Cập nhật UI real-time khi trạng thái sự kiện thay đổi.

### 3.5 File Storage

- **Avatar:** Lưu tại `wwwroot/uploads/avatars/`
- **Event images/banners:** URL (có thể là cloud storage hoặc local)
- **Excel templates:** Sinh động trong memory, trả về file download

### 3.6 QR Code & PDF

```
Infrastructure/QR/    ← QR code generation cho vé
Infrastructure/Pdf/   ← PDF ticket generation
```

(Chưa có implementation chi tiết - scaffold cấu trúc)

### 3.7 Payment Gateway Integration

```
Infrastructure/Payment/          ← Gateway abstraction
Modules/Payments/Gateways/       ← Specific gateway implementations
```

Hỗ trợ cấu trúc tích hợp nhiều cổng thanh toán (VNPay, MoMo...).

### 3.8 Health Checks

```
Extensions/HealthCheckExtensions.cs
```

Cấu hình health check endpoint cho SQL Server:
- Package: `AspNetCore.HealthChecks.SqlServer` v9.0.0
- Package: `AspNetCore.HealthChecks.UI.Client` v9.0.0
- Endpoint: `/health` (dạng JSON chi tiết)

---

## 4. Cấu Trúc Response Chuẩn

### 4.1 ApiResponseModel<T>

```csharp
public class ApiResponseModel<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
}
```

### 4.2 PaginationResponse<T>

```csharp
public class PaginationResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

### 4.3 PaginationRequest<T>

```csharp
public class PaginationRequest<T>
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
```

---

## 5. Helpers

### 5.1 SlugHelper

Sinh URL-friendly slug từ tiêu đề sự kiện:
- Chuyển về lowercase, bỏ dấu tiếng Việt.
- Thay khoảng trắng bằng `-`.
- Thêm suffix ngẫu nhiên để đảm bảo unique.

### 5.2 SeatHelper

Hỗ trợ tính toán vị trí ghế khi generate hàng loạt.

### 5.3 ExcelHelper

Sinh file Excel template, hỗ trợ parse file upload (NPOI library).

---

## 6. Tóm Tắt Các System Error Codes

Hệ thống sử dụng các error code string để định danh lỗi (từ `SystemError` class):

| Error Code | Mô tả |
|---|---|
| `EMAIL_ALREADY_EXISTS` | Email đã tồn tại |
| `PHONE_ALREADY_EXISTS` | Số điện thoại đã tồn tại |
| `EMAIL_PASSWORD_INCORRECT` | Email/mật khẩu sai |
| `PASSWORD_REQUIRED` | Mật khẩu bắt buộc |
| `PASSWORD_TOO_SHORT` | Mật khẩu quá ngắn |
| `INVALID_OR_EXPIRED_RESET_TOKEN` | OTP không hợp lệ hoặc hết hạn |
| `UNAUTHORIZED` | Chưa xác thực |
| `ACCESS_DENIED` | Không có quyền |
| `ORGANIZER_NOT_FOUND` | Không tìm thấy Organizer |
| `ORGANIZER_NOT_APPROVED` | Organizer chưa được duyệt |
| `VENUE_NOT_FOUND` | Không tìm thấy Venue |
| `CATEGORY_NOT_FOUND` | Không tìm thấy Category |
| `EVENT_EXIST` | Sự kiện đã tồn tại (conflict thời gian/venue) |

---

## 7. Checklist Tính Năng

### Đã implement

- [x] Đăng ký, đăng nhập, xác thực email OTP
- [x] Refresh Token, Logout
- [x] Quên mật khẩu, đặt lại mật khẩu
- [x] Quản lý hồ sơ người dùng, upload avatar
- [x] CRUD Category (Admin)
- [x] CRUD Venue + Seat Map
- [x] CRUD VenueZone
- [x] Import ghế từ Excel / Generate ghế tự động
- [x] CRUD Event (Organizer)
- [x] Publish sự kiện
- [x] CRUD TicketType (Organizer)
- [x] Duyệt/từ chối Organizer (Admin)
- [x] Event Wizard 7 bước (MVC Web)
- [x] Tự động cập nhật trạng thái sự kiện (Quartz + SignalR)
- [x] Global exception handler
- [x] Swagger API documentation
- [x] Health checks

### Chưa implement (scaffold có sẵn)

- [ ] Đặt vé, giỏ hàng, đặt chỗ tạm (Reservation)
- [ ] Thanh toán (Payment Gateway)
- [ ] Phát hành vé, download PDF/QR
- [ ] Check-in bằng QR code
- [ ] Yêu cầu và xử lý hoàn tiền
- [ ] Mã giảm giá (Coupon)
- [ ] Thông báo (Notification)
- [ ] Báo cáo doanh thu (Reports)
- [ ] Quản trị Admin tập trung
- [ ] Tính năng AI (tagging, gợi ý)
- [ ] Review & Rating sự kiện

---

*Tài liệu hoàn thành. Tổng cộng 4 phần:*
- *Part 1: Tổng quan & Kiến trúc*
- *Part 2: Mô hình dữ liệu*
- *Part 3: Module chức năng & API*
- *Part 4: Luồng nghiệp vụ, Bảo mật & Hạ tầng*
