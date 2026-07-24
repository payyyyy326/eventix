# 03. ACTOR VÀ USE CASE EVENTIX

## 1. Mục đích

Tài liệu xác định tác nhân tương tác với Eventix, quyền hạn, use case chính, điều kiện
trước/sau và quan hệ giữa các use case. Phạm vi phản ánh source code hiện tại.

## 2. Danh sách actor

### 2.1 Actor con người

| Actor | Mô tả | Cách xác thực |
|---|---|---|
| Guest | Người chưa đăng nhập | Không có JWT |
| Customer/User | Người dùng đã có tài khoản | JWT, role User/Customer tùy seed |
| Organizer | Người tổ chức đã được cấp role/quyền | JWT + role Organizer + ownership |
| Admin | Quản trị hệ thống | JWT + role Admin |
| Check-in Staff | Người quét vé cho sự kiện | Hiện dùng tài khoản Organizer/Admin |

`User` và `Customer` cùng tồn tại trong constants. Trong tài liệu này, “Customer”
đại diện người dùng mua vé; khi triển khai cần thống nhất một role chính để tránh
policy không đồng nhất.

### 2.2 Actor hệ thống/ngoại vi

| Actor | Vai trò |
|---|---|
| Eventix.Web | MVC client gọi REST API thay mặt trình duyệt |
| Gmail SMTP | Gửi OTP và email vòng đời đặt vé |
| Quartz Scheduler | Kích hoạt EventStatusJob và BookingExpirationJob mỗi phút |
| SQL Server | Lưu trạng thái giao dịch và áp constraint |
| Payment Demo | Mô phỏng cổng thanh toán thành công |
| File System | Lưu avatar/banner/image trong `wwwroot/uploads` |

## 3. Sơ đồ actor và use case tổng quát

```mermaid
flowchart LR
    G[Guest]
    C[Customer]
    O[Organizer]
    A[Admin]
    S[Check-in Staff]
    Q[Quartz]
    M[SMTP]

    subgraph Eventix
        UC1((Xem/tìm sự kiện))
        UC2((Đăng ký/đăng nhập))
        UC3((Quản lý hồ sơ))
        UC4((Giữ vé/chọn ghế))
        UC5((Thanh toán))
        UC6((Xem vé QR))
        UC7((Hủy giữ vé))
        UC8((Đăng ký Organizer))
        UC9((Quản lý venue/sơ đồ ghế))
        UC10((Tạo/publish sự kiện))
        UC11((Quản lý hạng vé))
        UC12((Check-in))
        UC13((Duyệt Organizer))
        UC14((Tự động hết hạn))
        UC15((Gửi email))
    end

    G --> UC1
    G --> UC2
    C --> UC1
    C --> UC3
    C --> UC4
    C --> UC5
    C --> UC6
    C --> UC7
    C --> UC8
    O --> UC9
    O --> UC10
    O --> UC11
    O --> UC12
    A --> UC13
    A --> UC12
    Q --> UC14
    UC2 --> UC15
    UC4 --> UC15
    UC5 --> UC15
    UC7 --> UC15
    UC14 --> UC15
    M --- UC15
```

## 4. Ma trận quyền use case

Ký hiệu: `✓` được thực hiện, `R` chỉ đọc/public, `Own` chỉ dữ liệu thuộc quyền,
`—` không được phép.

| Use case | Guest | Customer | Organizer | Admin |
|---|:---:|:---:|:---:|:---:|
| Xem danh sách/chi tiết event | R | R | R | R |
| Đăng ký, OTP, login, reset password | ✓ | ✓ | ✓ | ✓ |
| Quản lý hồ sơ cá nhân | — | Own | Own | Own |
| Đăng ký hồ sơ Organizer | — | ✓ | — | — |
| Tạo/sửa venue | — | — | Own | Quản trị |
| Tạo/sửa/publish event | — | — | Own | Quản trị |
| Quản lý zone/seat/ticket type | — | — | Own | Quản trị |
| Giữ vé/chọn ghế | — | ✓ | ✓* | ✓* |
| Tạo order/thanh toán | — | Own | Own* | Own* |
| Xem/hủy booking của mình | — | Own | Own | Own |
| Xem vé của mình | — | Own | Own | Own |
| Quét QR sự kiện | — | — | Own event | ✓ |
| Duyệt/từ chối Organizer | — | — | — | ✓ |

`*` Organizer/Admin vẫn có thể là người mua nếu API chỉ yêu cầu authenticated user;
vai trò mua vé nên được thống nhất bằng policy nếu nghiệp vụ muốn giới hạn.

## 5. Đặc tả use case Guest/Auth

### UC-A01: Xem và tìm kiếm sự kiện

