# EVENTIX CODING CONVENTIONS
## Phần 1: Kiến Trúc, Cấu Trúc Module & Quy Tắc Đặt Tên

---

## 1. Tổng Quan Kiến Trúc

Dự án gồm **3 project** — không được thêm project mới nếu không có sự đồng ý của team lead:

```
Eventix.sln
├── Eventix/          ← REST API backend (Web API)
├── Eventix.Share/    ← Shared library: DTOs, Enums, Constants, Models
└── Eventix.Web/      ← MVC frontend (Organizer Portal)
```

**Nguyên tắc phụ thuộc (Dependency Rule):**
- `Eventix` → phụ thuộc vào `Eventix.Share`
- `Eventix.Web` → phụ thuộc vào `Eventix.Share`
- `Eventix.Share` → **không phụ thuộc** vào project nào khác
- `Eventix.Web` → **không phụ thuộc** vào `Eventix` (chỉ giao tiếp qua HTTP)

---

## 2. Cấu Trúc Thư Mục Module (BẮT BUỘC)

Mỗi tính năng mới phải được tổ chức theo cấu trúc module. **Không được** đặt business logic trong Controllers hay ở ngoài module folder.

### 2.1 Cấu trúc module trong `Eventix/`

```
Modules/
└── {TênModule}Module/          ← Ví dụ: OrderModule, CheckInModule
    ├── Controllers/
    │   └── {TênModule}Controller.cs
    ├── Interfaces/
    │   └── I{TênModule}Service.cs
    └── Services/
        └── {TênModule}Service.cs
```

**Ví dụ đúng:**
```
Modules/
└── OrderModule/
    ├── Controllers/
    │   └── OrderController.cs
    ├── Interfaces/
    │   └── IOrderService.cs
    └── Services/
        └── OrderService.cs
```

**SAI — không được làm:**
```
Modules/
└── OrderModule/
    └── OrderController.cs    ← không có thư mục con
```

### 2.2 DTOs đặt trong `Eventix.Share/`

Tất cả DTO (Request/Response) **phải** đặt trong `Eventix.Share/DTOs/`, không đặt trong module của API project.

```
Eventix.Share/
└── DTOs/
    └── {TênDomain}/           ← Ví dụ: Order, Ticket, CheckIn
        ├── {Tên}Request.cs
        └── {Tên}Response.cs
```

**Lý do:** Eventix.Web cũng cần dùng cùng DTO để gọi API.

### 2.3 Infrastructure

Các service kỹ thuật (không phải business) đặt trong `Eventix/Infrastructure/`:

```
Infrastructure/
├── Email/       ← IEmailService, EmailService
├── Hubs/        ← SignalR Hubs
├── Jobs/        ← Quartz background jobs
├── QR/          ← QR code generation
├── Pdf/         ← PDF generation
├── Payment/     ← Payment gateway adapters
└── Storage/     ← File storage
```

---

## 3. Quy Tắc Đặt Tên (Naming Conventions)

### 3.1 File & Class

| Loại | Quy tắc | Ví dụ |
|---|---|---|
| Controller | `{Tên}Controller` | `OrderController` |
| Service | `{Tên}Service` | `OrderService` |
| Interface | `I{Tên}Service` | `IOrderService` |
| Entity | `{Tên}` (PascalCase) | `Order`, `TicketType` |
| Request DTO | `{Hành động}{Tên}Request` | `CreateOrderRequest`, `FilterEventRequest` |
| Response DTO | `{Tên}Response` | `OrderResponse`, `OrderDetailResponse` |
| Exception | Dùng class có sẵn | `BadRequestException`, `NotFoundException` |
| Job | `{Tên}Job` | `EventStatusJob`, `ReservationExpireJob` |
| Hub | `{Tên}Hub` | `EventHub`, `NotificationHub` |
| Constants | `System{Loại}` (static class) | `SystemConstants`, `SystemError`, `SystemSuccess` |

### 3.2 Method trong Service

