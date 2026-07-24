# DANH SÁCH TÍNH NĂNG CẦN HOÀN THIỆN - EVENTIX
> Cập nhật: 2026-07-19 | Dựa trên phân tích code hiện tại

---

## Trạng thái tổng quan

| Module | Trạng thái | Ghi chú |
|---|---|---|
| Auth | ✅ Hoàn thành | |
| User | ✅ Hoàn thành | |
| Category | ✅ Cơ bản xong | Thiếu GetById, Delete |
| Venue | ✅ Cơ bản xong | Thiếu Delete, thiếu check quyền |
| VenueZone | ✅ Hoàn thành | |
| Seat | ✅ Hoàn thành | |
| TicketType | ✅ Hoàn thành | |
| Organizer | ✅ Hoàn thành | |
| Event | ✅ Cơ bản xong | Thiếu Delete, GetByFeature |
| Event Wizard (Web) | ✅ Hoàn thành | |
| **Reservation** | ❌ Chưa làm | Folder rỗng |
| **Order** | ❌ Chưa làm | Folder rỗng |
| **Payment** | ❌ Chưa làm | Folder rỗng |
| **Ticket** | ❌ Chưa làm | Folder rỗng |
| **CheckIn** | ❌ Chưa làm | Folder rỗng |
| **Reports** | ❌ Chưa làm | Folder rỗng |
| **Admin** | ❌ Chưa làm | Folder rỗng |
| QR Code | ❌ Chưa làm | Infrastructure/QR rỗng |
| PDF Ticket | ❌ Chưa làm | Infrastructure/Pdf rỗng |
| Payment Gateway | ❌ Chưa làm | Infrastructure/Payment rỗng |
| File Storage | ❌ Chưa làm | Infrastructure/Storage rỗng |

---

## CHI TIẾT TỪNG NHÓM TÍNH NĂNG

---

## 2. 🛒 MODULE RESERVATION (Giữ chỗ tạm thời)

> **Mục đích:** Tránh oversell khi nhiều người cùng thanh toán. Ghế/vé bị lock tạm trong ~10-15 phút.

- [ ] **API:** `POST /api/reservations` — Tạo reservation (giữ chỗ)
  - Input: `eventId`, `ticketTypeId`, `seatId?`, `quantity`
  - Validate: TicketType còn vé (`quantity > soldQuantity + reservedQuantity`)
  - Validate: Seat chưa bị Reserved/Sold (nếu có chỗ ngồi)
  - Tạo `Reservation` với `Status=Pending`, `ExpiresAt = now + 15 phút`
  - Tăng `TicketType.ReservedQuantity += quantity`
  - Set `EventSeatStatus = Reserved` (nếu có ghế)

- [ ] **API:** `DELETE /api/reservations/{id}` — Huỷ reservation
  - Giảm `ReservedQuantity`
  - Set lại `EventSeatStatus = Available`

- [ ] **Background Job:** Tự động expire reservation quá hạn (Quartz)
  - Mỗi 1 phút quét `Reservation` có `ExpiresAt <= now && Status=Pending`
  - Set `Status = Expired`
  - Giảm `ReservedQuantity`
  - Trả ghế về `Available`

- [ ] **API:** `GET /api/reservations/my` — Xem reservation của user hiện tại

---

## 3. 📦 MODULE ORDER (Đơn hàng)

> **Mục đích:** Tạo đơn hàng từ reservation, tính giá, áp coupon.

- [ ] **API:** `POST /api/orders` — Tạo đơn hàng
  - Input: danh sách `reservationId[]`, `couponCode?`
  - Validate: Reservation còn hạn, thuộc về user này
  - Tính `subTotal = sum(quantity * price)`
  - Tính `serviceFee` (ví dụ 2% hoặc cố định)
  - Validate & áp dụng coupon (nếu có) → `discountAmount`
  - Tính `totalAmount = subTotal + serviceFee - discountAmount`
  - Tạo `Order` với `Status=Pending`, `ExpiresAt = now + 15 phút`
  - Tạo `OrderItem` cho mỗi reservation
  - Ghi `CouponUsage` (nếu dùng coupon)

