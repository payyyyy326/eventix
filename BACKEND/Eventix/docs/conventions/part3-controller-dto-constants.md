# EVENTIX CODING CONVENTIONS
## Phần 3: Pattern Controller, DTOs, Constants & Checklist

---

## 1. Pattern Viết Controller (BẮT BUỘC)

### 1.1 Cấu trúc chuẩn của một Controller

```csharp
// ✅ ĐÚNG — Controller chuẩn
[Route("api/[controller]")]
[Authorize]                          // ← Default: yêu cầu đăng nhập
public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: api/order
    [HttpGet]
    [AllowAnonymous]                  // ← Chỉ override nếu endpoint là public
    public async Task<ActionResult<ApiResponseModel<PaginationResponse<OrderResponse>>>> GetOrders(
        [FromQuery] PaginationRequest<OrderResponse> request)
    {
        var result = await _orderService.GetOrdersAsync(request);
        return SuccessResponse(SystemSuccess.ORDERS_RETRIEVED, result);
    }

    // POST: api/order
    [HttpPost]
    public async Task<ActionResult<ApiResponseModel<OrderResponse>>> CreateOrder(
        [FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();      // ← dùng helper method
        var result = await _orderService.CreateOrderAsync(request, userId);
        return SuccessResponse(SystemSuccess.ORDER_CREATED, result);
    }
}
```

### 1.2 Quy tắc cho Controller

```
✅ Controller CHỈ làm:
   - Đọc userId từ JWT claims
   - Gọi Service method
   - Trả về SuccessResponse(SystemSuccess.XXX, data)

❌ Controller KHÔNG được làm:
   - Business logic (if/else để quyết định nghiệp vụ)
   - Query database trực tiếp (DbContext)
   - Xử lý exception (đã có GlobalExceptionHandlerMiddleware)
   - Gọi nhiều hơn 1 service trong 1 action (nếu cần thì tạo service mới)
```

### 1.3 Lấy UserId từ JWT Claims

Luôn lấy `userId` theo pattern sau, **không hardcode**:

```csharp
// ✅ ĐÚNG — parse từ claims
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

// ✅ TỐT HƠN — thêm null-safe check khi cần
var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
if (!Guid.TryParse(userIdValue, out var userId))
    return Unauthorized();

// ❌ SAI — hardcode hoặc lấy từ body
var userId = request.UserId; // ← user tự truyền userId vào request
```

### 1.4 Authorization trên Endpoint

```csharp
// ✅ Endpoint public (không cần đăng nhập)
[AllowAnonymous]
[HttpGet]
public async Task<...> GetEvents([FromQuery] FilterEventRequest request) { }

// ✅ Endpoint chỉ cho Admin
[Authorize(Roles = SystemConstants.RoleConstants.ADMIN)]
[HttpPatch("{id}/approve")]
public async Task<...> ApproveOrganizer(Guid id) { }

// ✅ Endpoint chỉ cho Organizer (dùng Policy)
[Authorize(Policy = SystemConstants.PolicyConstants.OrganizerOnly)]
[HttpPost("create")]
public async Task<...> CreateEvent([FromBody] CreateEventRequest request) { }

// ❌ SAI — hardcode string role
[Authorize(Roles = "Admin")]     // ← dùng constant thay vì string
[Authorize(Roles = "organizer")] // ← sai case (phải là "Organizer")
```

### 1.5 Return Type chuẩn

```csharp
// ✅ ĐÚNG — Response với data
public async Task<ActionResult<ApiResponseModel<OrderResponse>>> CreateOrder(...) { }

// ✅ ĐÚNG — Response danh sách có phân trang
public async Task<ActionResult<ApiResponseModel<PaginationResponse<OrderResponse>>>> GetOrders(...) { }

// ✅ ĐÚNG — Response không có data (delete, action)
public async Task<ActionResult<ApiResponseModel<object>>> CancelOrder(...) { }

// ❌ SAI — Không có wrapper
public async Task<ActionResult<OrderResponse>> CreateOrder(...) { }
public async Task<IActionResult> CreateOrder(...) { }
```

