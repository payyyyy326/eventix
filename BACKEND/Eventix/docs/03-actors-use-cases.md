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

## 3. Use Case Diagram toàn bộ dự án

### 3.1 Phạm vi hệ thống và actor

Các actor trực tiếp của Eventix gồm:

- **Guest:** xem sự kiện, đăng ký, đăng nhập và đặt lại mật khẩu.
- **Customer:** đặt vé, thanh toán, quản lý lượt giữ, đơn hàng và vé QR.
- **Organizer:** quản lý hồ sơ tổ chức, địa điểm, sự kiện, khu vé, sơ đồ và check-in.
- **Check-in Staff:** thực hiện check-in dưới quyền của Organizer/Admin.
- **Admin:** duyệt Organizer và quản trị dữ liệu nền.
- **Email Service / Gmail SMTP:** hệ thống ngoài nhận yêu cầu gửi OTP và thông báo vòng đời vé.
- **Quartz Scheduler:** hệ thống ngoài kích hoạt hết hạn giữ vé và cập nhật trạng thái sự kiện.
- **Payment Demo / Payment Gateway:** xử lý yêu cầu thanh toán của Eventix.

SQL Server và File System là hạ tầng nội bộ, không phải actor nghiệp vụ trong Use Case Diagram.

### 3.2 Sơ đồ tổng quan toàn hệ thống

```mermaid
flowchart LR
    Guest["👤 Guest"]
    Customer["👤 Customer"]
    Organizer["👤 Organizer"]
    Staff["👤 Check-in Staff"]
    Admin["👤 Admin"]
    Mail["✉️ Email Service"]
    Scheduler["⏱ Quartz Scheduler"]
    PaymentGateway["💳 Payment Demo / Gateway"]

    subgraph Eventix["HỆ THỐNG EVENTIX"]
        direction TB
        Auth(["Xác thực tài khoản"])
        Discover(["Khám phá sự kiện"])
        Profile(["Quản lý hồ sơ"])
        Booking(["Đặt và giữ vé"])
        Orders(["Quản lý đơn hàng"])
        Payment(["Thanh toán"])
        Tickets(["Quản lý vé QR"])
        OrganizerProfile(["Đăng ký / quản lý Organizer"])
        Venue(["Quản lý địa điểm và sơ đồ"])
        Events(["Tạo / chỉnh sửa / công bố sự kiện"])
        TicketTypes(["Quản lý khu và loại vé"])
        CheckIn(["Check-in vé"])
        AdminData(["Quản trị hệ thống"])
        Expiration(["Tự động cập nhật / hết hạn"])
        Notification(["Gửi thông báo email"])
    end

    Guest --> Auth
    Guest --> Discover
    Customer --> Discover
    Customer --> Profile
    Customer --> Booking
    Customer --> Orders
    Customer --> Payment
    Customer --> Tickets
    Customer --> OrganizerProfile
    Organizer --> Profile
    Organizer --> OrganizerProfile
    Organizer --> Venue
    Organizer --> Events
    Organizer --> TicketTypes
    Organizer --> CheckIn
    Staff --> CheckIn
    Admin --> AdminData
    Admin --> CheckIn
    Scheduler --> Expiration
    PaymentGateway --- Payment
    Mail --- Notification

    Auth -.->|"<<include>>"| Notification
    Booking -.->|"<<include>>"| Notification
    Payment -.->|"<<include>>"| Notification
    Expiration -.->|"<<include>>"| Notification
```

### 3.3 Xác thực và hồ sơ người dùng