- [ ] **API:** `GET /api/orders` — Lịch sử đơn hàng của user (có phân trang)

- [ ] **API:** `GET /api/orders/{id}` — Chi tiết đơn hàng

- [ ] **API:** `DELETE /api/orders/{id}` — Huỷ đơn hàng (khi chưa thanh toán)
  - Set `Status = Cancelled`
  - Trả lại reservation → giảm `ReservedQuantity`
  - Rollback `CouponUsage`

- [ ] **Background Job:** Tự động huỷ đơn hàng quá hạn chưa thanh toán
  - Quét `Order` có `ExpiresAt <= now && Status=Pending`
  - Cascade huỷ reservation liên quan

---

## 4. 💳 MODULE PAYMENT (Thanh toán)

> **Mục đích:** Khởi tạo link thanh toán, xử lý callback từ gateway.

- [ ] **Infrastructure:** Implement `IPaymentGateway` interface
  - Implement **VNPay** adapter (tối thiểu)
  - Implement **MoMo** adapter (tuỳ chọn)

- [ ] **API:** `POST /api/payments/initiate` — Khởi tạo thanh toán
  - Input: `orderId`, `gateway` (VNPay/MoMo)
  - Validate: Order `Status=Pending`, thuộc về user này
  - Tạo `Payment` với `Status=Pending`
  - Gọi gateway → nhận `paymentUrl`
  - Trả về `paymentUrl` để redirect

- [ ] **API:** `GET /api/payments/callback` (hoặc POST) — Webhook/callback từ gateway
  - Xác thực chữ ký từ gateway (HMAC/RSA)
  - Nếu thành công:
    - Set `Payment.Status = Success`, `PaidAt = now`
    - Set `Order.Status = Paid`, `PaidAt = now`
    - Xác nhận `Reservation.Status = Confirmed`
    - Tăng `TicketType.SoldQuantity`, giảm `ReservedQuantity`
    - **Phát hành vé** → tạo `Ticket` cho mỗi item
    - Gửi email xác nhận + vé
    - Trigger SignalR nếu sự kiện SoldOut
  - Nếu thất bại:
    - Set `Payment.Status = Failed`
    - Giữ Order Pending hoặc Cancelled

- [ ] **API:** `GET /api/payments/{orderId}` — Kiểm tra trạng thái thanh toán (polling)

- [ ] **Lưu:** `PaymentWebhookLog` — log toàn bộ payload webhook thô để debug

---

## 5. 🎫 MODULE TICKET (Vé điện tử)

> **Mục đích:** Quản lý vé sau khi thanh toán thành công.

- [ ] **Service:** `IssueTickets(orderId)` — Phát hành vé (gọi từ Payment callback)
  - Tạo `Ticket` cho mỗi `OrderItem` (1 vé/ghế hoặc quantity vé)
  - Sinh `TicketCode` (unique, dạng: `EVT-XXXXXX`)
  - Sinh `QrToken` (UUID hoặc signed JWT nhỏ)
  - Set `EventSeatStatus = Sold` (nếu có ghế)

- [ ] **Infrastructure:** Implement QR Code generator (`Infrastructure/QR/`)
  - Nhận `QrToken` → sinh ảnh PNG QR code

- [ ] **Infrastructure:** Implement PDF generator (`Infrastructure/Pdf/`)
  - Template vé gồm: tên sự kiện, ngày giờ, địa điểm, tên người dùng, loại vé, số ghế, QR code

- [ ] **API:** `GET /api/tickets/my` — Danh sách vé của user (có phân trang, lọc theo event)

- [ ] **API:** `GET /api/tickets/{id}` — Chi tiết vé

- [ ] **API:** `GET /api/tickets/{id}/download` — Download vé PDF

- [ ] **API:** `GET /api/tickets/{id}/qr` — Lấy ảnh QR code của vé

---

## 6. ✅ MODULE CHECK-IN (Kiểm tra vé vào cổng)

> **Mục đích:** Nhân viên quét QR tại cổng để xác thực người tham dự.

