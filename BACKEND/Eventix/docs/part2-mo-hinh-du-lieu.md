# TÀI LIỆU ĐẶC TẢ HỆ THỐNG EVENTIX
## Phần 2: Mô Hình Dữ Liệu

---

## 1. Danh Sách Entity

Hệ thống có **24 entity** chính, được quản lý bởi EF Core và lưu trữ trong SQL Server:

| STT | Entity | Mô tả |
|---|---|---|
| 1 | `User` | Người dùng hệ thống |
| 2 | `Role` | Vai trò (Admin/Organizer/Customer) |
| 3 | `UserRefreshToken` | Refresh token của người dùng |
| 4 | `EmailOtp` | OTP gửi qua email |
| 5 | `OrganizerProfile` | Hồ sơ người tổ chức sự kiện |
| 6 | `Category` | Danh mục sự kiện |
| 7 | `Event` | Sự kiện |
| 8 | `EventImage` | Ảnh của sự kiện |
| 9 | `EventAitag` | Nhãn AI gán cho sự kiện |
| 10 | `Venue` | Địa điểm tổ chức |
| 11 | `VenueZone` | Khu vực trong địa điểm |
| 12 | `VenueSectionLayout` | Bố cục khu vực (tọa độ SVG) |
| 13 | `Seat` | Ghế ngồi cụ thể trong Venue |
| 14 | `EventSeatStatus` | Trạng thái ghế theo từng sự kiện |
| 15 | `TicketType` | Loại vé của sự kiện |
| 16 | `Cart` | Giỏ hàng của người dùng |
| 17 | `CartItem` | Item trong giỏ hàng |
| 18 | `Reservation` | Đặt chỗ tạm thời (có thời hạn) |
| 19 | `Order` | Đơn hàng |
| 20 | `OrderItem` | Chi tiết item trong đơn hàng |
| 21 | `Ticket` | Vé đã phát hành (sau khi thanh toán) |
| 22 | `Payment` | Giao dịch thanh toán |
| 23 | `Coupon` | Mã giảm giá |
| 24 | `CouponUsage` | Lịch sử sử dụng coupon |
| 25 | `RefundRequest` | Yêu cầu hoàn tiền |
| 26 | `CheckInLog` | Nhật ký check-in |
| 27 | `Notification` | Thông báo hệ thống |
| 28 | `AuditLog` | Nhật ký hành động hệ thống |
| 29 | `UserEventInteraction` | Tương tác người dùng với sự kiện |
| 30 | `Review` | Đánh giá sự kiện |
| 31 | `RefundPolicy` | Chính sách hoàn tiền |
| 32 | `PaymentWebhookLog` | Log webhook thanh toán |

---

## 2. Mô Tả Chi Tiết Từng Entity

### 2.1 User

Đại diện cho người dùng hệ thống (Customer, Organizer, Admin).

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `Email` | string | Email (unique, lowercase) |
| `PasswordHash` | string | Mật khẩu băm BCrypt |
| `FullName` | string | Họ tên đầy đủ |
| `PhoneNumber` | string? | Số điện thoại (unique) |
| `AvatarUrl` | string? | URL ảnh đại diện |
| `Status` | string | ACTIVE / INACTIVE / BANNED / DELETED |
| `EmailVerified` | bool | Đã xác thực email chưa |
| `EmailVerifiedAt` | DateTime? | Thời điểm xác thực email |
| `CreatedAt` | DateTime | Ngày tạo |
| `UpdatedAt` | DateTime? | Ngày cập nhật |

**Quan hệ:**
- Many-to-Many với `Role`
- One-to-One với `Cart`, `OrganizerProfile`
- One-to-Many với `Order`, `Ticket`, `Payment`, `Reservation`, `Notification`, `RefundRequest`, `Review`, `Venue`

### 2.2 Role

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `Name` | string | Tên vai trò (Admin/Organizer/Customer) |

### 2.3 OrganizerProfile

Hồ sơ của người tổ chức sự kiện. Cần được Admin phê duyệt trước khi tạo sự kiện.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `UserId` | Guid | FK → User |
| `OrganizationName` | string | Tên tổ chức |
| `Description` | string? | Mô tả |
| `ContactEmail` | string? | Email liên hệ |
| `ContactPhone` | string? | Điện thoại liên hệ |
| `Status` | string | Pending / Approved / Rejected / Suspended |
| `ApprovedBy` | Guid? | FK → User (Admin duyệt) |
| `ApprovedAt` | DateTime? | Thời điểm duyệt |
| `CreatedAt` | DateTime | Ngày tạo |

