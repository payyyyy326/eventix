# 04. CÁC LUỒNG NGHIỆP VỤ EVENTIX

## 1. Quy ước

- `Web`: Eventix.Web MVC.
- `API`: Eventix Web API.
- `DB`: SQL Server qua EF Core.
- Các thời điểm nghiệp vụ được lưu UTC; email hiển thị GMT+7.
- Các email booking được gửi sau commit và không làm rollback nghiệp vụ nếu SMTP lỗi.

## 2. Luồng tổng thể từ sự kiện tới check-in

```mermaid
flowchart LR
    A[Tạo venue/zone/seat] --> B[Tạo event]
    B --> C[Tạo ticket type]
    C --> D[Publish/OnSale]
    D --> E[Khách chọn khu/vé/ghế]
    E --> F[Giữ 15 phút]
    F -->|Thanh toán| G[Order Paid]
    G --> H[Phát hành Ticket + QR]
    H --> I[Check-in]
    F -->|Hủy| J[Trả vé/ghế]
    F -->|Hết hạn| J
```

## 3. Đăng ký và xác thực email

### Endpoint chính

- `POST /api/auth/register`
- `POST /api/auth/verify-otp`
- `POST /api/auth/resend-otp`

```mermaid
sequenceDiagram
    actor Guest
    participant Web
    participant API as Auth API
    participant DB
    participant Mail as SMTP

    Guest->>Web: Nhập thông tin đăng ký
    Web->>API: POST /api/auth/register
    API->>DB: Kiểm tra email unique
    API->>API: BCrypt password
    API->>DB: Tạo User + EmailOtp
    API->>Mail: Gửi OTP
    API-->>Web: Đăng ký thành công/chờ OTP
    Guest->>Web: Nhập OTP
    Web->>API: POST /api/auth/verify-otp
    API->>DB: Kiểm tra purpose, code, expiry
    API->>DB: EmailVerified = true
    API-->>Web: Xác thực thành công
```

### Nhánh lỗi

- Email đã tồn tại → từ chối đăng ký.
- OTP sai/hết hạn → không xác thực.
- Resend tạo/gửi OTP mới theo rule AuthService.
- SMTP lỗi được xử lý theo hành vi AuthService; khác email booking vốn là best-effort.

## 4. Đăng nhập và refresh token

```mermaid
sequenceDiagram
    actor User
    participant Web
    participant API
    participant DB

    User->>Web: Email + password
    Web->>API: POST /api/auth/login
    API->>DB: Tìm User + Roles
    API->>API: Verify BCrypt/status
    API->>DB: Lưu RefreshToken
    API-->>Web: AccessToken + RefreshToken
    Web->>Web: Lưu cookie
    Web->>API: Request kèm Bearer JWT
    alt Access token hết hạn
        Web->>API: POST /api/auth/refresh-token
        API->>DB: Xác minh refresh token
        API-->>Web: Token mới
    end
```

JWT được kiểm tra issuer, audience, signing key, lifetime và không có clock skew.

## 5. Đăng ký và duyệt Organizer

```mermaid
sequenceDiagram
    actor Customer
    actor Admin
    participant API
    participant DB

    Customer->>API: POST OrganizerProfile/create
    API->>DB: Tạo profile Pending
    API-->>Customer: Chờ phê duyệt
    Admin->>API: GET danh sách profile
    alt Chấp thuận
        Admin->>API: PATCH profile/approve
        API->>DB: Status = Approved, lưu ApprovedBy/At
        API->>DB: Bảo đảm role Organizer
    else Từ chối
        Admin->>API: PATCH profile/reject
        API->>DB: Status = Rejected + lý do
    end
```

## 6. Tạo venue và ghế

### 6.1 Venue

1. Organizer tạo Venue.
2. Venue là địa điểm vật lý, không bắt buộc phải cấu hình VenueZone trước khi tạo event.

### 6.2 Ghế được generate từ TicketType

```mermaid
sequenceDiagram
    actor Organizer
    participant Web
    participant TicketTypeAPI
    participant DB

    Organizer->>Web: Tạo Ticket Type (tên, giá, số lượng, IsSeatRequired)
    Web->>TicketTypeAPI: POST /api/OrganizerProfile/events/{id}/ticket-types
    TicketTypeAPI->>DB: Tạo TicketType (Section = tên ticket type)
    TicketTypeAPI->>DB: Tạo VenueSectionLayout (ánh xạ section → venue)
    Note over TicketTypeAPI,DB: Ghế chưa được tạo ở bước này
    Organizer->>Web: Publish event
    Web->>TicketTypeAPI: POST /api/events/{id}/publish
    TicketTypeAPI->>DB: Generate Seat theo lưới (Row × Col) từ Quantity
    TicketTypeAPI->>DB: Tạo EventSeatStatus = Available cho từng ghế
```

