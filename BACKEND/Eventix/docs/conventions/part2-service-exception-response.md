# EVENTIX CODING CONVENTIONS
## Phần 2: Pattern Service, Exception, Response & Entity

---

## 1. Pattern Viết Service (BẮT BUỘC)

### 1.1 Cấu trúc chuẩn của một Service class

```csharp
// ✅ ĐÚNG — cấu trúc chuẩn
public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    // Chỉ inject qua constructor. KHÔNG dùng service locator.
    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, Guid userId)
    {
        // 1. Validate inputs & business rules → throw exception nếu sai
        // 2. Thực hiện logic
        // 3. Persist vào DB
        // 4. Map sang Response DTO và trả về
    }
}
```

### 1.2 Thứ tự logic trong mỗi method Service

Mỗi method trong service phải tuân thủ thứ tự sau:

```
1. Validate sự tồn tại của các entity liên quan (FK)
2. Validate business rules (quyền, trạng thái, giới hạn)
3. Thực hiện thay đổi / tạo mới entity
4. Persist (SaveChangesAsync)
5. Map và return Response DTO
```

**Ví dụ:**
```csharp
public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, Guid userId)
{
    // BƯỚC 1: Validate sự tồn tại
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) throw new NotFoundException(SystemError.USER_NOT_FOUND);

    var ticketType = await _context.TicketTypes.FirstOrDefaultAsync(t => t.Id == request.TicketTypeId);
    if (ticketType == null) throw new NotFoundException(SystemError.TICKET_TYPE_NOT_FOUND);

    // BƯỚC 2: Validate business rules
    if (ticketType.Quantity <= ticketType.SoldQuantity + ticketType.ReservedQuantity)
        throw new BadRequestException(SystemError.TICKET_SOLD_OUT);

    // BƯỚC 3: Tạo entity
    var order = new Order
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        OrderCode = GenerateOrderCode(),
        Status = OrderStatus.PENDING,
        CreatedAt = DateTime.UtcNow
    };
    _context.Orders.Add(order);

    // BƯỚC 4: Persist
    await _context.SaveChangesAsync();

    // BƯỚC 5: Return DTO
    return new OrderResponse
    {
        Id = order.Id,
        OrderCode = order.OrderCode,
        Status = order.Status
    };
}
```

### 1.3 Khi nào dùng Transaction

Chỉ dùng `BeginTransactionAsync` khi có **nhiều hơn 1 bảng** bị thay đổi trong cùng một operation.

```csharp
// ✅ ĐÚNG — cần transaction vì update nhiều bảng
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    _context.Orders.Add(order);
    ticketType.ReservedQuantity += request.Quantity;  // bảng thứ 2
    reservation.Status = "Confirmed";                  // bảng thứ 3

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;  // LUÔN re-throw để GlobalExceptionHandler xử lý
}

// ❌ SAI — transaction không cần thiết khi chỉ insert 1 entity
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    _context.Categories.Add(category);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();  // ← thừa
}
```

### 1.4 Quy tắc Query Database

```csharp
// ✅ ĐÚNG — Dùng AsNoTracking() cho query chỉ đọc (GET)
var events = await _context.Events
    .AsNoTracking()
    .Include(e => e.Category)
    .ToListAsync();

// ✅ ĐÚNG — Không dùng AsNoTracking() khi cần update entity
var order = await _context.Orders
    .FirstOrDefaultAsync(o => o.Id == orderId);
order.Status = "Paid"; // EF Core tracking hoạt động bình thường
await _context.SaveChangesAsync();

// ✅ ĐÚNG — Chỉ Include những navigation property thực sự dùng
var ticketType = await _context.TicketTypes
    .Include(tt => tt.Event)   // ← dùng event.StartTime
    .Include(tt => tt.VenueZone) // ← dùng zone.Capacity
    .FirstOrDefaultAsync(tt => tt.Id == id);

// ❌ SAI — Include quá nhiều gây N+1 / over-fetch
var ticketType = await _context.TicketTypes
    .Include(tt => tt.Event)
        .ThenInclude(e => e.TicketTypes) // ← vòng tròn không cần thiết
        .ThenInclude(e => e.Venue)
        .ThenInclude(e => e.Organizer)
    .FirstOrDefaultAsync(tt => tt.Id == id);

// ✅ ĐÚNG — Dùng AnyAsync/CountAsync thay vì load toàn bộ entity để check
var exists = await _context.Events.AnyAsync(e => e.Slug == slug);

// ❌ SAI
var event = await _context.Events.FirstOrDefaultAsync(e => e.Slug == slug);
if (event != null) // ← load toàn bộ entity chỉ để check tồn tại
```

