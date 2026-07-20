# TÀI LIỆU ĐẶC TẢ HỆ THỐNG EVENTIX
## Phần 1: Tổng Quan Dự Án & Kiến Trúc Hệ Thống

---

## 1. Tổng Quan Dự Án

### 1.1 Giới thiệu

**Eventix** là một nền tảng quản lý và bán vé sự kiện trực tuyến, được xây dựng trên nền tảng .NET 8. Hệ thống cho phép người dùng khám phá, đặt vé các sự kiện; cho phép người tổ chức (Organizer) tạo và quản lý sự kiện; và cung cấp công cụ quản trị cho Admin.

### 1.2 Mục tiêu hệ thống

- Cung cấp nền tảng bán vé sự kiện đa loại hình: có chỗ ngồi (seated) và không có chỗ ngồi (general admission).
- Cho phép Organizer tạo sự kiện theo quy trình nhiều bước (Event Wizard) bao gồm: thông tin sự kiện, địa điểm, khu vực, loại vé, sơ đồ chỗ ngồi.
- Hỗ trợ quản lý đặt chỗ tạm thời (Reservation), thanh toán, hoàn tiền, check-in bằng QR code.
- Tự động cập nhật trạng thái sự kiện theo thời gian thực (Quartz + SignalR).
- Cung cấp hệ thống thông báo, coupon giảm giá, phân quyền đa vai trò.

### 1.3 Phạm vi hệ thống

| Đối tượng | Chức năng chính |
|---|---|
| **Khách (Anonymous)** | Xem danh sách sự kiện, xem chi tiết, lọc/tìm kiếm |
| **Customer (User)** | Đăng ký, đăng nhập, đặt vé, thanh toán, xem vé, check-in, yêu cầu hoàn tiền |
| **Organizer** | Tạo/quản lý sự kiện, quản lý địa điểm, loại vé, xem báo cáo doanh thu |
| **Admin** | Duyệt Organizer, quản lý người dùng, quản lý danh mục, toàn quyền hệ thống |

---

## 2. Kiến Trúc Hệ Thống

### 2.1 Tổng quan kiến trúc

Hệ thống Eventix được tổ chức thành **3 project** trong cùng một Solution (.NET 8):

```
Eventix.sln
├── Eventix/              ← Web API Backend (REST API)
├── Eventix.Share/        ← Shared Library (DTOs, Enums, Constants)
└── Eventix.Web/          ← MVC Web Frontend (Organizer Portal)
```

### 2.2 Chi tiết từng project

#### 2.2.1 Eventix (API Project)

Project chính, đóng vai trò **REST API backend**. Tổ chức theo kiến trúc Module-based:

```
Eventix/
├── Controllers/          ← BaseApiController
├── Entities/             ← EF Core Domain Entities (18+ entity)
├── Data/                 ← AppDbContext (EF Core)
├── Modules/              ← Các module nghiệp vụ (mỗi module = Controller + Service + Interface)
│   ├── AuthModule/
│   ├── UserModule/
│   ├── EventModule/
│   ├── VenueModule/
│   ├── VenueZoneModule/
│   ├── SeatModule/
│   ├── TicketTypeModule/
│   ├── OrganizerModule/
│   ├── CategoryModule/
│   ├── Orders/
│   ├── Payments/
│   ├── Tickets/
│   ├── Reservations/
│   ├── CheckIn/
│   ├── Refunds/
│   ├── Coupons/
│   ├── Notifications/
│   ├── Reports/
│   ├── Admin/
│   └── AI/
├── Infrastructure/
│   ├── Email/            ← MailKit email service
│   ├── Hubs/             ← SignalR EventHub
│   ├── Jobs/             ← Quartz EventStatusJob
│   ├── QR/               ← QR code generation
│   ├── Pdf/              ← PDF generation
│   ├── Payment/          ← Payment gateway integration
│   └── Storage/          ← File storage
├── Common/
│   ├── Exceptions/       ← Custom ApiException
│   └── Settings/         ← AppSettings (JWT, Email, Api)
├── Extensions/           ← Health checks, IQueryable extensions
├── Helpers/              ← SlugHelper, SeatHelper, ExcelHelper
└── Middleware/           ← GlobalExceptionHandlerMiddleware
```

#### 2.2.2 Eventix.Share (Shared Library)

Thư viện dùng chung giữa API và Web MVC:

```
Eventix.Share/
├── DTOs/                 ← Data Transfer Objects (Request/Response)
│   ├── Auth/
│   ├── Event/
│   ├── User/
│   ├── Venue/
│   ├── VenueZone/
│   ├── Seat/
│   ├── SeatMap/
│   ├── TicketType/
│   ├── Organizer/
│   ├── Category/
│   └── Role/
└── Common/
    ├── Constants/        ← SystemConstants (roles, statuses, cookie names...)
    ├── Models/           ← ApiResponseModel, PaginationRequest/Response
    └── Enums/            ← SystemEnum
```

#### 2.2.3 Eventix.Web (MVC Frontend)