---

## 2. Pattern DTO (BẮT BUỘC)

### 2.1 Request DTO

```csharp
// ✅ ĐÚNG — Request DTO với Data Annotations
public class CreateOrderRequest
{
    [Required]
    public Guid TicketTypeId { get; set; }

    [Required]
    [Range(1, 10, ErrorMessage = "Quantity must be between 1 and 10")]
    public int Quantity { get; set; }

    public Guid? SeatId { get; set; }    // optional field dùng nullable

    public string? CouponCode { get; set; }
}

// ✅ ĐÚNG — Filter/Query DTO kế thừa PaginationRequest
public class FilterOrderRequest : PaginationRequest<OrderResponse>
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
```

**Quy tắc cho Request DTO:**
- Dùng `[Required]` cho field bắt buộc
- Field optional dùng `Type?` (nullable)
- Không bao giờ để `UserId` trong Request DTO — lấy từ JWT
- Filter request kế thừa `PaginationRequest<TResponse>`

### 2.2 Response DTO

```csharp
// ✅ ĐÚNG — Response DTO (chỉ expose những field cần thiết)
public class OrderResponse
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ✅ ĐÚNG — Detail Response có navigation
public class OrderDetailResponse : OrderResponse
{
    public List<OrderItemResponse> Items { get; set; } = new();
    public UserResponse? User { get; set; }
    public PaymentResponse? Payment { get; set; }
}

// ❌ SAI — Expose quá nhiều / expose sensitive fields
public class OrderResponse
{
    public User User { get; set; }         // ← trả về Entity
    public string PasswordHash { get; set; } // ← sensitive data
    public List<Payment> Payments { get; set; } // ← trả về Entity collection
}
```

### 2.3 Đặt file DTO

```
Eventix.Share/DTOs/
└── Order/
    ├── CreateOrderRequest.cs       ← 1 file = 1 class
    ├── FilterOrderRequest.cs
    ├── OrderResponse.cs
    └── OrderDetailResponse.cs

// ❌ SAI — gộp nhiều DTO vào 1 file
// OrderDTOs.cs chứa cả CreateOrderRequest, OrderResponse, OrderDetailResponse
```

---

## 3. Quản Lý Constants (BẮT BUỘC)

### 3.1 SystemSuccess — Thêm mã success mới

Mọi endpoint mới phải có `SystemMessage` tương ứng trong `SystemSuccess.cs` (thuộc `Eventix.Share`):

```csharp
// Eventix.Share/Common/Constants/SystemData/Success.cs
public static class SystemSuccess
{
    // ... các message hiện có ...

    // Order Success Messages (341-360) ← dùng dải số tiếp theo
    public static readonly SystemMessage ORDERS_RETRIEVED     = new("341", "Orders retrieved successfully", true);
    public static readonly SystemMessage ORDER_RETRIEVED      = new("342", "Order retrieved successfully", true);
    public static readonly SystemMessage ORDER_CREATED        = new("343", "Order created successfully", true);
    public static readonly SystemMessage ORDER_CANCELLED      = new("344", "Order cancelled successfully", true);
}
```

**Quy tắc đánh số code:**
- Mỗi domain chiếm **20 số** (341-360, 361-380...)
- Xem file `Success.cs` để biết dải số đã dùng, dùng dải tiếp theo
- Format: `"{số 3 chữ số}"` — ví dụ `"341"`, `"342"`

### 3.2 SystemError — Thêm mã lỗi mới