| Hành động | Tên method | Ví dụ |
|---|---|---|
| Lấy danh sách | `GetAll{Tên}sAsync` | `GetAllOrdersAsync` |
| Lấy theo Id | `Get{Tên}ByIdAsync` | `GetOrderByIdAsync` |
| Lấy theo điều kiện | `Get{Tên}By{Field}Async` | `GetOrdersByUserAsync` |
| Tạo mới | `Create{Tên}Async` | `CreateOrderAsync` |
| Cập nhật | `Update{Tên}Async` | `UpdateOrderAsync` |
| Xoá | `Delete{Tên}Async` | `DeleteOrderAsync` |
| Hành động đặc biệt | `{Động từ}{Tên}Async` | `PublishEventAsync`, `ApproveOrganizerAsync` |

### 3.3 API Route Convention

```
GET    /api/{module}           → Lấy danh sách
GET    /api/{module}/{id}      → Lấy chi tiết
POST   /api/{module}           → Tạo mới
POST   /api/{module}/create    → (dùng khi POST / bị conflict với list)
PUT    /api/{module}/{id}      → Cập nhật toàn bộ
PATCH  /api/{module}/{id}/{action} → Hành động cụ thể
DELETE /api/{module}/{id}      → Xoá
```

**Ví dụ:**
```
GET    /api/orders
GET    /api/orders/{id}
POST   /api/orders
PATCH  /api/orders/{id}/cancel
GET    /api/orders/{id}/tickets
```

**Quy tắc đặt tên route:**
- Dùng **lowercase, kebab-case** cho URL: `/api/ticket-types`, `/api/venue-zones`
- Không dùng động từ trong URL: ~~`/api/getOrders`~~, ~~`/api/createOrder`~~
- Ngoại lệ cho action đặc biệt dùng PATCH: `/api/events/{id}/publish`

### 3.4 Biến & Field trong Code

| Loại | Convention | Ví dụ |
|---|---|---|
| Local variable | camelCase | `orderItem`, `totalAmount` |
| Parameter | camelCase | `userId`, `eventId` |
| Private field | `_camelCase` | `_context`, `_emailService` |
| Constant (static) | `UPPER_SNAKE_CASE` | (chỉ dùng trong SystemConstants) |
| Property | PascalCase | `TotalAmount`, `CreatedAt` |

---

## 4. Đăng Ký Service (Program.cs)

Mọi service mới phải được đăng ký trong `Program.cs` theo pattern **Scoped**:

```csharp
// ✅ ĐÚNG — Dùng Scoped cho business service
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();

// ✅ ĐÚNG — Infrastructure service cũng Scoped
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// ❌ SAI — Không dùng Singleton cho service có DbContext
builder.Services.AddSingleton<IOrderService, OrderService>();

// ❌ SAI — Không dùng Transient (tốn kém)
builder.Services.AddTransient<IOrderService, OrderService>();
```

**Thứ tự đăng ký trong Program.cs:**
1. Settings (Configure)
2. Authentication / Authorization
3. Database (DbContext)
4. Background Jobs (Quartz)
5. SignalR
6. Business Services (theo thứ tự alphabetical)
7. Infrastructure Services
8. Swagger / Health Checks
9. CORS

---

## 5. Quy Tắc Async/Await

**BẮT BUỘC:** Mọi method có I/O (database, email, file) phải là `async Task<T>`.

```csharp
// ✅ ĐÚNG
public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, Guid userId)
{
    await _context.SaveChangesAsync();
}

// ❌ SAI — Blocking trong async context
public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, Guid userId)
{
    _context.SaveChanges(); // ← KHÔNG BAO GIỜ làm điều này
}

// ❌ SAI — Thiếu await
public async Task SendEmailAsync(string email)
{
    _emailService.SendAsync(email); // ← quên await, fire-and-forget không kiểm soát
}
```

---

*→ Xem tiếp: Part 2 - Pattern Service, Exception, Response, Entity*