### 2.4 Category

Danh mục phân loại sự kiện.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `Name` | string | Tên danh mục |
| `Slug` | string | URL-friendly slug |
| `Description` | string? | Mô tả |
| `IsActive` | bool | Đang hoạt động |
| `CreatedAt` | DateTime | Ngày tạo |
| `CreatedBy` | Guid? | FK → User |

### 2.5 Event

Entity trung tâm của hệ thống.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `OrganizerId` | Guid | FK → OrganizerProfile |
| `CategoryId` | Guid | FK → Category |
| `VenueId` | Guid | FK → Venue |
| `Title` | string | Tiêu đề sự kiện |
| `Slug` | string | URL-friendly slug (tự động sinh) |
| `Description` | string? | Mô tả chi tiết |
| `Summary` | string? | Tóm tắt ngắn |
| `ImageUrl` | string? | Ảnh thumbnail |
| `BannerUrl` | string? | Ảnh banner |
| `StartTime` | DateTime | Thời gian bắt đầu |
| `EndTime` | DateTime | Thời gian kết thúc |
| `Status` | string | Draft / Published / OnSale / SoldOut / Ongoing / Completed / Cancelled |
| `ViewCount` | int | Số lượt xem |
| `IsFeatured` | bool | Sự kiện nổi bật |
| `PublishedAt` | DateTime? | Thời điểm công bố (tự động publish) |
| `CreatedAt` | DateTime | Ngày tạo |
| `CreatedBy` | Guid | FK → User |

**Trạng thái sự kiện (tự động bởi EventStatusJob):**

```
Draft ──(PublishedAt đến)──► Published ──(SaleStart đến)──► OnSale
                                                                │
                              ┌─────────────────────────────────┤
                              ▼                                 ▼
                           SoldOut                          Ongoing ──► Completed
```

### 2.6 Venue

Địa điểm tổ chức sự kiện.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `Name` | string | Tên địa điểm |
| `Address` | string? | Địa chỉ |
| `City` | string? | Thành phố |
| `Capacity` | int | Sức chứa tối đa |
| `CreatedBy` | Guid? | FK → User |
| `CreatedAt` | DateTime | Ngày tạo |

**Quan hệ:** One-to-Many với `VenueZone`, `Seat`, `VenueSectionLayout`, `Event`

### 2.7 VenueZone

Khu vực phân chia trong địa điểm (ví dụ: Khu A, Khu VIP, Sân khấu...).

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `VenueId` | Guid | FK → Venue |
| `Name` | string | Tên khu vực |
| `HasSeats` | bool | Có ghế được đánh số không |
| `Capacity` | int | Sức chứa khu vực |
| `Color` | string | Màu hiển thị trên sơ đồ |
| `SortOrder` | int | Thứ tự hiển thị |

### 2.8 Seat

Ghế ngồi cụ thể, thuộc về một Venue và VenueZone.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `VenueId` | Guid | FK → Venue |
| `VenueZoneId` | Guid? | FK → VenueZone |
| `Section` | string? | Khu vực (Section) |
| `Row` | string? | Hàng |
| `Number` | string | Số ghế |
| `Xposition` | decimal? | Tọa độ X trên sơ đồ SVG |
| `Yposition` | decimal? | Tọa độ Y trên sơ đồ SVG |
| `Status` | string | Available / Sold |

### 2.9 EventSeatStatus

Trạng thái của ghế theo từng sự kiện cụ thể (cùng một ghế có thể có trạng thái khác nhau ở các sự kiện khác nhau).

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `EventId` | Guid | FK → Event |
| `SeatId` | Guid | FK → Seat |
| `TicketTypeId` | Guid | FK → TicketType |
| `Status` | string | Available / Reserved / Sold |

### 2.10 TicketType