- **Actor:** Guest, mọi tài khoản.
- **Tiền điều kiện:** Không.
- **Luồng chính:** Nhập từ khóa/bộ lọc → API lấy event public → hiển thị danh sách →
  mở chi tiết.
- **Hậu điều kiện:** Không thay đổi dữ liệu nghiệp vụ; view count có thể được cập nhật.
- **Ngoại lệ:** Event không tồn tại hoặc không public.

### UC-A02: Đăng ký tài khoản

- **Actor:** Guest.
- **Tiền điều kiện:** Email chưa tồn tại.
- **Luồng chính:** Nhập thông tin → hash password → tạo user/OTP → gửi email → nhập
  OTP → xác thực email.
- **Hậu điều kiện:** User được kích hoạt/xác thực và có role mặc định.
- **Ngoại lệ:** Email trùng, OTP sai/hết hạn, SMTP lỗi.

### UC-A03: Đăng nhập

- **Actor:** Guest.
- **Tiền điều kiện:** Tài khoản tồn tại, hợp lệ, đã xác thực theo rule AuthService.
- **Luồng chính:** Kiểm tra password → tạo access/refresh token → Web lưu cookie.
- **Hậu điều kiện:** Request sau mang JWT.
- **Ngoại lệ:** Sai thông tin, tài khoản bị khóa, token hết hạn.

### UC-A04: Quên/đặt lại mật khẩu

- **Actor:** Guest/User.
- **Luồng:** Yêu cầu OTP → email OTP → xác minh → đặt password mới.
- **Bảo mật:** OTP có purpose riêng và thời hạn.

## 6. Đặc tả use case Customer

### UC-C01: Quản lý hồ sơ

- Xem/cập nhật FullName, PhoneNumber và thông tin cá nhân.
- Upload avatar vào vùng file cho phép.
- Đổi mật khẩu sau khi xác minh mật khẩu hiện tại.

### UC-C02: Chọn và giữ vé đứng

- **Tiền điều kiện:** Đã đăng nhập; event/ticket type đang bán; quota đủ.
- **Luồng:** Chọn loại “Vé đứng” → chọn zone → chọn hạng vé → nhập quantity → gửi
  booking → server tạo reservation Active 15 phút.
- **Hậu điều kiện:** ReservedQuantity tăng; email giữ vé được gửi.

### UC-C03: Chọn và giữ vé ngồi

- **Tiền điều kiện:** Như UC-C02; zone `HasSeats = true`.
- **Luồng:** Chọn loại “Vé ngồi” → zone → ticket type → xem seat map → chọn tối đa
  số ghế cho phép → server kiểm tra lại trạng thái → giữ từng ghế.
- **Hậu điều kiện:** EventSeatStatus `Available → Reserved`; một reservation/ghế.
- **Ngoại lệ:** Ghế vừa bị người khác giữ; trả Conflict và tải lại sơ đồ.

### UC-C04: Xem và hủy lượt giữ

- **Tiền điều kiện:** Reservation thuộc user và còn Active.
- **Luồng:** Mở “Vé đang giữ” → chọn hủy → trả quota/ghế → hủy order Pending liên quan.
- **Hậu điều kiện:** Reservation Cancelled; email hủy được gửi.

### UC-C05: Tạo đơn hàng

- Chọn một hoặc nhiều reservation Active còn hạn.
- Hệ thống tạo Order Pending, OrderItem snapshot và gắn Reservation.OrderId.
- Tổng tiền gồm subtotal và service fee hiện tại.

### UC-C06: Thanh toán và nhận vé QR

- **Tiền điều kiện:** Order Pending, chưa hết hạn; toàn bộ reservation Active.
- **Luồng:** Xác nhận thanh toán Demo → cập nhật inventory → phát hành Ticket → tạo
  Payment Success → gửi email có QR.
- **Hậu điều kiện:** Order Paid, Reservation Confirmed, Seat Sold, Ticket Active.
- **Ngoại lệ:** Order hết hạn/đã xử lý, reservation không còn Active.

### UC-C07: Xem vé của tôi

- Xem danh sách ticket đã phát hành.
- Mở chi tiết ticket để hiển thị QR từ QrToken.
- TicketCode và QR là định danh duy nhất.

### UC-C08: Booking tự hết hạn

- **Actor khởi tạo:** Quartz, không phải Customer.
- Reservation quá 15 phút chưa thanh toán → Expired → trả quota/ghế → email đặt vé
  thất bại.

## 7. Đặc tả use case Organizer

### UC-O01: Đăng ký hồ sơ Organizer

- **Actor:** Customer.
- **Luồng:** Gửi thông tin doanh nghiệp/cá nhân → OrganizerProfile Pending → chờ Admin.
- **Hậu điều kiện:** Chỉ profile Approved mới nên được phép vận hành event.