- Tên section của ghế lấy từ **tên TicketType** (không phải VenueZone).
- Ghế được đặt tên tự động: `{RowLabel}{Number}`, tọa độ tính theo lưới.
- `IsSeatRequired = true`: ghế ngồi, buyer chọn ghế cụ thể trên seat map.
- `IsSeatRequired = false`: vé đứng, buyer chỉ nhập số lượng.
- Ghế đã tạo ở lần publish trước sẽ được **skip** để tránh trùng.
- `VenueZoneId = null` trong luồng này; VenueZone là module tùy chọn riêng.

## 7. Tạo sự kiện bằng Event Wizard

```mermaid
flowchart TD
    S1[Thông tin cơ bản] --> S2[Chọn/tạo venue]
    S2 --> S3[Tạo ticket type + cấu hình ghế]
    S3 --> S4[Upload banner/images]
    S4 --> S5[Review]
    S5 --> S6[Lưu Draft hoặc Publish]
```

### Quy tắc

- Event liên kết Category, Venue và OrganizerProfile.
- Ticket type khai báo **tên section, quota, price, sale window** và `IsSeatRequired`.
- Section của seat map được tạo tự động từ **tên TicketType** — không cần cấu hình VenueZone riêng.
- Ghế được generate khi Publish; mỗi lần publish chỉ tạo ghế chưa tồn tại.
- Event chỉ nên publish khi thông tin bắt buộc, venue và ticket type hợp lệ.
- Ảnh được lưu vào vùng upload và URL được lưu trong database.

## 8. Publish và tự động cập nhật trạng thái Event

`EventStatusJob` chạy mỗi phút.

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Published: Organizer publish
    Published --> OnSale: đến thời gian bán
    OnSale --> SoldOut: hết quota
    OnSale --> Ongoing: đến StartTime
    SoldOut --> Ongoing: đến StartTime
    Ongoing --> Completed: qua EndTime
    Draft --> Cancelled
    Published --> Cancelled
    OnSale --> Cancelled
```

Job không được ghi đè `Cancelled`. Mọi thay đổi được lưu theo batch của lần chạy.

## 9. Xem thông tin booking

### Endpoint

`GET /api/events/{eventId}/booking`

API trả:

- Event, venue và thời gian.
- Ticket type, zone, `HasSeats`, quota còn lại.
- Toàn bộ ghế kèm status và tọa độ.

Ghế Reserved/Sold vẫn được trả về để UI giữ đúng bố cục, nhưng bị disable và hiển
thị “không khả dụng”. Ghế được sắp theo section, row, XPosition và number.

## 10. Giữ vé đứng

```mermaid
sequenceDiagram
    actor Customer
    participant Web
    participant API as Booking API
    participant DB
    participant Mail

    Customer->>Web: Chọn Vé đứng → zone → hạng vé → quantity
    Web->>API: POST /api/bookings
    API->>DB: BEGIN Serializable
    API->>DB: Đọc TicketType/Event
    API->>API: Kiểm tra sale window/status/quota
    API->>DB: Tạo Reservation Active, ExpiresAt +15 phút
    API->>DB: ReservedQuantity += quantity
    API->>DB: COMMIT
    API->>Mail: Email "Giữ vé thành công"
    API-->>Web: BookingResponse
```

Vé đứng không nhận SeatIds. Nếu client gửi ghế cho ticket type không yêu cầu ghế,
API từ chối.

## 11. Giữ vé ngồi và chống trùng ghế

```mermaid
sequenceDiagram
    actor Customer
    participant Web
    participant API as Booking API
    participant DB

    Customer->>Web: Chọn Vé ngồi → zone → ticket type
    Web->>API: GET event booking/seat map
    Customer->>Web: Chọn các ghế Available
    Web->>API: POST /api/bookings với SeatIds
    API->>DB: BEGIN Serializable
    API->>API: Distinct SeatIds; count = Quantity
    API->>DB: Lấy EventSeatStatus Available
    alt Tất cả ghế còn trống
        API->>DB: Tạo một Reservation/ghế
        API->>DB: SeatStatus → Reserved
        API->>DB: ReservedQuantity += số ghế
        API->>DB: COMMIT
        API-->>Web: Danh sách reservation
    else Một ghế không còn Available
        API->>DB: ROLLBACK
        API-->>Web: 409 Seat not available
    end