```csharp
// Eventix.Share/Common/Constants/SystemData/Error.cs
public class SystemError
{
    // ... các error hiện có ...

    // Order Errors
    public static readonly SystemMessage ORDER_NOT_FOUND      = new("470", "Order not found", false);
    public static readonly SystemMessage ORDER_ALREADY_PAID   = new("471", "Order has already been paid", false);
    public static readonly SystemMessage ORDER_EXPIRED        = new("472", "Order has expired", false);
    public static readonly SystemMessage TICKET_SOLD_OUT      = new("473", "Ticket is sold out", false);
}
```

**Quy tắc:**
- Xem dải số đã dùng trong `Error.cs`, dùng số tiếp theo
- `IsSuccess` luôn là `false` cho Error
- Message phải rõ nghĩa, tiếng Anh
- **Không được** dùng lại số code đã tồn tại

### 3.3 SystemConstants — Thêm status constants mới

```csharp
// Eventix.Share/Common/Constants/SystemConstants.cs
public static class SystemConstants
{
    // ... các constants hiện có ...

    public static class OrderStatus
    {
        public const string PENDING   = "Pending";
        public const string PAID      = "Paid";
        public const string CANCELLED = "Cancelled";
        public const string REFUNDED  = "Refunded";
    }

    public static class TicketStatus
    {
        public const string ACTIVE    = "Active";
        public const string USED      = "Used";
        public const string CANCELLED = "Cancelled";
    }
}
```

**Quy tắc:**
- Tên inner class: `{Entity}Status`, `{Entity}Type`, `{Feature}Names`...
- Giá trị string: PascalCase (để nhất quán với các status hiện có)
- **Không hardcode string** ở bất kỳ đâu — luôn dùng constant

---

## 4. Quy Tắc Bổ Sung

### 4.1 DateTime — Luôn dùng UTC

```csharp
// ✅ ĐÚNG
CreatedAt = DateTime.UtcNow
ExpiresAt = DateTime.UtcNow.AddMinutes(15)

// ❌ SAI
CreatedAt = DateTime.Now    // ← local time, không nhất quán giữa các server
```

### 4.2 Kiểm tra quyền sở hữu resource

Khi user chỉ được thao tác với resource của mình:

```csharp
// ✅ ĐÚNG — kiểm tra ownership trong query
var order = await _context.Orders
    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
if (order == null)
    throw new NotFoundException(SystemError.ORDER_NOT_FOUND);
// (Không tiết lộ resource có tồn tại hay không)

// ❌ SAI — 2 query riêng biệt, tiết lộ resource tồn tại
var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
if (order == null) throw new NotFoundException(...);
if (order.UserId != userId) throw new ForbiddenException(...); // ← tiết lộ order tồn tại
```

### 4.3 Không để method NotImplemented trong code đang chạy

```csharp
// ❌ SAI — method chưa làm mà để public
public Task<bool> DeleteOrderAsync(Guid id)
{
    throw new NotImplementedException(); // ← crash 500 khi gọi
}

// ✅ ĐÚNG — comment out controller action nếu chưa implement
// [HttpDelete("{id}")]
// public async Task<...> DeleteOrder(Guid id) { ... }

// HOẶC trả về 501 có message rõ ràng
[HttpDelete("{id}")]
public IActionResult DeleteOrder(Guid id)
{
    return StatusCode(501, new { message = "Not implemented yet." });
}
```

### 4.4 Logging

```csharp
// ✅ ĐÚNG — Log ở các điểm quan trọng trong Infrastructure service
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public async Task SendOtpEmailAsync(string email, string otp)
    {
        try
        {
            // ... gửi email
            _logger.LogInformation("OTP sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP to {Email}", email);
            throw;
        }
    }
}

// ❌ SAI — Log sensitive data
_logger.LogInformation("OTP code: {Otp}", otp);         // ← lộ OTP
_logger.LogInformation("Password: {Password}", password); // ← tuyệt đối không
```

### 4.5 Không để TODO/Debug code khi commit

```csharp
// ❌ KHÔNG commit những dòng sau
Console.WriteLine("debug here");
var x = 1; // TODO: remove
// throw new Exception("test");
```

---