```mermaid
flowchart LR
    Guest["👤 Guest"]
    User["👤 Customer / Organizer / Admin"]
    Mail["✉️ Email Service"]

    subgraph AuthModule["AUTH & USER MODULE"]
        Register(["Đăng ký tài khoản"])
        VerifyOtp(["Xác minh OTP"])
        ResendOtp(["Gửi lại OTP"])
        Login(["Đăng nhập"])
        Refresh(["Làm mới access token"])
        Forgot(["Quên mật khẩu"])
        Reset(["Đặt lại mật khẩu"])
        ViewProfile(["Xem hồ sơ"])
        UpdateProfile(["Cập nhật hồ sơ"])
        ChangePassword(["Đổi mật khẩu"])
        UploadAvatar(["Tải ảnh đại diện"])
        SendEmail(["Gửi email OTP"])
    end

    Guest --> Register
    Guest --> VerifyOtp
    Guest --> ResendOtp
    Guest --> Login
    Guest --> Forgot
    Guest --> Reset
    User --> Refresh
    User --> ViewProfile
    User --> UpdateProfile
    User --> ChangePassword

    Register -.->|"<<include>>"| SendEmail
    ResendOtp -.->|"<<include>>"| SendEmail
    Forgot -.->|"<<include>>"| SendEmail
    Register -.->|"<<include>>"| VerifyOtp
    Reset -.->|"<<include>>"| VerifyOtp
    UploadAvatar -.->|"<<extend>> khi đổi ảnh"| UpdateProfile
    Mail --- SendEmail
```

### 3.4 Khám phá, đặt vé, thanh toán và vé QR

```mermaid
flowchart LR
    Guest["👤 Guest"]
    Customer["👤 Customer"]
    Scheduler["⏱ Quartz Scheduler"]
    Mail["✉️ Email Service"]
    Gateway["💳 Payment Demo / Gateway"]

    subgraph Commerce["EVENT, BOOKING & COMMERCE MODULE"]
        Browse(["Xem danh sách sự kiện"])
        Search(["Tìm kiếm / lọc sự kiện"])
        Detail(["Xem chi tiết sự kiện"])
        SelectType(["Chọn vé đứng / vé ngồi"])
        SelectZone(["Chọn khu vé"])
        SelectQuantity(["Chọn số lượng"])
        SelectSeat(["Chọn ghế cụ thể"])
        CheckAvailability(["Kiểm tra quota / trạng thái ghế"])
        Hold(["Tạo lượt giữ vé 15 phút"])
        ViewHold(["Xem vé đang giữ"])
        CancelHold(["Hủy lượt giữ vé"])
        Release(["Hoàn quota / giải phóng ghế"])
        CreateOrder(["Tạo đơn hàng"])
        ValidateReservation(["Kiểm tra reservation còn hạn"])
        Pay(["Thanh toán đơn hàng"])
        ProcessPayment(["Xử lý giao dịch"])
        IssueQr(["Phát hành vé QR"])
        ViewOrders(["Xem lịch sử đơn hàng"])
        ViewTickets(["Xem vé của tôi"])
        DownloadTicket(["Lưu ảnh vé QR"])
        Expire(["Hết hạn lượt giữ vé"])
        SendEmail(["Gửi email vòng đời vé"])
    end

    Guest --> Browse
    Guest --> Search
    Guest --> Detail
    Customer --> Browse
    Customer --> Search
    Customer --> Detail
    Customer --> SelectType
    Customer --> ViewHold
    Customer --> CancelHold
    Customer --> CreateOrder
    Customer --> Pay
    Customer --> ViewOrders
    Customer --> ViewTickets
    Scheduler --> Expire
    Mail --- SendEmail
    Gateway --- ProcessPayment

    Search -.->|"<<extend>> khi nhập bộ lọc"| Browse
    Detail -.->|"<<extend>> khi chọn sự kiện"| Browse
    SelectType -.->|"<<include>>"| SelectZone
    SelectQuantity -.->|"<<extend>> vé đứng"| SelectZone
    SelectSeat -.->|"<<extend>> vé ngồi"| SelectZone
    SelectSeat -.->|"<<include>>"| CheckAvailability
    SelectQuantity -.->|"<<include>>"| CheckAvailability
    Hold -.->|"<<include>>"| CheckAvailability
    Hold -.->|"<<include>>"| SendEmail
    SelectZone -.->|"<<include>>"| Hold
    CancelHold -.->|"<<include>>"| Release
    CancelHold -.->|"<<include>>"| SendEmail
    CreateOrder -.->|"<<include>>"| ValidateReservation
    Pay -.->|"<<include>>"| ValidateReservation
    Pay -.->|"<<include>>"| ProcessPayment
    Pay -.->|"<<include>>"| IssueQr
    Pay -.->|"<<include>>"| SendEmail
    DownloadTicket -.->|"<<extend>> khi khách lưu ảnh"| ViewTickets
    Expire -.->|"<<include>>"| Release
    Expire -.->|"<<include>>"| SendEmail
```

