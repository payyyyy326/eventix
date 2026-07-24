# LUỒNG NGHIỆP VỤ ĐẶT VÉ EVENTIX

## 1. Phạm vi chức năng

Phần đặt vé quản lý toàn bộ vòng đời của một vé:

```text
Xem sự kiện
    ↓
Chọn loại vé, số lượng và ghế
    ↓
Giữ vé trong 15 phút
    ├── Thanh toán thành công → Phát hành vé QR
    ├── Người dùng hủy       → Trả lại vé và ghế
    └── Quá 15 phút          → Tự động trả lại vé và ghế
```

Các đối tượng chính:

- `Reservation`: lượt giữ vé tạm thời.
- `Order`: đơn hàng được tạo từ lượt giữ vé.
- `OrderItem`: thông tin loại vé, ghế, số lượng và giá trong đơn hàng.
- `Payment`: giao dịch thanh toán.
- `Ticket`: vé điện tử được phát hành sau khi thanh toán.
- `CheckInLog`: lịch sử sử dụng vé tại cổng.
- `EventSeatStatus`: trạng thái ghế của một sự kiện.

Hệ thống chia vé thành hai loại:

| Loại vé | Cấu hình | Cách người dùng đặt |
|---|---|---|
| Vé đứng | `VenueZone.HasSeats = false` | Chọn khu vé và nhập số lượng |
| Vé ngồi | `VenueZone.HasSeats = true` | Chọn khu vé rồi chọn các số ghế cụ thể |

Với vé ngồi, số lượng không được nhập thủ công. Giao diện tự tính
`Quantity = SeatIds.Count`.

Thứ tự lựa chọn trên giao diện:

```text
1. Chọn loại vé: Vé đứng hoặc Vé ngồi
2. Chọn khu vé thuộc loại đã chọn
3. Chọn hạng vé trong khu
4. Chọn số lượng hoặc chọn số ghế
```

Nếu sự kiện chưa cấu hình một loại khu, lựa chọn tương ứng vẫn được hiển
thị nhưng bị vô hiệu hóa và có thông báo, ví dụ
`Sự kiện chưa có khu ghế`.

Tên khu ưu tiên lấy từ `TicketType.ZoneName`. Với dữ liệu cũ chưa có
`VenueZoneId/ZoneName`, giao diện dùng `TicketType.Section` làm tên khu
dự phòng để người dùng vẫn có thể chọn khu.

---

## 2. Xem thông tin đặt vé

### Giao diện

Người dùng mở trang chi tiết sự kiện và bấm **Đặt vé**.

### API

```http
GET /api/events/{eventId}/booking
```

API trả về:

- Thông tin sự kiện.
- Danh sách loại vé.
- Giá vé.
- Số lượng vé còn lại.
- Thời gian mở bán.
- Danh sách ghế đang khả dụng.

Số vé còn lại được tính theo công thức:

```text
AvailableQuantity = Quantity - SoldQuantity - ReservedQuantity
```

Với loại vé có `IsSeatRequired = true`, người dùng phải chọn một ghế.

---

## 3. Tạo lượt giữ vé

### API

```http
POST /api/bookings
Authorization: Bearer {token}
```

Ví dụ request:

```json
{
  "eventId": "event-guid",
  "ticketTypeId": "ticket-type-guid",
  "quantity": 3,
  "seatIds": [
    "seat-a01-guid",
    "seat-a02-guid",
    "seat-a03-guid"
  ]
}
```

### Kiểm tra nghiệp vụ

`BookingService.CreateBookingAsync` kiểm tra:

1. Loại vé thuộc đúng sự kiện.
2. Loại vé đang hoạt động.
3. Thời điểm hiện tại nằm trong thời gian mở bán.
4. Sự kiện chưa bị hủy hoặc kết thúc.
5. Số lượng vé còn lại đủ đáp ứng.
6. Vé ngồi phải có danh sách `SeatIds`.
7. Số phần tử trong `SeatIds` phải bằng `Quantity`.
8. Không được chọn trùng một ghế.
9. Tất cả ghế phải thuộc đúng sự kiện, khu và loại vé.
10. Tất cả ghế phải đang ở trạng thái `Available`.
11. Mỗi lần được chọn tối đa 10 ghế.
12. Vé đứng không được gửi `SeatIds`.

### Thay đổi dữ liệu

Hệ thống chạy transaction với mức cô lập `Serializable`:

