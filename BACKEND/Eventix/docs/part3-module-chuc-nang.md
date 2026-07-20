# TÀI LIỆU ĐẶC TẢ HỆ THỐNG EVENTIX
## Phần 3: Các Module Chức Năng & API

---

## 1. Tổng Quan API

- **Base URL:** `https://localhost:{port}/api`
- **Authentication:** JWT Bearer Token (header `Authorization: Bearer <token>`)
- **Response format:** Tất cả response đều dùng `ApiResponseModel<T>`:

```json
{
  "code": "SUCCESS_CODE",
  "message": "Mô tả kết quả",
  "data": { ... }
}
```

- **Pagination:** Các endpoint danh sách hỗ trợ `PaginationRequest` với `pageIndex`, `pageSize`.
- **Swagger UI:** Truy cập `/swagger` khi chạy môi trường Development.

---

## 2. Module Authentication (`/api/auth`)

### Mục đích
Xác thực người dùng: đăng ký, đăng nhập, xác thực email OTP, refresh token, quên mật khẩu.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Đăng ký tài khoản mới |
| POST | `/api/auth/verify-otp` | Anonymous | Xác thực email bằng OTP |
| POST | `/api/auth/resend-otp` | Anonymous | Gửi lại OTP |
| POST | `/api/auth/login` | Anonymous | Đăng nhập, trả về JWT + Refresh Token |
| POST | `/api/auth/refresh-token` | Anonymous | Làm mới JWT bằng Refresh Token |
| POST | `/api/auth/logout` | Authorized | Đăng xuất, thu hồi Refresh Token |
| POST | `/api/auth/forgot-password` | Anonymous | Gửi OTP đặt lại mật khẩu qua email |
| POST | `/api/auth/reset-password` | Anonymous | Đặt lại mật khẩu bằng OTP |

### Request/Response mẫu

**Register:**
```json
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "Password123",
  "confirmPassword": "Password123",
  "fullName": "Nguyen Van A",
  "phoneNumber": "0901234567"
}
```

**Login response:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "expiresAt": "2026-07-19T22:00:00Z",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "fullName": "Nguyen Van A",
    "roles": ["Customer"]
  }
}
```

### Business Logic
- Mật khẩu phải ≥ 6 ký tự, băm bằng BCrypt.
- Email phải unique. Số điện thoại phải unique.
- Sau đăng ký, OTP 6 số được gửi qua email (MailKit). OTP có thời hạn.
- Người dùng chưa xác thực email **không thể đăng nhập**.
- Refresh Token được lưu trong `UserRefreshToken`, có thể bị thu hồi khi logout.

---

## 3. Module User (`/api/user`)

### Mục đích
Quản lý thông tin cá nhân của người dùng.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/user/profile` | Authorized | Lấy thông tin hồ sơ cá nhân |
| PUT | `/api/user/profile` | Authorized | Cập nhật hồ sơ (tên, phone) |
| PUT | `/api/user/avatar` | Authorized | Upload ảnh đại diện (multipart/form-data) |
| POST | `/api/user/change-password` | Authorized | Đổi mật khẩu |

### Business Logic
- Avatar được upload và lưu vào `wwwroot/uploads/avatars/`.
- Đổi mật khẩu yêu cầu xác nhận mật khẩu hiện tại.

---

## 4. Module Event (`/api/events`)