### 3.5 Organizer, địa điểm và quản lý sự kiện

```mermaid
flowchart LR
    Customer["👤 Customer"]
    Organizer["👤 Organizer"]
    Admin["👤 Admin"]
    Scheduler["⏱ Quartz Scheduler"]

    subgraph OrganizerModule["ORGANIZER, VENUE & EVENT MODULE"]
        Apply(["Đăng ký hồ sơ Organizer"])
        Review(["Duyệt / từ chối Organizer"])
        ManageOrg(["Cập nhật hồ sơ tổ chức"])
        Dashboard(["Xem dashboard / thống kê"])
        ManageVenue(["Quản lý địa điểm"])
        ConfigureMap(["Thiết kế sơ đồ tổng"])
        ConfigureZone(["Cấu hình khu đứng / khu ngồi"])
        ConfigureSeat(["Sinh / import ghế"])
        CreateEvent(["Tạo sự kiện bằng Wizard"])
        EventInfo(["Nhập thông tin sự kiện"])
        SelectVenue(["Chọn địa điểm"])
        CreateTicketType(["Tạo khu / loại vé"])
        Preview(["Xem trước ghế và sơ đồ"])
        ValidateEvent(["Kiểm tra dữ liệu và trùng lịch"])
        Publish(["Công bố sự kiện"])
        EditEvent(["Chỉnh sửa sự kiện"])
        ManageTickets(["Quản lý loại vé"])
        ViewEventStats(["Xem doanh thu / số vé"])
        UpdateStatus(["Cập nhật trạng thái sự kiện"])
    end

    Customer --> Apply
    Admin --> Review
    Organizer --> ManageOrg
    Organizer --> Dashboard
    Organizer --> ManageVenue
    Organizer --> CreateEvent
    Organizer --> EditEvent
    Organizer --> ManageTickets
    Organizer --> ViewEventStats
    Organizer --> Publish
    Scheduler --> UpdateStatus

    ConfigureMap -.->|"<<extend>> khi thiết kế sơ đồ"| ManageVenue
    ConfigureZone -.->|"<<extend>> khi chia khu"| ConfigureMap
    ConfigureSeat -.->|"<<extend>> với khu ghế ngồi"| ConfigureZone
    CreateEvent -.->|"<<include>>"| EventInfo
    CreateEvent -.->|"<<include>>"| SelectVenue
    CreateEvent -.->|"<<include>>"| CreateTicketType
    CreateEvent -.->|"<<include>>"| Preview
    CreateEvent -.->|"<<include>>"| ValidateEvent
    Publish -.->|"<<extend>> khi đủ điều kiện"| CreateEvent
    EditEvent -.->|"<<include>>"| ValidateEvent
    ManageTickets -.->|"<<extend>> sau khi có sự kiện"| CreateEvent
```

### 3.6 Check-in và quản trị

```mermaid
flowchart LR
    Organizer["👤 Organizer"]
    Staff["👤 Check-in Staff"]
    Admin["👤 Admin"]

    subgraph Operations["CHECK-IN & ADMIN MODULE"]
        ChooseEvent(["Chọn sự kiện check-in"])
        CheckIn(["Check-in vé"])
        UploadQr(["Tải ảnh QR từ máy"])
        ManualCode(["Nhập QR / mã vé thủ công"])
        DecodeQr(["Giải mã QR"])
        VerifyTicket(["Xác minh vé và quyền sự kiện"])
        MarkUsed(["Đánh dấu vé đã sử dụng"])
        Log(["Ghi CheckInLog"])
        Stats(["Xem thống kê check-in"])
        ReviewOrg(["Duyệt / từ chối Organizer"])
        ManageCategory(["Quản lý danh mục"])
        ManageUsers(["Quản lý người dùng"])
        Moderate(["Quản trị sự kiện / dữ liệu"])
    end

    Organizer --> ChooseEvent
    Staff --> ChooseEvent
    Admin --> ChooseEvent
    Organizer --> Stats
    Staff --> CheckIn
    Organizer --> CheckIn
    Admin --> CheckIn
    Admin --> ReviewOrg
    Admin --> ManageCategory
    Admin --> ManageUsers
    Admin --> Moderate

    CheckIn -.->|"<<include>>"| ChooseEvent
    UploadQr -.->|"<<extend>> chọn quét ảnh"| CheckIn
    ManualCode -.->|"<<extend>> chọn nhập tay"| CheckIn
    UploadQr -.->|"<<include>>"| DecodeQr
    CheckIn -.->|"<<include>>"| VerifyTicket
    CheckIn -.->|"<<include>>"| MarkUsed
    CheckIn -.->|"<<include>>"| Log
```