```text
Vé đứng:
    Tạo một Reservation với Quantity đã chọn

Vé ngồi:
    Tạo một Reservation cho mỗi ghế
    Các Reservation có cùng CreatedAt và ExpiresAt

Mỗi Reservation:
    Status = Active
    ExpiresAt = UtcNow + 15 phút

TicketType.ReservedQuantity += Quantity

Với toàn bộ ghế đã chọn:
    EventSeatStatus: Available → Reserved
```

`Serializable` giúp ngăn hai người đồng thời đặt cùng một ghế hoặc đặt
vượt quá số vé còn lại. Nếu chỉ một ghế trong nhóm không còn trống, toàn
bộ yêu cầu thất bại và không ghế nào bị giữ một phần.

### Giao diện sơ đồ ghế

Khi chọn một loại vé ngồi, Web hiển thị:

- Sân khấu ở phía trên.
- Các ghế được sắp xếp theo hàng và số.
- Mã ghế như `A1`, `A2`, `B1`.
- Màu ghế còn trống.
- Màu nổi bật cho ghế đang chọn.
- Ghế `Reserved` hoặc `Sold` vẫn nằm đúng vị trí trên sơ đồ nhưng có màu
  không khả dụng và không thể chọn.
- Chú thích trạng thái ghế.
- Danh sách mã ghế đã chọn.
- Số lượng và tổng tiền cập nhật ngay lập tức.

Giao diện lọc loại vé và ghế theo khu đã chọn. Người dùng không nhìn thấy
loại vé hoặc ghế của khu khác.

Khi chọn vé đứng, sơ đồ ghế được ẩn và người dùng nhập số lượng vé.

---

## 4. Xem vé đang giữ

### Giao diện

```text
/Commerce/Bookings
```

Người dùng có thể mở mục **Vé đang giữ** trên thanh điều hướng.

### API

```http
GET /api/bookings/my
Authorization: Bearer {token}
```

Trang hiển thị:

- Sự kiện.
- Loại vé.
- Số lượng.
- Ghế.
- Tổng tiền vé.
- Trạng thái giữ vé.
- Đồng hồ đếm ngược.
- Nút **Thanh toán ngay**.
- Nút **Hủy giữ vé**.

Vé chưa thanh toán chưa xuất hiện trong **Vé của tôi**, vì `Ticket` chỉ được tạo sau khi thanh toán thành công.

---

## 5. Tạo đơn hàng

Khi người dùng bấm **Thanh toán ngay**, Web gọi:

```http
POST /api/orders
Authorization: Bearer {token}
```

Request:

```json
{
  "reservationIds": [
    "reservation-a01-guid",
    "reservation-a02-guid",
    "reservation-a03-guid"
  ]
}
```

`CommerceService.CreateOrderAsync` kiểm tra:

1. Tất cả Reservation tồn tại.
2. Tất cả Reservation thuộc người dùng hiện tại.
3. Tất cả Reservation đang có trạng thái `Active`.
4. Không Reservation nào hết hạn.
5. Không Reservation nào thuộc một order khác.

Sau đó hệ thống tạo:

```text
Order
    Status = Pending
    SubTotal = UnitPrice × Quantity
    ServiceFee = SubTotal × 2%
    TotalAmount = SubTotal + ServiceFee
    ExpiresAt = Reservation.ExpiresAt

OrderItem
    EventId
    TicketTypeId
    SeatId
    Quantity
    UnitPrice
    TotalPrice

Tất cả Reservation.OrderId = Order.Id
```

Nếu toàn bộ nhóm reservation đã thuộc cùng một order, hệ thống trả lại
order cũ thay vì tạo trùng.

---

## 6. Thanh toán

### API Demo

```http
POST /api/payments/demo/complete
Authorization: Bearer {token}
```

Request:

```json
{
  "orderId": "order-guid"
}
```

Thanh toán Demo không trừ tiền thật. Nó được dùng để kiểm thử đầy đủ luồng nghiệp vụ khi chưa có merchant key của VNPay hoặc MoMo.

### Kiểm tra nghiệp vụ

Hệ thống kiểm tra:

1. Order thuộc người dùng hiện tại.
2. Order đang ở trạng thái `Pending`.
3. Order chưa hết hạn.
4. Reservation liên quan vẫn đang `Active`.

### Xác nhận thanh toán

Toàn bộ thao tác được thực hiện trong một transaction:

```text
Payment
    Status = Success
    Gateway = Demo
    PaidAt = UtcNow

Order
    Pending → Paid

Reservation
    Active → Confirmed

TicketType
    ReservedQuantity -= Quantity
    SoldQuantity += Quantity

Ghế
    Reserved → Sold

Ticket
    Sinh TicketCode duy nhất
    Sinh QrToken duy nhất
    Status = Active
```