- [ ] **API:** `POST /api/checkin/scan` — Quét QR check-in
  - Input: `qrToken`, `eventId`
  - Validate: `QrToken` tồn tại trong DB
  - Validate: `Ticket` thuộc đúng `eventId`
  - Validate: `Ticket.Status = Active` (chưa dùng)
  - Validate: Sự kiện đang ở trạng thái `OnSale` hoặc `Ongoing`
  - Set `Ticket.Status = Used`, `CheckedInAt = now`
  - Tạo `CheckInLog`
  - Trả về: tên người dùng, loại vé, số ghế, ảnh avatar

- [ ] **API:** `GET /api/checkin/event/{eventId}/stats` — Thống kê check-in realtime
  - Tổng vé, số đã check-in, số chưa check-in

- [ ] **API:** `GET /api/checkin/event/{eventId}/logs` — Lịch sử check-in của sự kiện (có phân trang)

---

## 7. 💰 MODULE COUPON (Mã giảm giá)

> **Mục đích:** Organizer/Admin tạo mã giảm giá, user áp dụng khi đặt vé.

- [ ] **API:** `POST /api/coupons` — Tạo coupon (Admin/Organizer)
  - `discountType`: `Percent` hoặc `Fixed`
  - `scope`: `Global` hoặc `Event` (kèm `eventId`)
  - Validate: `startTime < endTime`, `discountValue > 0`

- [ ] **API:** `GET /api/coupons` — Danh sách coupon (Admin/Organizer)

- [ ] **API:** `GET /api/coupons/{id}` — Chi tiết coupon

- [ ] **API:** `PATCH /api/coupons/{id}/deactivate` — Vô hiệu hoá coupon

- [ ] **API:** `POST /api/coupons/validate` — Kiểm tra coupon hợp lệ (gọi từ client trước khi tạo order)
  - Input: `code`, `eventId`, `subTotal`
  - Validate: code tồn tại, `IsActive`, trong thời hạn, chưa hết `usageLimit`
  - Tính `discountAmount` và trả về preview

---

## 8. 💸 MODULE REFUND (Hoàn tiền)

> **Mục đích:** Customer yêu cầu hoàn tiền, Admin xét duyệt.

- [ ] **API:** `POST /api/refunds` — Tạo yêu cầu hoàn tiền (Customer)
  - Input: `orderId`, `reason`, `refundType` (Full/Partial)
  - Validate: Order `Status=Paid`
  - Validate: Sự kiện chưa diễn ra (hoặc theo `RefundPolicy`)
  - Validate: Chưa có refund request pending cho order này
  - Tạo `RefundRequest` với `Status=Pending`

- [ ] **API:** `GET /api/refunds/my` — Danh sách yêu cầu hoàn tiền của user

- [ ] **API:** `GET /api/refunds` — Danh sách tất cả yêu cầu (Admin, lọc theo status)

- [ ] **API:** `PATCH /api/refunds/{id}/approve` — Duyệt hoàn tiền (Admin)
  - Gọi Payment Gateway để hoàn tiền thực tế
  - Set `Order.Status = Refunded`
  - Set `Ticket.Status = Cancelled` cho tất cả vé của order
  - Giảm `TicketType.SoldQuantity`
  - Gửi email thông báo

- [ ] **API:** `PATCH /api/refunds/{id}/reject` — Từ chối hoàn tiền (Admin)
  - Gửi email thông báo lý do

- [ ] **Entity:** Implement `RefundPolicy` — chính sách hoàn tiền theo sự kiện
  - Ví dụ: hoàn 100% nếu trước 7 ngày, hoàn 50% nếu trước 3 ngày, không hoàn nếu dưới 24h

---

## 9. 🔔 MODULE NOTIFICATION (Thông báo)

> **Mục đích:** Gửi thông báo in-app và email đến người dùng.

- [ ] **API:** `GET /api/notifications` — Danh sách thông báo của user (có phân trang)

- [ ] **API:** `PATCH /api/notifications/{id}/read` — Đánh dấu đã đọc

- [ ] **API:** `PATCH /api/notifications/read-all` — Đánh dấu tất cả đã đọc

- [ ] **API:** `GET /api/notifications/unread-count` — Đếm thông báo chưa đọc