### 3.7 Quy ước chiều mũi tên

- `A --<<include>>--> B`: A luôn gọi B như một bước bắt buộc hoặc dùng chung.
- `X --<<extend>>--> A`: X bổ sung cho A khi có điều kiện hoặc lựa chọn cụ thể.
- Actor nối bằng đường liền với use case mà actor trực tiếp khởi tạo hoặc tham gia.
- Actor ngoài hệ thống như Email, Scheduler và Payment Gateway đặt ngoài biên `Eventix`.

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

## 9. Quan hệ `<<include>>` và `<<extend>>`

### 9.1 Sơ đồ quan hệ chi tiết

```mermaid
flowchart LR
    Register(["Đăng ký tài khoản"])
    ResetPassword(["Đặt lại mật khẩu"])
    VerifyOtp(["Xác minh OTP"])

    Hold(["Chọn và giữ vé"])
    CheckInventory(["Kiểm tra số lượng còn lại"])
    SelectSeat(["Chọn ghế cụ thể"])
    LockSeat(["Kiểm tra và khóa ghế"])

    Payment(["Thanh toán"])
    CreateOrder(["Tạo / kiểm tra đơn hàng"])
    IssueTicket(["Phát hành vé QR"])

    Cancel(["Hủy lượt giữ vé"])
    Expire(["Hết hạn lượt giữ vé"])
    Release(["Hoàn lại vé / giải phóng ghế"])

    CheckIn(["Check-in vé"])
    VerifyTicket(["Xác minh QR, trạng thái vé và quyền sự kiện"])
    ScanImage(["Quét QR từ ảnh tải lên"])
    ManualCode(["Nhập mã vé thủ công"])

    Notify(["Gửi thông báo email"])
    Mail["✉️ Email Service"]

    Register -.->|"<<include>>"| VerifyOtp
    ResetPassword -.->|"<<include>>"| VerifyOtp

    Hold -.->|"<<include>>"| CheckInventory
    SelectSeat -.->|"<<extend>> vé ngồi"| Hold
    SelectSeat -.->|"<<include>>"| LockSeat
    Hold -.->|"<<include>>"| Notify

    Payment -.->|"<<include>>"| CreateOrder
    Payment -.->|"<<include>>"| IssueTicket
    Payment -.->|"<<include>>"| Notify

    Cancel -.->|"<<include>>"| Release
    Cancel -.->|"<<include>>"| Notify
    Expire -.->|"<<include>>"| Release
    Expire -.->|"<<include>>"| Notify

    CheckIn -.->|"<<include>>"| VerifyTicket
    ScanImage -.->|"<<extend>> chọn quét ảnh"| CheckIn
    ManualCode -.->|"<<extend>> chọn nhập tay"| CheckIn

    Mail --- Notify
```

### 9.2 Giải thích `<<include>>`

`A <<include>> B` nghĩa là **mỗi lần thực hiện A thì B là bước bắt buộc hoặc được tái sử dụng**.
Mũi tên nét đứt đi từ `A` sang `B`.