### UC-O02: Quản lý venue

- Tạo/sửa venue thuộc quyền.
- Khai báo capacity và thông tin địa điểm.
- Xem/cập nhật seat map.

### UC-O03: Quản lý zone và section layout

- Tạo zone đứng/ngồi.
- Ánh xạ section với zone.
- Kiểm tra capacity và trạng thái import ghế.

### UC-O04: Quản lý ghế

- Xem ghế theo venue/section.
- Download template/import Excel.
- Generate ghế theo hàng, số, tọa độ.
- Đảm bảo không trùng `(Venue, Section, Row, Number)`.

### UC-O05: Tạo sự kiện bằng Event Wizard

Các bước thực tế gồm thông tin cơ bản, venue, zone/seat, ticket type, ảnh và xác nhận.
Wizard lưu trạng thái tạm qua web/session/form và gọi API ở các bước tương ứng.

### UC-O06: Publish sự kiện

- **Tiền điều kiện:** Event thuộc organizer, dữ liệu bắt buộc hợp lệ, có ticket type.
- **Luồng:** Validate → đổi Draft sang Published/OnSale theo nghiệp vụ.
- **Hậu điều kiện:** Event xuất hiện với khách hàng khi đủ điều kiện public.

### UC-O07: Quản lý ticket type

- Tạo/sửa/deactivate hạng vé.
- Khai báo giá, quota, sale window, zone/section, yêu cầu ghế.
- Không được làm quota nhỏ hơn số đã bán/đang giữ.

### UC-O08: Check-in và xem thống kê

- Chọn event thuộc quyền.
- Quét/nhập QrToken.
- Ticket Active → Used và tạo CheckInLog.
- Xem tổng vé, đã check-in và còn lại.

## 8. Đặc tả use case Admin

### UC-AD01: Duyệt Organizer

- Xem hồ sơ Pending.
- Approve hoặc Reject.
- Khi approve, hệ thống cần bảo đảm role Organizer được gán nhất quán.

### UC-AD02: Quản trị dữ liệu nền

- Quản lý category, user, venue/event khi có endpoint tương ứng.
- Can thiệp trạng thái sai phạm theo policy.
- Phần admin tập trung hiện chưa đầy đủ, cần xem là phạm vi mở rộng.

### UC-AD03: Check-in hỗ trợ

Admin có thể check-in cho mọi event theo logic `isAdmin`; Organizer chỉ event thuộc quyền.

## 9. Quan hệ include/extend

```mermaid
flowchart TD
    B[Giữ vé] -->|include| V[Kiểm tra quota]
    B -->|include với vé ngồi| S[Kiểm tra và khóa ghế]
    B -->|include| E1[Gửi email giữ vé]
    O[Tạo order] -->|include| R[Kiểm tra reservation còn hạn]
    P[Thanh toán] -->|include| O
    P -->|include| T[Phát hành ticket QR]
    P -->|include| E2[Gửi email thành công]
    X[Job hết hạn] -->|extend khi quá ExpiresAt| B
    X --> E3[Gửi email thất bại]
    C[Hủy booking] --> E4[Gửi email hủy]
    CI[Check-in] -->|include| QV[Xác minh QR và quyền event]
```

## 10. Quy tắc authorization cần duy trì

1. Luôn lấy UserId từ JWT claim, không nhận UserId tùy ý từ client.
2. Booking/order/ticket chỉ truy cập dữ liệu có UserId trùng claim.
3. Organizer phải được kiểm tra ownership của event/venue.
4. Admin bypass ownership chỉ ở use case được định nghĩa rõ.
5. API public chỉ gồm danh sách/chi tiết event, category và dữ liệu thực sự công khai.
6. Phân biệt 401 (chưa xác thực/token lỗi) và 403 (đã xác thực nhưng thiếu quyền).

## 11. Use case chưa hoàn thiện

Các use case sau mới có entity hoặc tài liệu dự kiến, chưa nên demo như chức năng hoàn chỉnh:

- Áp coupon tại checkout.
- Hoàn tiền.
- Review/rating.
- Notification center.
- AI tagging/gợi ý.
- Giỏ hàng nhiều event.
- Payment gateway và webhook thật.

## 12. Kịch bản demo đề xuất

1. Guest đăng ký và nhập OTP.
2. Customer mở event, chọn zone ngồi và hai ghế.
3. Nhận email giữ vé.
4. Thanh toán Demo, nhận email có hai QR.
5. Organizer quét một QR: thành công; quét lại: bị từ chối.
6. Tạo lượt giữ khác nhưng không thanh toán; sau 15–16 phút nhận email thất bại và
   thấy ghế trở lại Available.
7. Tạo lượt giữ mới và hủy thủ công; nhận email hủy.