### Mục đích
CRUD sự kiện, tìm kiếm, lọc, xem chi tiết, publish sự kiện.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/events` | Anonymous | Lấy danh sách sự kiện (có lọc) |
| GET | `/api/events/{id}` | Anonymous | Lấy chi tiết sự kiện |
| GET | `/api/events/{id}/booking` | Anonymous | Lấy thông tin sự kiện để đặt vé |
| POST | `/api/events/create` | Organizer | Tạo sự kiện mới |
| PUT | `/api/events/{id}` | Authorized | Cập nhật sự kiện |
| PUT | `/api/events/{id}/upload-banner` | Authorized | Upload banner |
| PUT | `/api/events/{id}/upload-image` | Authorized | Upload ảnh thumbnail |
| POST | `/api/events/{id}/publish` | Authorized | Publish sự kiện |

### Filter parameters (`GET /api/events`)
```
?keyword=     Tìm theo tên
?categoryId=  Lọc theo danh mục
?status=      Lọc theo trạng thái
?city=        Lọc theo thành phố
?fromDate=    Từ ngày
?toDate=      Đến ngày
?pageIndex=1&pageSize=10
```

### Business Logic tạo sự kiện
1. Organizer phải có `OrganizerProfile` với status = `Approved`.
2. Venue phải tồn tại, Category phải tồn tại.
3. Kiểm tra conflict thời gian: cùng Venue không thể có 2 sự kiện chồng nhau.
4. Slug tự động sinh từ Title + unique suffix.
5. Nếu `PublishedAt` được đặt, EventStatusJob sẽ tự động chuyển `Draft → Published`.

---

## 5. Module Organizer (`/api/OrganizerProfile`)

### Mục đích
Quản lý hồ sơ Organizer, quản lý sự kiện của Organizer, Admin duyệt/từ chối Organizer.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/OrganizerProfile/organizer-profiles` | Authorized | Danh sách tất cả Organizer (Admin) |
| GET | `/api/OrganizerProfile/organizer-detail` | Authorized | Hồ sơ Organizer của user hiện tại |
| POST | `/api/OrganizerProfile/create` | Authorized | Tạo hồ sơ Organizer mới |
| GET | `/api/OrganizerProfile/events` | Authorized | Danh sách sự kiện của Organizer |
| GET | `/api/OrganizerProfile/events/{id}` | Authorized | Chi tiết sự kiện của Organizer |
| GET | `/api/OrganizerProfile/events/{eventId}/ticket-types` | Authorized | Danh sách loại vé của sự kiện |
| POST | `/api/OrganizerProfile/events/{eventId}/ticket-types` | Authorized | Tạo loại vé mới cho sự kiện |
| GET | `/api/OrganizerProfile/ticket-types/{id}` | Authorized | Chi tiết loại vé |
| PUT | `/api/OrganizerProfile/ticket-types/{id}` | Authorized | Cập nhật loại vé |
| PATCH | `/api/OrganizerProfile/ticket-types/{id}/deactivate` | Authorized | Vô hiệu hóa loại vé |
| GET | `/api/OrganizerProfile/events/{eventId}/sections` | Authorized | Danh sách Section của sự kiện |
| PATCH | `/api/OrganizerProfile/{id}/approve` | Admin | Duyệt Organizer |
| PATCH | `/api/OrganizerProfile/{id}/reject` | Admin | Từ chối Organizer |

### Business Logic
- Mỗi User chỉ có một `OrganizerProfile`.
- Organizer mới có status `Pending`, chờ Admin duyệt.
- Organizer bị `Rejected` hoặc `Suspended` không thể tạo sự kiện.

---

## 6. Module Venue (`/api/venue`)

### Mục đích
Quản lý địa điểm, bao gồm cả sơ đồ chỗ ngồi (seat map).

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/venue/venues` | Anonymous | Danh sách tất cả Venue |
| GET | `/api/venue/{id}` | Authorized | Chi tiết Venue |
| POST | `/api/venue/create` | Authorized | Tạo Venue mới |
| PUT | `/api/venue/{id}` | Authorized | Cập nhật Venue |
| GET | `/api/venue/{venueId}/seat-map` | Authorized | Lấy sơ đồ chỗ ngồi (SVG layout) |
| PUT | `/api/venue/{venueId}/seat-map` | Authorized | Lưu sơ đồ chỗ ngồi |

---

## 7. Module VenueZone (`/api/VenueZone`)

### Mục đích
Quản lý các khu vực (zone) trong địa điểm và trạng thái import ghế.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/VenueZone/venue/{venueId}` | Authorized | Danh sách zone theo Venue |
| POST | `/api/VenueZone/venue/{venueId}` | Authorized | Tạo zone mới |
| PUT | `/api/VenueZone/{zoneId}` | Authorized | Cập nhật zone |
| GET | `/api/VenueZone/venue/{venueId}/seat-import-status` | Authorized | Kiểm tra trạng thái import ghế theo zone |

---

## 8. Module Seat (`/api/Seat`)