| Use case chính A | Use case được include B | Lý do |
|---|---|---|
| Đăng ký tài khoản | Xác minh OTP | Tài khoản phải xác minh email theo luồng đăng ký hiện tại |
| Chọn và giữ vé | Kiểm tra số lượng còn lại | Không được giữ vượt quá quota |
| Chọn ghế cụ thể | Kiểm tra và khóa ghế | Ghế phải còn trống và được khóa nguyên tử |
| Thanh toán | Tạo/kiểm tra đơn hàng | Chỉ thanh toán đơn hợp lệ, chưa hết hạn |
| Thanh toán | Phát hành vé QR | Thanh toán thành công phải tạo vé sử dụng để check-in |
| Check-in | Xác minh QR, trạng thái và quyền | Mọi hình thức check-in đều phải xác minh vé |
| Giữ/thanh toán/hủy/hết hạn | Gửi thông báo email | Đây là hậu xử lý bắt buộc theo yêu cầu hiện tại |

Ví dụ: `Thanh toán --<<include>>--> Phát hành vé QR` vì một giao dịch được xem là hoàn tất trong
Eventix khi hệ thống cập nhật đơn hàng và phát hành vé tương ứng.

### 9.3 Giải thích `<<extend>>`

`X <<extend>> A` nghĩa là **X chỉ bổ sung cho A tại một extension point và chỉ chạy khi thỏa điều kiện**.
Mũi tên nét đứt đi từ use case mở rộng `X` về use case gốc `A`.

| Use case mở rộng X | Use case gốc A | Điều kiện mở rộng |
|---|---|---|
| Chọn ghế cụ thể | Chọn và giữ vé | Chỉ khi loại vé là vé ngồi; vé đứng chỉ chọn khu và số lượng |
| Quét QR từ ảnh tải lên | Check-in vé | Nhân viên chọn phương thức tải ảnh |
| Nhập mã vé thủ công | Check-in vé | Nhân viên chọn phương thức nhập tay |
| Công bố sự kiện | Tạo/chỉnh sửa sự kiện | Organizer chọn công bố ngay hoặc khi sự kiện đủ điều kiện |

Ví dụ: `Chọn ghế cụ thể --<<extend>>--> Chọn và giữ vé`. Việc giữ vé luôn tồn tại, nhưng bước chọn
số ghế chỉ xuất hiện với khu vé ngồi, vì vậy đây là `extend`, không phải `include` của mọi booking.

### 9.4 Phân biệt nhanh

| Tiêu chí | `<<include>>` | `<<extend>>` |
|---|---|---|
| Có bắt buộc không? | Có, trong phạm vi use case chính | Không, chỉ xảy ra theo điều kiện/lựa chọn |
| Mục đích | Tách bước dùng chung hoặc bắt buộc | Bổ sung một biến thể cho luồng gốc |
| Hướng mũi tên | Use case chính → use case được include | Use case mở rộng → use case gốc |
| Ví dụ Eventix | Check-in → Xác minh vé | Quét ảnh QR → Check-in |

## 10. Quy tắc authorization cần duy trì

1. Luôn lấy UserId từ JWT claim, không nhận UserId tùy ý từ client.
2. Booking/order/ticket chỉ truy cập dữ liệu có UserId trùng claim.
3. Organizer phải được kiểm tra ownership của event/venue.
4. Admin bypass ownership chỉ ở use case được định nghĩa rõ.
5. API public chỉ gồm danh sách/chi tiết event, category và dữ liệu thực sự công khai.
6. Phân biệt 401 (chưa xác thực/token lỗi) và 403 (đã xác thực nhưng thiếu quyền).

## 11. Use case chưa hoàn thiện

Các use case sau chưa có implementation:

- AI tagging và gợi ý sự kiện.
- Payment gateway thật (VNPay, MoMo) và webhook.

## 12. Kịch bản demo đề xuất

1. Guest đăng ký và nhập OTP.
2. Customer mở event, chọn zone ngồi và hai ghế.
3. Nhận email giữ vé.
4. Thanh toán Demo, nhận email có hai QR.
5. Organizer quét một QR: thành công; quét lại: bị từ chối.
6. Tạo lượt giữ khác nhưng không thanh toán; sau 15–16 phút nhận email thất bại và
   thấy ghế trở lại Available.
7. Tạo lượt giữ mới và hủy thủ công; nhận email hủy.