```

Server là nguồn sự thật. Việc ghế hiển thị Available ở trình duyệt không bảo đảm ghế
còn trống tại thời điểm submit.

## 12. Tạo order

### Endpoint

`POST /api/orders`

```mermaid
sequenceDiagram
    actor Customer
    participant API as Orders API
    participant DB

    Customer->>API: ReservationIds
    API->>DB: BEGIN Serializable
    API->>DB: Lấy reservation thuộc User
    API->>API: Kiểm tra Active và chưa hết hạn
    API->>DB: Lấy giá TicketType
    API->>API: Tính SubTotal + ServiceFee - Discount
    API->>DB: Tạo Order Pending + OrderItems
    API->>DB: Gắn Reservation.OrderId
    API->>DB: COMMIT
    API-->>Customer: OrderResponse + thời hạn
```

Nếu toàn bộ reservation đã thuộc cùng một order, service trả lại order đó thay vì tạo
trùng. Reservation thuộc order khác bị từ chối.

## 13. Thanh toán và phát hành vé QR

### Endpoint Demo

`POST /api/payments/demo/complete`

```mermaid
sequenceDiagram
    actor Customer
    participant API as Payment API
    participant DB
    participant QR as QRCoder
    participant Mail

    Customer->>API: OrderId
    API->>DB: BEGIN Serializable
    API->>DB: Lấy Order + Items + Reservations
    API->>API: Kiểm tra Pending, chưa hết hạn
    loop Mỗi reservation
        API->>DB: ReservedQuantity giảm; SoldQuantity tăng
        API->>DB: Reservation → Confirmed
        API->>DB: Ghế → Sold (nếu có)
        API->>DB: Tạo Ticket Active + TicketCode + QrToken
    end
    API->>DB: Order → Paid; tạo Payment Success
    API->>DB: COMMIT
    API->>QR: Tạo PNG từ từng QrToken
    API->>Mail: Email "Đặt vé thành công" + QR inline
    API-->>Customer: PaymentResponse
```

### Idempotency hiện tại

Nếu Order đã Paid, API tìm Payment Success và trả lại response, không phát hành vé
lần nữa. Email chỉ được gửi ở nhánh thanh toán mới thành công.

### QR

- `QrToken` là payload được mã hóa thành QR.
- TicketCode và QrToken đều unique.
- Email dùng QRCoder tạo PNG và MimeKit gắn `Content-ID` cho từng ảnh.

## 14. Hủy lượt giữ

### Endpoint

`DELETE /api/bookings/{reservationId}`

```mermaid
sequenceDiagram
    actor Customer
    participant API as Booking API
    participant DB
    participant Mail

    Customer->>API: Hủy reservation của mình
    API->>DB: BEGIN Serializable
    API->>DB: Kiểm tra ownership + Active
    API->>DB: Lấy nhóm cùng Order nếu có
    loop Mỗi reservation Active
        API->>DB: Status → Cancelled
        API->>DB: ReservedQuantity giảm
        API->>DB: Ghế Reserved → Available
    end
    API->>DB: Order Pending → Cancelled
    API->>DB: COMMIT
    API->>Mail: Email "Đã hủy vé"
    API-->>Customer: Thành công
```

Reservation Confirmed/Expired/Cancelled không thể hủy bằng luồng giữ vé này.

## 15. Tự động hết hạn sau 15 phút

`BookingExpirationJob` chạy mỗi phút và có `DisallowConcurrentExecution`.

```mermaid
sequenceDiagram
    participant Q as Quartz
    participant Job as BookingExpirationJob
    participant DB
    participant Mail

    Q->>Job: Trigger mỗi phút
    Job->>DB: BEGIN Serializable
    Job->>DB: Tìm Active và ExpiresAt <= now
    loop Mỗi booking
        Job->>DB: Reservation → Expired
        Job->>DB: Order Pending → Expired
        Job->>DB: ReservedQuantity giảm
        Job->>DB: Ghế Reserved → Available
    end
    Job->>DB: COMMIT
    Job->>Mail: Email "Đặt vé thất bại" theo nhóm order
```

Do chu kỳ một phút, email có thể đến ở phút 15–16. Nhiều reservation cùng order được
gộp thành một email.

## 16. Vòng đời email booking

```mermaid
stateDiagram-v2
    [*] --> HeldMail: Giữ vé commit
    HeldMail --> SuccessMail: Thanh toán commit
    HeldMail --> CancelMail: Hủy commit
    HeldMail --> ExpiredMail: Job hết hạn commit