### Mục đích
Quản lý ghế ngồi: import từ Excel, tự động generate, xem theo Venue.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/Seat/venue/{venueId}` | Authorized | Danh sách ghế theo Venue |
| GET | `/api/Seat/template` | Authorized | Tải file Excel template import ghế |
| POST | `/api/Seat/{venueId}/import-excel` | Authorized | Import ghế từ file Excel (multipart) |
| GET | `/api/Seat/venue/{venueId}/sections` | Authorized | Danh sách Section trong Venue |
| POST | `/api/Seat/venue/{venueId}/generate` | Authorized | Tự động generate ghế theo cấu hình |

### Import Excel Format

File Excel template có các cột:

| Cột | Tên | Mô tả |
|---|---|---|
| 0 | Section | Tên khu vực |
| 1 | StartRow | Hàng bắt đầu (A, B, ...) |
| 2 | EndRow | Hàng kết thúc |
| 3 | StartNumber | Số ghế bắt đầu |
| 4 | EndNumber | Số ghế kết thúc |
| 5 | StartX | Tọa độ X bắt đầu |
| 6 | StartY | Tọa độ Y bắt đầu |
| 7 | GapX | Khoảng cách X giữa các ghế |
| 8 | GapY | Khoảng cách Y giữa các hàng |

---

## 9. Module TicketType (`/api/TicketType`)

### Mục đích
Quản lý các loại vé của sự kiện.

### API Endpoints

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/TicketType?eventId={id}` | Anonymous | Danh sách loại vé theo sự kiện |
| GET | `/api/TicketType/{id}` | Anonymous | Chi tiết loại vé |
| POST | `/api/TicketType/event/{eventId}` | Authorized | Tạo loại vé mới |
| PUT | `/api/TicketType/update/{id}` | Authorized | Cập nhật loại vé |

---

## 10. Module Category (`/api/Category`)

### Mục đích
Quản lý danh mục sự kiện (Admin).

### API Endpoints (suy luận từ service)

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/Category` | Anonymous | Danh sách danh mục |
| GET | `/api/Category/{id}` | Anonymous | Chi tiết danh mục |
| POST | `/api/Category` | Admin | Tạo danh mục mới |
| PUT | `/api/Category/{id}` | Admin | Cập nhật danh mục |

---

## 11. Event Wizard (Eventix.Web MVC)

Giao diện tạo sự kiện nhiều bước dành cho Organizer, chạy tại Eventix.Web.

### Các bước (Steps)

| Bước | URL | Nội dung |
|---|---|---|
| Step 1 | `/EventWizard/Step1` | Thông tin sự kiện cơ bản (tên, mô tả, thời gian, danh mục) |
| Step 2 | `/EventWizard/Step2` | Chọn Venue |
| Step 3 | `/EventWizard/Step3` | Cấu hình Zone của Venue |
| Step 4 | `/EventWizard/Step4` | Quản lý ghế (import Excel hoặc generate) |
| Step 5 | `/EventWizard/Step5` | Tạo loại vé (TicketType) |
| Step 6 | `/EventWizard/Step6` | Gán TicketType vào Zone/Seat |
| Step 7 | `/EventWizard/Step7` | Review toàn bộ và Submit tạo sự kiện |

### Cơ chế lưu trạng thái wizard
- Dữ liệu mỗi bước được lưu tạm vào `HttpContext.Session` dưới dạng JSON.
- Key session: `EventWizard_Info`, `EventWizard_Venue`, `EventWizard_Zones`...
- Bước cuối (Step 7) gọi API để tạo sự kiện thực tế.

---

## 12. Các Module Phụ (Đã scaffold, chờ implement)

Các module sau đã có cấu trúc thư mục (Controllers/Services/Interfaces/DTOs) nhưng chưa có API public:

| Module | Đường dẫn | Chức năng dự kiến |
|---|---|---|
| `Orders` | `/api/Orders` | Tạo đơn hàng, xem lịch sử đơn |
| `Payments` | `/api/Payments` | Khởi tạo thanh toán, webhook callback |
| `Tickets` | `/api/Tickets` | Xem vé, download vé PDF |
| `Reservations` | `/api/Reservations` | Giữ chỗ tạm thời |
| `CheckIn` | `/api/CheckIn` | Quét QR check-in |
| `Refunds` | `/api/Refunds` | Yêu cầu và xử lý hoàn tiền |
| `Coupons` | `/api/Coupons` | Áp dụng mã giảm giá |
| `Notifications` | `/api/Notifications` | Lấy thông báo của user |
| `Reports` | `/api/Reports` | Báo cáo doanh thu cho Organizer/Admin |
| `Admin` | `/api/Admin` | Quản trị hệ thống tập trung |
| `AI` | `/api/AI` | Tính năng AI (gợi ý, tagging...) |

---

## 13. SignalR Hub

**Endpoint:** `wss://{host}/hubs/events`

### Sự kiện broadcast

| Sự kiện | Dữ liệu | Khi nào |
|---|---|---|
| `EventStatusChanged` | `{ eventId, title, oldStatus, newStatus }` | Khi EventStatusJob thay đổi trạng thái sự kiện |

### Cách kết nối (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/events")
    .build();

connection.on("EventStatusChanged", (data) => {
    console.log(`Event ${data.title}: ${data.oldStatus} → ${data.newStatus}`);
});

await connection.start();
```

---

*→ Xem tiếp: Part 4 - Luồng nghiệp vụ, bảo mật, hạ tầng*