### 1.5 Không được làm trong Service

```csharp
// ❌ KHÔNG inject HttpContext vào Service
public class OrderService
{
    private readonly IHttpContextAccessor _httpContextAccessor; // ← SAI
}

// ❌ KHÔNG throw exception chung chung
throw new Exception("Something went wrong"); // ← dùng ApiException hierarchy

// ❌ KHÔNG trả về Entity trực tiếp từ Service — phải map sang DTO
public async Task<Order> GetOrderAsync(Guid id) // ← SAI, trả về Entity
public async Task<OrderResponse> GetOrderAsync(Guid id) // ← ĐÚNG

// ❌ KHÔNG gọi SaveChanges() đồng bộ trong async method
_context.SaveChanges(); // ← phải là await _context.SaveChangesAsync()
```

---

## 2. Pattern Exception (BẮT BUỘC)

### 2.1 Các loại Exception có sẵn

Dự án đã có sẵn hierarchy sau — **chỉ dùng những class này**, không tự tạo exception mới:

| Class | HTTP Status | Khi nào dùng |
|---|---|---|
| `BadRequestException` | 400 | Input không hợp lệ, vi phạm business rule |
| `UnauthorizedException` | 401 | Chưa đăng nhập / token không hợp lệ |
| `ForbiddenException` | 403 | Đã đăng nhập nhưng không có quyền |
| `NotFoundException` | 404 | Entity không tồn tại |
| `ConflictException` | 409 | Dữ liệu bị trùng lặp (duplicate) |
| `InternalServerErrorException` | 500 | Lỗi hệ thống không lường trước |

### 2.2 Cách throw đúng

```csharp
// ✅ ĐÚNG — Luôn dùng SystemMessage từ SystemError
if (order == null)
    throw new NotFoundException(SystemError.ORDER_NOT_FOUND);

if (order.UserId != currentUserId)
    throw new ForbiddenException(SystemError.FORBIDDEN);

if (emailExists)
    throw new ConflictException(SystemError.EMAIL_ALREADY_EXISTS);

// ✅ ĐÚNG — Có thể dùng string message khi chưa có SystemMessage
throw new BadRequestException("Sale end time must be before event start time.");

// ❌ SAI — Dùng sai loại exception
if (order == null)
    throw new BadRequestException(SystemError.ORDER_NOT_FOUND); // ← nên là NotFoundException

if (order.UserId != currentUserId)
    throw new BadRequestException(SystemError.FORBIDDEN); // ← nên là ForbiddenException

// ❌ SAI — Tự catch và swallow exception
try { ... }
catch (Exception ex)
{
    // không làm gì, hoặc chỉ log mà không re-throw
    _logger.LogError(ex, "...");
    // ← thiếu throw; → lỗi bị nuốt
}

// ✅ ĐÚNG — Trong transaction block: catch, rollback, rồi re-throw
try { ... }
catch
{
    await transaction.RollbackAsync();
    throw; // ← PHẢI có dòng này
}
```

### 2.3 Quy tắc chọn Exception type

```
Câu hỏi: Entity có tồn tại không?
  → Không → NotFoundException

Câu hỏi: User có đăng nhập không?
  → Không → UnauthorizedException (nhưng thường do middleware xử lý tự động)

Câu hỏi: User đã đăng nhập, nhưng có quyền với resource này không?
  → Không → ForbiddenException

Câu hỏi: Dữ liệu input sai / vi phạm business rule?
  → BadRequestException

Câu hỏi: Dữ liệu bị trùng (email, slug, code)?
  → ConflictException
```