## 5. Checklist Trước Khi Submit Code

Dùng checklist này trước mỗi Pull Request / commit lên branch chính:

### ✅ Kiến trúc & Cấu trúc
- [ ] Module mới có đủ 3 folder: `Controllers/`, `Interfaces/`, `Services/`
- [ ] DTO mới được đặt trong `Eventix.Share/DTOs/{Domain}/`
- [ ] Service mới đã được đăng ký trong `Program.cs`
- [ ] Không có business logic trong Controller

### ✅ Service
- [ ] Tất cả method dùng `async Task<T>` và `await`
- [ ] Không có `SaveChanges()` đồng bộ — phải là `SaveChangesAsync()`
- [ ] Transaction chỉ dùng khi cần thiết (nhiều bảng)
- [ ] Trong `catch` của transaction block có `throw;`
- [ ] Query read-only dùng `AsNoTracking()`
- [ ] Chỉ `Include()` navigation property thực sự dùng
- [ ] Service trả về DTO, không trả về Entity

### ✅ Exception & Validation
- [ ] Dùng đúng loại Exception (`NotFoundException` cho 404, `ForbiddenException` cho 403...)
- [ ] Mọi lỗi dùng `SystemMessage` từ `SystemError`, không hardcode message string
- [ ] Validate tồn tại entity trước khi validate business rule
- [ ] Không có `throw new Exception("...")` chung chung

### ✅ Controller
- [ ] Kế thừa `BaseApiController`
- [ ] Return type là `ActionResult<ApiResponseModel<T>>`
- [ ] Dùng `SuccessResponse(SystemSuccess.XXX, data)`
- [ ] UserId lấy từ `User.FindFirstValue(ClaimTypes.NameIdentifier)`
- [ ] Phân quyền đúng (`[AllowAnonymous]` / `[Authorize(Roles = ...)]`)

### ✅ Constants
- [ ] Success message mới đã thêm vào `SystemSuccess.cs`
- [ ] Error message mới đã thêm vào `SystemError.cs`
- [ ] Status string mới đã thêm vào `SystemConstants.cs`
- [ ] Không có string literal status/error hardcode trong service/controller

### ✅ Chung
- [ ] Tất cả DateTime dùng `DateTime.UtcNow`
- [ ] Không có `Console.WriteLine` hoặc debug code
- [ ] Không có method `NotImplementedException` public đang được expose
- [ ] Code build thành công, không có warning nghiêm trọng

---

## 6. Ví Dụ Module Hoàn Chỉnh (Template)

Đây là template đầy đủ để tạo một module mới, ví dụ `OrderModule`:

```
// Bước 1: Tạo DTO trong Eventix.Share
Eventix.Share/DTOs/Order/
├── CreateOrderRequest.cs
├── FilterOrderRequest.cs
├── OrderResponse.cs
└── OrderDetailResponse.cs

// Bước 2: Thêm SystemMessage vào Eventix.Share
SystemSuccess.cs → thêm ORDERS_RETRIEVED, ORDER_CREATED...
SystemError.cs   → thêm ORDER_NOT_FOUND, ORDER_EXPIRED...
SystemConstants.cs → thêm OrderStatus class

// Bước 3: Tạo module trong Eventix
Eventix/Modules/OrderModule/
├── Interfaces/
│   └── IOrderService.cs      ← định nghĩa interface
├── Services/
│   └── OrderService.cs       ← implement business logic
└── Controllers/
    └── OrderController.cs    ← HTTP endpoints

// Bước 4: Đăng ký service trong Program.cs
builder.Services.AddScoped<IOrderService, OrderService>();
```

---

*Tài liệu conventions hoàn thành. 3 phần:*
- *Part 1: Kiến trúc, cấu trúc module, quy tắc đặt tên*
- *Part 2: Pattern Service, Exception, Response, Entity*
- *Part 3: Pattern Controller, DTOs, Constants, Checklist*