Loại vé của một sự kiện (VIP, Thường, Early Bird...).

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `EventId` | Guid | FK → Event |
| `VenueZoneId` | Guid? | FK → VenueZone (khu vực áp dụng) |
| `Name` | string | Tên loại vé |
| `Description` | string? | Mô tả |
| `Price` | decimal | Giá vé |
| `Quantity` | int | Tổng số lượng |
| `SoldQuantity` | int | Số đã bán |
| `ReservedQuantity` | int | Số đang được giữ chỗ |
| `Section` | string? | Khu vực áp dụng |
| `SaleStartTime` | DateTime | Thời điểm bắt đầu bán |
| `SaleEndTime` | DateTime | Thời điểm kết thúc bán |
| `IsSeatRequired` | bool | Có yêu cầu chọn ghế không |
| `Status` | string | Active / Inactive |

### 2.11 Cart & CartItem

Giỏ hàng tạm thời trước khi đặt chỗ.

**Cart:**
| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `UserId` | Guid | FK → User (One-to-One) |

**CartItem:**
| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `CartId` | Guid | FK → Cart |
| `EventId` | Guid | FK → Event |
| `TicketTypeId` | Guid | FK → TicketType |
| `SeatId` | Guid? | FK → Seat (nếu có chỗ ngồi) |
| `Quantity` | int | Số lượng |
| `UnitPrice` | decimal | Giá tại thời điểm thêm vào giỏ |

### 2.12 Reservation

Đặt chỗ tạm thời, có thời hạn (tránh oversell khi nhiều người cùng mua).

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `UserId` | Guid | FK → User |
| `EventId` | Guid | FK → Event |
| `TicketTypeId` | Guid | FK → TicketType |
| `SeatId` | Guid? | FK → Seat |
| `OrderId` | Guid? | FK → Order (khi đã tạo đơn) |
| `Quantity` | int | Số lượng giữ chỗ |
| `Status` | string | Pending / Confirmed / Expired / Cancelled |
| `ExpiresAt` | DateTime | Thời điểm hết hạn |

### 2.13 Order

Đơn hàng của người dùng.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `UserId` | Guid | FK → User |
| `OrderCode` | string | Mã đơn hàng (unique, hiển thị) |
| `Status` | string | Pending / Paid / Cancelled / Refunded |
| `SubTotal` | decimal | Tổng trước giảm giá |
| `ServiceFee` | decimal | Phí dịch vụ |
| `DiscountAmount` | decimal | Số tiền giảm |
| `TotalAmount` | decimal | Tổng thanh toán |
| `CouponId` | Guid? | FK → Coupon (nếu có) |
| `ExpiresAt` | DateTime? | Thời hạn thanh toán |
| `PaidAt` | DateTime? | Thời điểm thanh toán |
| `CreatedAt` | DateTime | Ngày tạo |

### 2.14 OrderItem

Chi tiết dòng hàng trong đơn.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `OrderId` | Guid | FK → Order |
| `TicketTypeId` | Guid | FK → TicketType |
| `Quantity` | int | Số lượng |
| `UnitPrice` | decimal | Giá tại thời điểm mua |

### 2.15 Ticket

Vé điện tử được phát hành sau khi đơn hàng thanh toán thành công.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `EventId` | Guid | FK → Event |
| `TicketTypeId` | Guid | FK → TicketType |
| `OrderId` | Guid | FK → Order |
| `UserId` | Guid | FK → User |
| `SeatId` | Guid? | FK → Seat (nếu có chỗ ngồi) |
| `TicketCode` | string | Mã vé unique |
| `QrToken` | string | Token nhúng trong QR code |
| `Status` | string | Active / Used / Cancelled |
| `IssuedAt` | DateTime | Thời điểm phát hành |
| `CheckedInAt` | DateTime? | Thời điểm check-in |

### 2.16 Payment

Giao dịch thanh toán cho đơn hàng.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `OrderId` | Guid | FK → Order |
| `UserId` | Guid | FK → User |
| `Gateway` | string | Cổng thanh toán (VNPay, MoMo...) |
| `TransactionCode` | string? | Mã giao dịch nội bộ |
| `GatewayTransactionId` | string? | Mã giao dịch từ gateway |
| `Amount` | decimal | Số tiền |
| `Currency` | string | Đơn vị tiền tệ (VND) |
| `Status` | string | Pending / Success / Failed / Refunded |
| `PaymentUrl` | string? | URL thanh toán (redirect) |
| `PaidAt` | DateTime? | Thời điểm thanh toán thành công |

### 2.17 Coupon