Web frontend dành cho Organizer, sử dụng ASP.NET Core MVC:

```
Eventix.Web/
├── Controllers/          ← MVC Controllers (gọi API qua HttpClient)
│   ├── AuthController
│   ├── HomeController
│   ├── EventController
│   ├── EventWizardController   ← 7-step wizard tạo sự kiện
│   ├── OrganizerController
│   ├── VenueController
│   ├── TicketTypeController
│   └── UserController
├── Views/                ← Razor Views (Cshtml)
├── Models/               ← ViewModel (EventWizard steps)
└── wwwroot/              ← Static files (CSS, JS, Bootstrap)
```

### 2.3 Pattern kiến trúc

| Layer | Pattern |
|---|---|
| API Layer | Controller → Service (Interface) → Repository (EF Core DbContext) |
| Response | Unified `ApiResponseModel<T>` wrapper cho mọi response |
| Exception | `GlobalExceptionHandlerMiddleware` + `ApiException` hierarchy |
| Auth | JWT Bearer Token + Refresh Token |
| Realtime | SignalR Hub (`/hubs/events`) |
| Background Job | Quartz.NET Hosted Service |

### 2.4 Luồng giao tiếp

```
[Browser/Mobile]
      │
      ▼
[Eventix.Web MVC] ─── HttpClient ──► [Eventix REST API]
                                            │
                                     [AppDbContext]
                                            │
                                      [SQL Server]
```

- Eventix.Web giao tiếp với API qua `IHttpClientFactory`, đọc JWT từ cookie.
- API trả về `ApiResponseModel<T>` chuẩn hoá.
- Realtime updates qua SignalR được broadcast đến tất cả clients.

---

## 3. Công Nghệ Sử Dụng

### 3.1 Backend (Eventix API)

| Công nghệ | Phiên bản | Mục đích |
|---|---|---|
| **.NET 8** | 8.0 | Framework chính |
| **ASP.NET Core Web API** | 8.0 | REST API |
| **Entity Framework Core** | 8.0.2 | ORM, Database access |
| **EF Core SqlServer** | 8.0.2 | SQL Server provider |
| **SQL Server** | — | Cơ sở dữ liệu chính |
| **JWT Bearer** | 8.0.2 | Authentication |
| **System.IdentityModel.Tokens.Jwt** | 7.3.1 | JWT generation/validation |
| **BCrypt.Net-Next** | 4.2.0 | Password hashing |
| **MailKit** | 4.17.0 | Gửi email (OTP, xác thực) |
| **Quartz.NET** | 3.18.2 | Background job scheduling |
| **SignalR** | Built-in | Realtime communication |
| **NPOI** | 2.8.0 | Excel import/export (seat template) |
| **Swashbuckle (Swagger)** | 6.6.2 | API documentation |
| **AspNetCore.HealthChecks.SqlServer** | 9.0.0 | Health monitoring |

### 3.2 Frontend (Eventix.Web MVC)

| Công nghệ | Mục đích |
|---|---|
| **ASP.NET Core MVC** | Web framework |
| **Razor Views (.cshtml)** | Server-side rendering |
| **Bootstrap 5** | UI framework |
| **jQuery** | DOM manipulation, AJAX |
| **jQuery Validation** | Client-side form validation |
| **Custom CSS** | event-wizard.css, organizer.css, auth.css... |

### 3.3 Cấu hình ứng dụng

**appsettings.json** (API):
```json
{
  "ConnectionStrings": {
    "DB": "<SQL Server connection string>"
  },
  "JwtSettings": {
    "Key": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpiresInMinutes": 60
  },
  "EmailSettings": {
    "Host": "...",
    "Port": 587,
    "Username": "...",
    "Password": "..."
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:..."
  }
}
```

### 3.4 CORS Policy

API cấu hình CORS cho phép origin `https://localhost:7240` (Eventix.Web) với `AllowCredentials()` để hỗ trợ SignalR.

---

## 4. Phân Quyền & Vai Trò

### 4.1 Các vai trò trong hệ thống

| Vai trò | Mô tả |
|---|---|
| `Admin` | Quản trị viên hệ thống, toàn quyền |
| `Organizer` | Người tổ chức sự kiện (cần được Admin duyệt) |
| `Customer` / `User` | Người dùng đặt vé thông thường |

### 4.2 Policy Authorization

| Policy | Điều kiện |
|---|---|
| `AdminOnly` | Role = Admin |
| `OrganizerOnly` | Role = Organizer |
| `CustomerOnly` | Role = Customer |
| `AdminOrOrganizer` | Role = Admin hoặc Organizer |

### 4.3 Trạng thái tài khoản

| Trạng thái | Mô tả |
|---|---|
| `ACTIVE` | Tài khoản hoạt động bình thường |
| `INACTIVE` | Chưa kích hoạt |
| `BANNED` | Bị cấm |
| `DELETED` | Đã xóa (soft delete) |

---

*→ Xem tiếp: Part 2 - Mô hình dữ liệu*