---

## 3. Pattern Response (BẮT BUỘC)

### 3.1 Response Model chuẩn

Mọi API response phải wrap trong `ApiResponseModel<T>`. **Không bao giờ** trả về raw object hay raw list.

```csharp
// ✅ ĐÚNG — return từ Controller
return SuccessResponse(SystemSuccess.ORDER_CREATED, orderResponse);

// ❌ SAI — return trực tiếp không qua wrapper
return Ok(orderResponse);
return Ok(new { id = order.Id, status = order.Status });
```

### 3.2 Cấu trúc Response DTO

Mỗi entity thường cần tối thiểu **2 DTO**:

```
{Tên}Response         → Dùng cho danh sách (ít field hơn)
{Tên}DetailResponse   → Dùng cho chi tiết (đầy đủ field, có navigation)
```

**Quy tắc map từ Entity sang DTO:**
```csharp
// ✅ ĐÚNG — Map thủ công, rõ ràng từng field
return new OrderResponse
{
    Id = order.Id,
    OrderCode = order.OrderCode,
    Status = order.Status,
    TotalAmount = order.TotalAmount,
    CreatedAt = order.CreatedAt
};

// ❌ SAI — Trả về Entity trực tiếp (expose DB schema ra ngoài)
return order; // ← tuyệt đối không làm

// ❌ SAI — Dùng AutoMapper khi chưa được thống nhất trong team
return _mapper.Map<OrderResponse>(order); // ← chưa cấu hình trong project
```

### 3.3 Pagination Response

Tất cả endpoint trả về danh sách phải dùng `PaginationResponse<T>` thông qua extension `GetPaged()`:

```csharp
// ✅ ĐÚNG
public async Task<PaginationResponse<OrderResponse>> GetOrdersAsync(
    PaginationRequest<OrderResponse> request, Guid userId)
{
    var query = _context.Orders
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new OrderResponse
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt
        });

    return await query.GetPaged(request.CurrentPage, request.PageSize);
}

// ❌ SAI — Return List<T> thay vì PaginationResponse
public async Task<List<OrderResponse>> GetOrdersAsync() // ← không có phân trang
```

---

## 4. Pattern Entity (BẮT BUỘC)

### 4.1 Cấu trúc chuẩn của Entity

```csharp
// ✅ ĐÚNG — Cấu trúc entity chuẩn
public partial class Order
{
    // 1. Primary Key luôn là Guid
    public Guid Id { get; set; }

    // 2. Foreign Keys (Guid, nullable nếu optional)
    public Guid UserId { get; set; }
    public Guid? CouponId { get; set; }

    // 3. Business fields
    public string OrderCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }

    // 4. Audit fields — LUÔN có CreatedAt, nên có UpdatedAt
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // 5. Navigation properties — cuối cùng
    public virtual User User { get; set; } = null!;
    public virtual Coupon? Coupon { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

### 4.2 Quy tắc về Entity

```
- Primary Key: LUÔN là Guid, tên là "Id"
- String không nullable: dùng = null! để khai báo (EF Core sẽ require)
- String nullable: dùng string?
- Foreign Key nullable: Guid? (quan hệ optional)
- Collections: khởi tạo ngay = new List<T>() để tránh NullReference
- Audit: CreatedAt (DateTime, not null), UpdatedAt (DateTime?, nullable)
- KHÔNG có business logic trong Entity (không có method tính toán)
- KHÔNG dùng [DataAnnotations] trong Entity — cấu hình qua AppDbContext/Fluent API
```

### 4.3 Sử dụng Status fields

Status của entity phải dùng string constant từ `SystemConstants`, không hardcode string:

```csharp
// ✅ ĐÚNG
order.Status = SystemConstants.OrderStatus.PENDING;
event.Status = SystemConstants.EventStatus.Published;

// ❌ SAI — hardcode string
order.Status = "pending";    // ← typo không được phát hiện lúc compile
order.Status = "Pending";    // ← dễ không nhất quán
```

---

*→ Xem tiếp: Part 3 - Pattern Controller, DTOs, Constants & Checklist*