- [ ] **Service:** `INotificationService.SendAsync(userId, type, title, content)`
  - Tạo record `Notification` trong DB
  - Push realtime qua SignalR đến user cụ thể (dùng user connection group)

- [ ] **Tích hợp gửi notification tại các sự kiện:**
  - Sau khi thanh toán thành công → "Đặt vé thành công"
  - Trước sự kiện 24h → "Nhắc nhở sự kiện sắp diễn ra" (Quartz job)
  - Khi refund được duyệt/từ chối
  - Khi Organizer bị approve/reject

- [ ] **SignalR:** Cập nhật `EventHub` để hỗ trợ gửi notification đến user cụ thể (theo userId group)

---

## 10. 📊 MODULE REPORTS (Báo cáo)

> **Mục đích:** Organizer xem doanh thu sự kiện, Admin xem toàn hệ thống.

- [ ] **API:** `GET /api/reports/organizer/revenue` — Doanh thu theo khoảng thời gian (Organizer)
  - Lọc theo: `fromDate`, `toDate`, `eventId?`
  - Trả về: tổng doanh thu, số vé bán, breakdown theo sự kiện

- [ ] **API:** `GET /api/reports/organizer/events/{eventId}/summary` — Tóm tắt một sự kiện
  - Tổng vé: bán / đặt chỗ / còn lại
  - Doanh thu theo loại vé
  - Số lượt check-in
  - Biểu đồ bán vé theo ngày

- [ ] **API:** `GET /api/reports/admin/overview` — Tổng quan hệ thống (Admin)
  - Tổng sự kiện, user, doanh thu, vé bán ra

- [ ] **API:** `GET /api/reports/admin/revenue` — Doanh thu toàn hệ thống theo khoảng thời gian

- [ ] **API:** `GET /api/reports/organizer/events/{eventId}/checkin` — Thống kê check-in

---

## 11. 🛡️ MODULE ADMIN (Quản trị hệ thống)

> **Mục đích:** Admin quản lý tập trung users, sự kiện, nội dung.

- [ ] **API:** `GET /api/admin/users` — Danh sách tất cả users (Admin, có lọc theo status/role)

- [ ] **API:** `GET /api/admin/users/{id}` — Chi tiết user

- [ ] **API:** `PATCH /api/admin/users/{id}/ban` — Ban user
- [ ] **API:** `PATCH /api/admin/users/{id}/activate` — Kích hoạt lại user

- [ ] **API:** `GET /api/admin/events` — Danh sách tất cả sự kiện (Admin)

- [ ] **API:** `PATCH /api/admin/events/{id}/cancel` — Admin huỷ sự kiện (vi phạm chính sách)
  - Trigger hoàn tiền tự động cho tất cả order đã thanh toán

- [ ] **API:** `GET /api/admin/payments` — Danh sách giao dịch thanh toán toàn hệ thống

- [ ] **API:** `GET /api/admin/audit-logs` — Xem nhật ký hành động hệ thống

---

## 12. 🏗️ INFRASTRUCTURE CÒN THIẾU

### 12.1 QR Code (`Infrastructure/QR/`)
- [ ] Implement `IQrCodeService`
- [ ] Dùng thư viện `QRCoder` hoặc `ZXing.Net`
- [ ] Method: `GenerateQrCodePng(string content) → byte[]`

### 12.2 PDF Generator (`Infrastructure/Pdf/`)
- [ ] Implement `IPdfService`
- [ ] Dùng thư viện `QuestPDF` (khuyến nghị) hoặc `iText7`
- [ ] Method: `GenerateTicketPdf(TicketPdfModel model) → byte[]`
- [ ] Template vé: logo, tên sự kiện, thời gian, địa điểm, tên người dùng, loại vé, số ghế, QR code

### 12.3 Payment Gateway (`Infrastructure/Payment/`)
- [ ] Implement `IPaymentGateway` interface với các method:
  - `CreatePaymentUrl(order) → string`
  - `VerifyCallback(queryParams) → PaymentResult`
- [ ] Implement `VNPayGateway : IPaymentGateway`
  - Sinh URL thanh toán theo chuẩn VNPay
  - Verify HMAC signature callback