Mỗi vé được phát hành thành một bản ghi `Ticket`. Với vé ngồi, mỗi Ticket
liên kết với đúng một `SeatId`.

---

## 7. Vé điện tử

### Danh sách vé

```http
GET /api/tickets/my
Authorization: Bearer {token}
```

Giao diện:

```text
/Commerce/Tickets
```

### Chi tiết vé

```http
GET /api/tickets/{ticketId}
Authorization: Bearer {token}
```

Vé điện tử hiển thị:

- Tên sự kiện.
- Thời gian.
- Địa điểm.
- Loại vé.
- Số ghế.
- Mã vé.
- QR code.
- Trạng thái vé.

QR được tạo từ `QrToken`. Người dùng có thể sử dụng chức năng in của trình duyệt để in hoặc lưu vé thành PDF.

---

## 8. Hủy lượt giữ vé

### API

```http
DELETE /api/bookings/{bookingId}
Authorization: Bearer {token}
```

Chỉ reservation đang `Active` và thuộc người dùng hiện tại mới được hủy.

Khi hủy:

```text
Reservation: Active → Cancelled
TicketType.ReservedQuantity -= Quantity
Ghế: Reserved → Available
Order liên quan: Pending → Cancelled
```

Hệ thống sử dụng transaction `Serializable` để tránh việc tác vụ hết hạn và người dùng hủy cùng một reservation hai lần.

---

## 9. Tự động hết hạn

`BookingExpirationJob` chạy mỗi phút.

Job tìm các reservation thỏa mãn:

```text
Status = Active
ExpiresAt <= UtcNow
```

Sau đó:

```text
Reservation: Active → Expired
Order liên quan: Pending → Expired
TicketType.ReservedQuantity -= Quantity
Ghế: Reserved → Available
```

Do job chạy mỗi phút, dữ liệu có thể được cập nhật chậm tối đa khoảng một phút. Tuy nhiên, API thanh toán kiểm tra trực tiếp `ExpiresAt`, nên order hết hạn không thể được thanh toán.

---

## 10. Check-in

### API

```http
POST /api/checkin/scan
Authorization: Bearer {organizer-or-admin-token}
```

Cách 1 - nhập token thủ công:

```json
POST /api/checkin/scan
{
  "qrToken": "ticket-qr-token",
  "eventId": "event-guid"
}
```

Cách 2 - tải ảnh QR từ máy:

```http
POST /api/checkin/scan-image
Content-Type: multipart/form-data

eventId={event-guid}
qrImage={png-jpg-webp-bmp, tối đa 5 MB}
```

API dùng SkiaSharp đọc ảnh và ZXing giải mã QR thành `QrToken`, sau đó cả hai cách
đều gọi chung nghiệp vụ `CommerceService.CheckInAsync`.

Hệ thống kiểm tra:

1. Người thực hiện là Organizer hoặc Admin.
2. Organizer có quyền quản lý sự kiện.
3. QR token tồn tại.
4. Vé thuộc đúng sự kiện.
5. Vé đang ở trạng thái `Active`.

Khi hợp lệ:

```text
Ticket: Active → Used
Ticket.CheckedInAt = UtcNow
Tạo CheckInLog
```

Nếu quét lại cùng một vé, hệ thống từ chối vì vé đã có trạng thái `Used`.

### Thống kê

```http
GET /api/checkin/event/{eventId}/stats
```

Kết quả gồm:

- Tổng số vé.
- Số vé đã check-in.
- Số vé chưa check-in.

Organizer/Admin có thể lấy danh sách vé của sự kiện qua:

```http
GET /api/tickets/event/{eventId}
```

---

## 11. Phân bổ ghế VIP

Khi publish sự kiện, hệ thống:

1. Lấy các loại vé yêu cầu ghế.
2. Nhóm loại vé theo `VenueZoneId`.
3. Lấy danh sách ghế thuộc từng zone.
4. Phân bổ ghế lần lượt cho các loại vé.
5. Tạo `EventSeatStatus` với trạng thái `Available`.

Luồng trạng thái ghế:

```text
Available → Reserved → Sold
              │
              ├── Hủy giữ vé → Available
              └── Hết hạn    → Available
```

---

## 12. Bảng trạng thái

| Đối tượng | Trạng thái |
|---|---|
| Reservation | `Active → Confirmed / Cancelled / Expired` |
| Order | `Pending → Paid / Cancelled / Expired` |
| Payment | `Pending → Success / Failed` |
| Ticket | `Active → Used / Cancelled` |
| Ghế thanh toán thành công | `Available → Reserved → Sold` |
| Ghế không thanh toán | `Available → Reserved → Available` |

---