Mã giảm giá.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `Code` | string | Mã coupon (unique) |
| `Name` | string? | Tên mô tả |
| `DiscountType` | string | Percent / Fixed |
| `DiscountValue` | decimal | Giá trị giảm (% hoặc số tiền) |
| `MaxDiscountAmount` | decimal? | Giảm tối đa (cho type Percent) |
| `UsageLimit` | int? | Số lần dùng tối đa |
| `UsedCount` | int | Số lần đã dùng |
| `StartTime` | DateTime | Bắt đầu hiệu lực |
| `EndTime` | DateTime | Kết thúc hiệu lực |
| `Scope` | string | Global / Event-specific |
| `EventId` | Guid? | FK → Event (nếu scope = Event) |
| `IsActive` | bool | Đang hoạt động |

### 2.18 RefundRequest

Yêu cầu hoàn tiền từ khách hàng.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `OrderId` | Guid | FK → Order |
| `UserId` | Guid | FK → User (người yêu cầu) |
| `Reason` | string? | Lý do hoàn tiền |
| `RefundAmount` | decimal | Số tiền hoàn |
| `RefundType` | string | Full / Partial |
| `Status` | string | Pending / Approved / Rejected |
| `ReviewedBy` | Guid? | FK → User (Admin xử lý) |
| `ReviewedAt` | DateTime? | Thời điểm xử lý |

### 2.19 CheckInLog

Lịch sử check-in tại sự kiện.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `TicketId` | Guid | FK → Ticket |
| `EventId` | Guid | FK → Event |
| `CheckedInBy` | Guid | FK → User (nhân viên check-in) |
| `CheckInTime` | DateTime | Thời điểm check-in |
| `Method` | string | QR / Manual |
| `Note` | string? | Ghi chú |

### 2.20 Notification

Thông báo hệ thống gửi tới người dùng.

| Trường | Kiểu | Mô tả |
|---|---|---|
| `Id` | Guid | Khóa chính |
| `UserId` | Guid | FK → User |
| `Type` | string | REMINDER / SYSTEM / AI |
| `Title` | string | Tiêu đề |
| `Content` | string? | Nội dung |
| `Status` | string | Unread / Read / Sent |
| `SentAt` | DateTime? | Thời điểm gửi |

---

## 3. Sơ Đồ Quan Hệ Tóm Tắt (ERD)

```
User ──────────────────────┐
 │                         │
 ├── many:many ── Role      │
 ├── 1:1 ── OrganizerProfile│
 │               │         │
 │               └── 1:many── Event ──── Category
 │                          │       └── Venue ── VenueZone ── Seat
 │                          │               └── EventSeatStatus
 │                          └── TicketType ────────────────────┘
 │
 ├── 1:1 ── Cart ── CartItem
 │
 ├── 1:many ── Reservation
 │
 └── 1:many ── Order ──── OrderItem
                    ├──── Payment
                    ├──── Ticket ── CheckInLog
                    ├──── Coupon (FK)
                    └──── RefundRequest
```

---

## 4. Các Enum & Constants Quan Trọng

### 4.1 Event Status (vòng đời sự kiện)

| Giá trị | Ý nghĩa |
|---|---|
| `Draft` | Nháp, chưa công bố |
| `Published` | Đã công bố, chưa mở bán |
| `OnSale` | Đang mở bán vé |
| `SoldOut` | Hết vé |
| `Ongoing` | Sự kiện đang diễn ra |
| `Completed` | Sự kiện đã kết thúc |
| `Cancelled` | Đã huỷ |

### 4.2 Organizer Status

| Giá trị | Ý nghĩa |
|---|---|
| `Pending` | Chờ duyệt |
| `Approved` | Đã được duyệt |
| `Rejected` | Bị từ chối |
| `Suspended` | Bị tạm đình chỉ |

### 4.3 Seat Status

| Giá trị | Ý nghĩa |
|---|---|
| `Available` | Ghế trống, có thể đặt |
| `Sold` | Đã bán |

### 4.4 Ticket Type Status

| Giá trị | Ý nghĩa |
|---|---|
| `Active` | Đang bán |
| `Inactive` | Tạm dừng bán |

### 4.5 User Account Status

| Giá trị | Ý nghĩa |
|---|---|
| `ACTIVE` | Hoạt động |
| `INACTIVE` | Chưa kích hoạt |
| `BANNED` | Bị cấm |
| `DELETED` | Đã xóa |

---

*→ Xem tiếp: Part 3 - Các module chức năng & API*