### 12.4 File Storage (`Infrastructure/Storage/`)
- [ ] Implement `IStorageService`
- [ ] Hiện tại đang lưu file vào `wwwroot/uploads/` — phù hợp dev
- [ ] Cân nhắc: local disk vs Azure Blob vs AWS S3 cho production

---

## 13. 🌐 CÁC TÍNH NĂNG CHƯA HOÀN CHỈNH TRONG MODULE ĐÃ CÓ

### Category
- [ ] `GET /api/category/{id}` — GetCategoryByIdAsync đang `NotImplementedException`
- [ ] `DELETE /api/category/{id}` — DeleteCategoryAsync đang `NotImplementedException`
- [ ] `UpdateCategoryAsync` — đang dùng `SaveChanges()` đồng bộ (cần async)
- [ ] `CreateCategoryAsync` — không kiểm tra user có role Admin không (chỉ check user tồn tại)

### Event
- [ ] `DELETE /api/events/{id}` — DeleteEventAsync đang `NotImplementedException`
- [ ] `GET /api/events/featured` — GetEventsByFeatureAsync đang `NotImplementedException`
- [ ] Thêm filter `?city=` trong `GetEventsAsync`
- [ ] Thêm field `CategoryName`, `VenueAddress` vào `EventResponse` cho phía Web hiển thị

### Venue
- [ ] `DELETE /api/venue/{id}` — DeleteVenueAsync đang `NotImplementedException`
- [ ] Kiểm tra: không cho xoá venue nếu có sự kiện sắp diễn ra
- [ ] Fix `GetVenueByIdAsync` — thiếu `Include(CreatedByNavigation)` (BUG-12)

### User
- [ ] Xem lịch sử vé đã mua: `GET /api/user/tickets`
- [ ] Xem lịch sử đơn hàng: `GET /api/user/orders`
- [ ] `UserEventInteraction` — chưa có API like/save/share sự kiện

---

## 14. 🎨 EVENTIX.WEB — GIAO DIỆN CÒN THIẾU

> Hiện tại Web chỉ có: Auth, Home (danh sách event), Event Detail (xem), Organizer Dashboard, Event Wizard.

- [ ] **Trang đặt vé** — Chọn loại vé, chọn ghế, hiện sơ đồ chỗ ngồi tương tác
- [ ] **Trang thanh toán** — Xem tóm tắt order, nhập coupon, chọn gateway, redirect
- [ ] **Trang vé của tôi** — Danh sách vé, xem QR, download PDF
- [ ] **Trang lịch sử đơn hàng** — Danh sách order, chi tiết, nút yêu cầu hoàn tiền
- [ ] **Trang thông báo** — Danh sách notification, badge unread trên header
- [ ] **Trang Admin**
  - Quản lý user (ban/activate)
  - Duyệt/từ chối Organizer (hiện có nhưng chỉ qua API)
  - Quản lý sự kiện toàn hệ thống
  - Xem báo cáo doanh thu
- [ ] **Trang Organizer — Báo cáo** — Doanh thu, check-in realtime của sự kiện
- [ ] **Check-in App** — Giao diện quét QR cho nhân viên tại cổng

---

## THỨ TỰ TRIỂN KHAI ĐỀ XUẤT

```
Giai đoạn 1 — Core booking flow (quan trọng nhất):
  Fix Bugs → Reservation → Order → Payment → Ticket (phát hành) → CheckIn

Giai đoạn 2 — Trải nghiệm người dùng:
  Coupon → Notification → Refund → Reports

Giai đoạn 3 — Quản trị & hoàn thiện:
  Admin module → Web UI còn thiếu → Category/Event/Venue cleanup

Giai đoạn 4 — Nâng cao (nếu có thời gian):
  UserEventInteraction (like/save) → AI tagging → Review & Rating
```

---

## Thống kê

| Loại | Số lượng |
|---|---|
| Fix bugs cần làm trước | 6 |
| API endpoints mới cần tạo | ~45 |
| Infrastructure cần implement | 4 |
| Tính năng web UI cần tạo | ~10 |
| Method NotImplemented cần hoàn thiện | 8 |
| **Tổng việc cần làm** | **~73** |