## 13. Các API chính

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/events/{id}/booking` | Lấy thông tin phục vụ đặt vé |
| POST | `/api/bookings` | Giữ vé trong 15 phút |
| GET | `/api/bookings/my` | Xem lịch sử giữ vé |
| DELETE | `/api/bookings/{id}` | Hủy giữ vé |
| POST | `/api/orders` | Tạo order từ reservation |
| GET | `/api/orders/my` | Xem lịch sử đơn hàng |
| POST | `/api/payments/demo/complete` | Thanh toán Demo |
| GET | `/api/tickets/my` | Xem vé đã thanh toán |
| GET | `/api/tickets/{id}` | Xem chi tiết vé |
| GET | `/api/tickets/event/{eventId}` | Quản lý vé theo sự kiện |
| POST | `/api/checkin/scan` | Check-in vé |
| GET | `/api/checkin/event/{eventId}/stats` | Thống kê check-in |

---

## 14. Cấu trúc code

```text
Eventix.Share/
└── DTOs/
    ├── Booking/
    └── Commerce/

Eventix/
├── Infrastructure/Jobs/
│   └── BookingExpirationJob.cs
├── Modules/BookingModule/
│   ├── Controllers/
│   ├── Interfaces/
│   └── Services/
└── Modules/CommerceModule/
    ├── Controllers/
    ├── Interfaces/
    └── Services/

Eventix.Web/
├── Controllers/
│   ├── EventController.cs
│   └── CommerceController.cs
└── Views/
    ├── Event/
    │   ├── Booking.cshtml
    │   └── BookingConfirmation.cshtml
    └── Commerce/
        ├── Bookings.cshtml
        ├── Checkout.cshtml
        ├── Orders.cshtml
        ├── Tickets.cshtml
        ├── Ticket.cshtml
        └── CheckIn.cshtml
```
---

## 15. Thông báo email trong vòng đời đặt vé

Hệ thống sử dụng `IEmailService`/`EmailService` (MailKit + SMTP) để gửi thông báo tới
email của tài khoản thực hiện thao tác.

### Giữ vé thành công

```text
POST /api/bookings
    → kiểm tra vé/ghế còn trống
    → tạo Reservation và chuyển ghế sang Reserved
    → commit transaction
    → gửi email "Giữ vé thành công"
```

Email bao gồm tên sự kiện, hạng vé, số ghế, số lượng, tổng tiền và thời điểm hết hạn
giữ vé theo GMT+7. Email ghi rõ đây mới là lượt giữ vé 15 phút; người dùng phải thanh
toán trước hạn để nhận vé điện tử.

### Thanh toán và đặt vé thành công

```text
POST /api/payments/demo/complete
    → xác nhận Order còn hiệu lực
    → chuyển Order sang Paid và Reservation sang Confirmed
    → chuyển ghế sang Sold, phát hành Ticket/QR
    → commit transaction
    → gửi email "Đặt vé thành công"
```

Email xác nhận thời điểm thanh toán, mã đơn hàng, sự kiện, hạng vé, ghế và tổng tiền.
Mỗi vé được tạo một ảnh QR PNG từ QrToken, nhúng trực tiếp vào email bằng Content-ID
(cid:) và hiển thị cùng TicketCode. Người dùng có thể quét QR ngay trong email hoặc
mở mục **Vé của tôi** để xem lại.
### Hủy vé thành công

```text
DELETE /api/bookings/{id}
    → chuyển Reservation sang Cancelled
    → trả số lượng vé và ghế về Available
    → hủy Order Pending liên quan (nếu có)
    → commit transaction
    → gửi email "Đã hủy vé"
```

Nếu một order chứa nhiều reservation, email hủy liệt kê toàn bộ vé/ghế được hủy cùng
nhau.

### Hết 15 phút chưa thanh toán

```text
BookingExpirationJob
    → tìm Reservation Active đã quá ExpiresAt
    → chuyển Reservation/Order sang Expired
    → trả số lượng vé và ghế về Available
    → commit transaction
    → gửi email "Đặt vé thất bại"
```

Email giải thích lượt giữ vé đã hết hạn do chưa thanh toán, liệt kê vé/ghế được trả
lại và hướng dẫn người dùng đặt lại nếu vé vẫn còn. Các reservation thuộc cùng một
order được gộp trong một email.
### Xử lý lỗi gửi mail

Email luôn được gửi sau khi transaction dữ liệu đã commit. Nếu SMTP tạm thời lỗi,
`BookingService` ghi lỗi qua `ILogger` nhưng không rollback kết quả đặt/hủy vé và
không trả lỗi nghiệp vụ cho người dùng.