```

| Mốc | Subject | Nội dung chính |
|---|---|---|
| Giữ | Giữ vé thành công | Hạng vé/ghế/tổng tiền/hạn 15 phút |
| Thanh toán | Đặt vé thành công | Order, ticket code, QR inline |
| Hủy | Đã hủy vé | Vé/ghế đã trả hệ thống |
| Hết hạn | Đặt vé thất bại | Lý do quá hạn và hướng dẫn đặt lại |

SMTP exception được catch và log. Trạng thái DB đã commit không bị rollback.

## 17. Xem vé điện tử

- `GET /api/tickets/my`: danh sách vé của user.
- `GET /api/tickets/{id}`: chi tiết vé thuộc user.
- `GET /api/tickets/event/{eventId}`: danh sách vé sự kiện cho Organizer/Admin.

Razor View dùng qrcode.js để dựng QR trên trang chi tiết; email dùng QRCoder phía API.
Cả hai cùng mã hóa đúng `QrToken`.

## 18. Check-in

### Endpoint

- Nhập token: `POST /api/checkin/scan` với JSON.
- Tải ảnh: `POST /api/checkin/scan-image` với multipart, tối đa 5 MB.

```mermaid
sequenceDiagram
    actor Staff
    participant Web
    participant API as CheckIn API
    participant Decoder as SkiaSharp + ZXing
    participant DB

    alt Nhập token thủ công
        Staff->>Web: EventId + QrToken
        Web->>API: POST /api/checkin/scan
    else Tải ảnh QR
        Staff->>Web: EventId + ảnh PNG/JPG/WEBP/BMP
        Web->>API: POST /api/checkin/scan-image (multipart)
        API->>Decoder: Đọc ảnh và giải mã QR
        Decoder-->>API: QrToken
    end

    API->>DB: Kiểm tra quyền Admin/Organizer sở hữu event
    API->>DB: BEGIN Serializable
    API->>DB: Tìm Ticket theo QrToken
    alt Ticket thuộc event và Active
        API->>DB: Ticket → Used, CheckedInAt = now
        API->>DB: Tạo CheckInLog
        API->>DB: COMMIT
        API-->>Web: Thông tin khách/vé/ghế
        Web-->>Staff: Check-in thành công
    else Ảnh không có QR, QR sai, khác event hoặc đã dùng
        API->>DB: ROLLBACK nếu đã mở transaction
        API-->>Web: Từ chối và hiển thị lý do
    end
```

Hai cách nhập chỉ khác bước lấy `QrToken`; authorization, ownership, kiểm tra trạng
thái vé và chống check-in hai lần dùng chung `CommerceService.CheckInAsync`.

Thống kê `GET /api/checkin/event/{eventId}/stats` trả tổng ticket không Cancelled,
số Used và số còn lại.
## 19. Luồng lỗi và response

API sử dụng response chuẩn:

```text
ApiResponseModel<T>
├── IsSuccess / Code / Message
└── Data
```

- Validation/business rule → 400.
- Không tìm thấy → 404.
- Tranh chấp ghế → 409.
- Chưa đăng nhập/token hết hạn → 401.
- Thiếu quyền/không sở hữu → 403.
- Lỗi không xử lý → middleware trả response lỗi chuẩn và log.

Trong transaction, exception dẫn tới rollback. Riêng lỗi email xảy ra sau commit và
bị catch để không đổi kết quả nghiệp vụ.

## 20. Các luồng dự kiến chưa triển khai

Những luồng sau chưa có implementation:

- Payment gateway thật (VNPay, MoMo), callback/webhook và reconciliation.
- AI tagging và recommendation.

## 21. Checklist kiểm thử luồng

### Booking

- Hai user chọn cùng ghế gần đồng thời: chỉ một người thành công.
- Chọn ghế Sold/Reserved: 409 và không tăng counter.
- Vé đứng quantity vượt quota: bị từ chối.
- Hủy nhóm nhiều ghế: trả đủ counter và ghế.

### Payment

- Order hết hạn không thanh toán được.
- Gọi thanh toán hai lần không tạo ticket trùng.
- Nhiều ghế tạo đúng số ticket và QR.

### Expiration

- Sau 15–16 phút reservation/order thành Expired.
- Ghế trở lại Available và có thể đặt lại.
- Một email thất bại cho mỗi order.

### Check-in

- QR đúng/event đúng/ticket Active: thành công.
- Quét lại QR: thất bại.
- Organizer khác event: 403.

### Email

- Mail giữ, thành công, hủy, hết hạn đến đúng tài khoản.
- Gmail/Outlook hiển thị QR inline.
- SMTP lỗi không làm API báo thất bại sau khi transaction đã commit.