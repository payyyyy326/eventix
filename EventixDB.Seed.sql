/*
    Eventix development seed
    -------------------------------------------------------------------------
    Run this AFTER EventixDB.sql (and add_tickettypeid_to_sectionlayouts.sql).

    Scope: 3 roles only (Admin, Customer, Organizer), 129 users, 8 organizers,
    11 categories, 12 venues, 24 events, 72 ticket types, 4,320 mapped seats,
    260 paid orders/tickets and active reservations.

    The seat-map data deliberately uses the TicketType-based flow:
      - TicketTypes.VenueZoneId       = NULL
      - Seats.VenueZoneId             = NULL
      - VenueSectionLayouts.VenueZoneId = NULL
      - VenueSectionLayouts.TicketTypeId is populated
    No row is inserted into VenueZones.

    Development login password for every seeded user: password
    (Replace the BCrypt hash before using this script outside local development.)
*/
USE [EventTicketDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email LIKE '%@seed.eventix.local')
    THROW 51000, 'Seed data already exists. Restore an empty database before re-running this seed.', 1;
GO

BEGIN TRANSACTION;

DECLARE @Now datetime2(7) = SYSUTCDATETIME();
DECLARE @PasswordHash nvarchar(max) = N'$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'; -- password
DECLARE @AdminId uniqueidentifier = NEWID();

DECLARE @Users table
(
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    UserNo int NOT NULL,
    UserType varchar(20) NOT NULL,
    FullName nvarchar(255) NOT NULL,
    Email nvarchar(255) NOT NULL UNIQUE
);
DECLARE @Organizers table
(
    UserId uniqueidentifier NOT NULL PRIMARY KEY,
    ProfileId uniqueidentifier NOT NULL UNIQUE,
    OrganizerNo int NOT NULL UNIQUE
);
DECLARE @Categories table (Id uniqueidentifier NOT NULL PRIMARY KEY, CategoryNo int NOT NULL UNIQUE);
DECLARE @Venues table (Id uniqueidentifier NOT NULL PRIMARY KEY, VenueNo int NOT NULL UNIQUE);
DECLARE @Events table (Id uniqueidentifier NOT NULL PRIMARY KEY, EventNo int NOT NULL UNIQUE, VenueId uniqueidentifier NOT NULL, Status nvarchar(50) NOT NULL);
DECLARE @TicketTypes table (Id uniqueidentifier NOT NULL PRIMARY KEY, EventId uniqueidentifier NOT NULL, EventNo int NOT NULL, TierNo int NOT NULL, Section nvarchar(100) NOT NULL, IsSeatRequired bit NOT NULL, Price decimal(18,2) NOT NULL);
DECLARE @Orders table (Id uniqueidentifier NOT NULL PRIMARY KEY, OrderNo int NOT NULL UNIQUE, UserId uniqueidentifier NOT NULL, EventId uniqueidentifier NOT NULL, TicketTypeId uniqueidentifier NOT NULL, SeatId uniqueidentifier NULL, TotalAmount decimal(18,2) NOT NULL);

/* Exactly the three application roles requested. */
INSERT dbo.Roles (Id, Name)
SELECT NEWID(), r.Name
FROM (VALUES (N'Admin'), (N'Customer'), (N'Organizer')) r(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles x WHERE x.Name = r.Name);

INSERT @Users (Id, UserNo, UserType, FullName, Email)
VALUES (@AdminId, 0, 'Admin', N'Eventix System Admin', N'admin@seed.eventix.local');

;WITH n AS
(
    SELECT TOP (8) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects
)
INSERT @Users (Id, UserNo, UserType, FullName, Email)
SELECT NEWID(), No, 'Organizer',
       CHOOSE(No, N'Nguyễn Minh Anh', N'Trần Quốc Bảo', N'Lê Hoài Phương', N'Phạm Gia Huy', N'Vũ Khánh Linh', N'Đỗ Thành Nam', N'Bùi Mỹ Duyên', N'Hoàng Nhật Long'),
       N'organizer' + RIGHT(N'00' + CONVERT(nvarchar(2), No), 2) + N'@seed.eventix.local'
FROM n;

;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT @Users (Id, UserNo, UserType, FullName, Email)
SELECT NEWID(), No, 'Customer',
       CHOOSE(((No - 1) % 20) + 1,
           N'Nguyễn Hải Yến', N'Trần Đức Anh', N'Lê Bảo Ngọc', N'Phạm Minh Khang', N'Võ Thu Hà',
           N'Đặng Quang Huy', N'Bùi Khánh Vy', N'Hoàng Gia Bảo', N'Đỗ Phương Thảo', N'Ngô Thành Đạt',
           N'Vũ Mai Anh', N'Dương Quốc Khánh', N'Lý Tú Uyên', N'Đinh Trung Kiên', N'Phan Nhật Hào',
           N'Châu Thanh Trúc', N'Huỳnh Minh Châu', N'Đào Anh Tuấn', N'Đoàn Mỹ Linh', N'La Quốc Việt')
       + N' ' + RIGHT(N'000' + CONVERT(nvarchar(3), No), 3),
       N'customer' + RIGHT(N'000' + CONVERT(nvarchar(3), No), 3) + N'@seed.eventix.local'
FROM n;

INSERT dbo.Users (Id, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, CreatedAt, UpdatedAt, EmailVerified, EmailVerifiedAt)
SELECT Id, Email, @PasswordHash, FullName,
       N'0' + RIGHT(N'000000000' + CONVERT(nvarchar(9), 900000000 + UserNo), 9),
       N'https://i.pravatar.cc/300?u=' + Email, N'ACTIVE', DATEADD(day, -UserNo - 20, @Now), @Now, 1, DATEADD(day, -UserNo - 19, @Now)
FROM @Users;

INSERT dbo.UserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM @Users u
JOIN dbo.Roles r ON r.Name = CASE u.UserType WHEN 'Admin' THEN N'Admin' WHEN 'Organizer' THEN N'Organizer' ELSE N'Customer' END;

INSERT @Organizers (UserId, ProfileId, OrganizerNo)
SELECT Id, NEWID(), UserNo FROM @Users WHERE UserType = 'Organizer';

INSERT dbo.OrganizerProfiles (Id, UserId, OrganizationName, Description, ContactEmail, ContactPhone, Status, ApprovedBy, ApprovedAt, CreatedAt)
SELECT o.ProfileId, o.UserId,
       CHOOSE(o.OrganizerNo, N'Lotus Live Entertainment', N'North Star Events', N'Mây Lang Thang Production', N'NextGen Conference', N'Urban Beats Vietnam', N'VietSport Community', N'ArtHaus Collective', N'Green Future Foundation'),
       N'Đơn vị tổ chức sự kiện chuyên nghiệp, tập trung vào trải nghiệm an toàn và đáng nhớ cho khán giả.',
       u.Email, u.PhoneNumber, N'Approved', @AdminId, DATEADD(day, -70, @Now), DATEADD(day, -90, @Now)
FROM @Organizers o JOIN dbo.Users u ON u.Id = o.UserId;

INSERT @Categories (Id, CategoryNo)
SELECT NEWID(), v.No
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11)) v(No);

INSERT dbo.Categories (Id, Name, Slug, Description, IsActive, CreatedAt, CreatedBy)
SELECT c.Id,
       CHOOSE(c.CategoryNo, N'Âm nhạc', N'Đời sống', N'Kinh doanh', N'Công nghệ', N'Thể thao', N'Nghệ thuật', N'Ẩm thực', N'Giáo dục', N'Cộng đồng', N'Gia đình', N'Khác'),
       CHOOSE(c.CategoryNo, N'am-nhac', N'doi-song', N'kinh-doanh', N'cong-nghe', N'the-thao', N'nghe-thuat', N'am-thuc', N'giao-duc', N'cong-dong', N'gia-dinh', N'khac'),
       N'Các sự kiện được tuyển chọn bởi Eventix.', 1, DATEADD(day, -120, @Now), @AdminId
FROM @Categories c;

INSERT @Venues (Id, VenueNo)
SELECT NEWID(), v.No
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) v(No);

INSERT dbo.Venues (Id, Name, Address, City, Capacity, CreatedBy, CreatedAt)
SELECT v.Id,
       CHOOSE(v.VenueNo, N'Nhà thi đấu Quân Khu 7', N'Trung tâm Hội nghị Quốc gia', N'Nhà hát Hòa Bình', N'Đại học RMIT Sài Gòn', N'Công viên Yên Sở', N'Cung Văn hóa Hữu nghị Việt Xô', N'WTC Expo Bình Dương', N'Nhà thi đấu Nguyễn Du', N'Đại học Bách Khoa Đà Nẵng', N'Quảng trường 2/4 Nha Trang', N'Nhà Văn hóa Thanh niên', N'GEM Center'),
       CHOOSE(v.VenueNo, N'202 Hoàng Văn Thụ, Tân Bình', N'57 Phạm Hùng, Nam Từ Liêm', N'240 Đường 3 Tháng 2, Quận 10', N'702 Nguyễn Văn Linh, Quận 7', N'Gamuda Central, Hoàng Mai', N'91 Trần Hưng Đạo, Hoàn Kiếm', N'A19 Hùng Vương, Thủ Dầu Một', N'116 Nguyễn Du, Quận 1', N'54 Nguyễn Lương Bằng, Liên Chiểu', N'Trần Phú, Lộc Thọ', N'4 Phạm Ngọc Thạch, Quận 1', N'8 Nguyễn Bỉnh Khiêm, Quận 1'),
       CHOOSE(v.VenueNo, N'Hồ Chí Minh', N'Hà Nội', N'Hồ Chí Minh', N'Hồ Chí Minh', N'Hà Nội', N'Hà Nội', N'Bình Dương', N'Hồ Chí Minh', N'Đà Nẵng', N'Khánh Hòa', N'Hồ Chí Minh', N'Hồ Chí Minh'),
       CHOOSE(v.VenueNo, 3000, 3500, 1800, 1200, 5000, 2200, 4000, 1500, 1800, 2500, 1000, 800), @AdminId, DATEADD(day, -100, @Now)
FROM @Venues v;

;WITH n AS
(
    SELECT TOP (24) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects
)
INSERT @Events (Id, EventNo, VenueId, Status)
SELECT NEWID(), n.No, v.Id,
       CASE WHEN n.No <= 6 THEN N'Completed' WHEN n.No = 7 THEN N'Ongoing' WHEN n.No <= 20 THEN N'OnSale' WHEN n.No <= 22 THEN N'Published' ELSE N'Draft' END
FROM n JOIN @Venues v ON v.VenueNo = ((n.No - 1) % 12) + 1;

INSERT dbo.Events (Id, OrganizerId, CategoryId, VenueId, Title, Slug, Description, Summary, ImageUrl, BannerUrl, StartTime, EndTime, Status, ViewCount, IsFeatured, CreatedAt, CreatedBy, PublishedAt)
SELECT e.Id, o.ProfileId, c.Id, e.VenueId,
       CHOOSE(((e.EventNo - 1) % 12) + 1,
          N'Những Thành Phố Mộng Mơ', N'Vietnam Tech Summit 2026', N'Ngày Hội Sống Xanh', N'Indie Vibes Live', N'Future of Work Forum', N'Run for Smiles 2026',
          N'Art After Dark', N'Food & Craft Weekend', N'AI for Everyone', N'Family Fun Festival', N'Jazz in the Garden', N'Creative Leaders Meetup') + N' ' + CONVERT(nvarchar(4), 2025 + ((e.EventNo - 1) / 12)),
       N'seed-event-' + RIGHT(N'00' + CONVERT(nvarchar(2), e.EventNo), 2),
       N'Một sự kiện được thiết kế với nội dung chất lượng, không gian chỉn chu và trải nghiệm thuận tiện từ lúc đặt vé đến khi check-in.',
       N'Khám phá, kết nối và lưu giữ những khoảnh khắc đáng nhớ cùng cộng đồng.',
       N'https://images.unsplash.com/photo-1492684223066-81342ee5ff30?auto=format&fit=crop&w=1200&q=80',
       N'https://images.unsplash.com/photo-1506157786151-b8491531f063?auto=format&fit=crop&w=1800&q=85',
       CASE WHEN e.EventNo <= 6 THEN DATEADD(day, -70 + (e.EventNo * 8), @Now) WHEN e.EventNo = 7 THEN DATEADD(hour, -2, @Now) ELSE DATEADD(day, 5 + ((e.EventNo - 8) * 6), @Now) END,
       CASE WHEN e.EventNo <= 6 THEN DATEADD(hour, 5, DATEADD(day, -70 + (e.EventNo * 8), @Now)) WHEN e.EventNo = 7 THEN DATEADD(hour, 5, @Now) ELSE DATEADD(hour, 4, DATEADD(day, 5 + ((e.EventNo - 8) * 6), @Now)) END,
       e.Status, 450 + e.EventNo * 137, CASE WHEN e.EventNo IN (8, 11, 14, 17) THEN 1 ELSE 0 END,
       DATEADD(day, -45, @Now), ou.Id, CASE WHEN e.Status = N'Draft' THEN NULL ELSE DATEADD(day, -35, @Now) END
FROM @Events e
JOIN @Organizers o ON o.OrganizerNo = ((e.EventNo - 1) % 8) + 1
JOIN @Users ou ON ou.Id = o.UserId
JOIN @Categories c ON c.CategoryNo = ((e.EventNo - 1) % 10) + 1;

INSERT @TicketTypes (Id, EventId, EventNo, TierNo, Section, IsSeatRequired, Price)
SELECT NEWID(), e.Id, e.EventNo, t.TierNo, N'MAP-' + RIGHT(N'00' + CONVERT(nvarchar(2), e.EventNo), 2) + N'-' + t.Code, t.IsSeatRequired, t.Price
FROM @Events e
CROSS JOIN (VALUES
    (1, N'Vé VIP',      N'Khu vực gần sân khấu, lối vào ưu tiên và quà tặng sự kiện.',  650000.00,  60, 1, N'VIP'),
    (2, N'Vé Standard', N'Chỗ ngồi tiêu chuẩn với tầm nhìn tốt.',                         350000.00, 120, 1, N'STD'),
    (3, N'Vé General',  N'Vé tham dự tự do, không chọn ghế.',                            150000.00, 300, 0, N'GEN')
) t(TierNo, Name, Description, Price, Quantity, IsSeatRequired, Code);

INSERT dbo.TicketTypes (Id, EventId, Name, Description, Price, Quantity, SoldQuantity, ReservedQuantity, Section, SaleStartTime, SaleEndTime, IsSeatRequired, CreatedAt, CreatedBy, Status, VenueZoneId)
SELECT tt.Id, tt.EventId,
       CHOOSE(tt.TierNo, N'Vé VIP', N'Vé Standard', N'Vé General'),
       CHOOSE(tt.TierNo, N'Khu vực gần sân khấu, lối vào ưu tiên và quà tặng sự kiện.', N'Chỗ ngồi tiêu chuẩn với tầm nhìn tốt.', N'Vé tham dự tự do, không chọn ghế.'),
       tt.Price, CHOOSE(tt.TierNo, 60, 120, 300), 0, 0, tt.Section,
       DATEADD(day, -30, @Now), DATEADD(day, CASE WHEN e.Status IN (N'Completed', N'Ongoing') THEN -1 ELSE 3 + ((tt.EventNo - 8) * 6) END, @Now),
       tt.IsSeatRequired, DATEADD(day, -40, @Now), o.UserId, CASE WHEN e.Status = N'Draft' THEN N'Inactive' ELSE N'Active' END, NULL
FROM @TicketTypes tt JOIN @Events e ON e.Id = tt.EventId JOIN @Organizers o ON o.OrganizerNo = ((tt.EventNo - 1) % 8) + 1;

/* Each section block belongs directly to a ticket type; VenueZoneId stays NULL. */
INSERT dbo.VenueSectionLayouts (Id, VenueId, Section, X, Y, Width, Height, Color, CreatedAt, VenueZoneId, TicketTypeId)
SELECT NEWID(), e.VenueId, tt.Section,
       CASE tt.TierNo WHEN 1 THEN 340 WHEN 2 THEN 100 ELSE 580 END,
       CASE tt.TierNo WHEN 1 THEN 80 WHEN 2 THEN 250 ELSE 80 END,
       CASE tt.TierNo WHEN 1 THEN 260 WHEN 2 THEN 520 ELSE 160 END,
       CASE tt.TierNo WHEN 1 THEN 130 WHEN 2 THEN 260 ELSE 430 END,
       CASE tt.TierNo WHEN 1 THEN N'#F59E0B' WHEN 2 THEN N'#3B82F6' ELSE N'#10B981' END,
       @Now, NULL, tt.Id
FROM @TicketTypes tt JOIN @Events e ON e.Id = tt.EventId
WHERE tt.IsSeatRequired = 1;

;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects
)
INSERT dbo.Seats (Id, VenueId, Section, Row, Number, XPosition, YPosition, Status, VenueZoneId)
SELECT NEWID(), e.VenueId, tt.Section,
       NCHAR(65 + ((n.No - 1) / 12)), CONVERT(nvarchar(10), ((n.No - 1) % 12) + 1),
       30 + (((n.No - 1) % 12) * 42), 30 + (((n.No - 1) / 12) * 42), N'Available', NULL
FROM @TicketTypes tt
JOIN @Events e ON e.Id = tt.EventId
JOIN n ON n.No <= CASE WHEN tt.TierNo = 1 THEN 60 ELSE 120 END
WHERE tt.IsSeatRequired = 1;

INSERT dbo.EventSeatStatuses (Id, EventId, SeatId, TicketTypeId, Status)
SELECT NEWID(), tt.EventId, s.Id, tt.Id, N'Available'
FROM @TicketTypes tt JOIN dbo.Seats s ON s.Section = tt.Section
WHERE tt.IsSeatRequired = 1;

;WITH n AS
(
    SELECT TOP (260) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
), selections AS
(
    SELECT n.No, u.Id AS UserId, tt.Id AS TicketTypeId, tt.EventId, tt.Price, tt.IsSeatRequired,
           ROW_NUMBER() OVER (PARTITION BY tt.Id ORDER BY n.No) AS SeatSequence
    FROM n
    JOIN @Users u ON u.UserType = 'Customer' AND u.UserNo = ((n.No - 1) % 120) + 1
    JOIN @TicketTypes tt ON tt.EventNo = ((n.No - 1) % 20) + 1 AND tt.TierNo = ((n.No - 1) % 3) + 1
)
INSERT @Orders (Id, OrderNo, UserId, EventId, TicketTypeId, SeatId, TotalAmount)
SELECT NEWID(), x.No, x.UserId, x.EventId, x.TicketTypeId,
       CASE WHEN x.IsSeatRequired = 1 THEN ss.SeatId END,
       x.Price + CEILING(x.Price * 0.03)
FROM selections x
OUTER APPLY
(
    SELECT SeatId FROM
    (
        SELECT ess.SeatId, ROW_NUMBER() OVER (ORDER BY ess.SeatId) AS rn
        FROM dbo.EventSeatStatuses ess WHERE ess.TicketTypeId = x.TicketTypeId
    ) z WHERE z.rn = x.SeatSequence
) ss;

INSERT dbo.Orders (Id, UserId, OrderCode, Status, SubTotal, ServiceFee, DiscountAmount, TotalAmount, ExpiresAt, PaidAt, CreatedAt, UpdatedAt)
SELECT o.Id, o.UserId, N'EVX' + CONVERT(char(8), DATEADD(day, -o.OrderNo, @Now), 112) + N'-' + RIGHT(N'0000' + CONVERT(nvarchar(4), o.OrderNo), 4),
       N'Paid', o.TotalAmount - CEILING((o.TotalAmount / 1.03) * 0.03), CEILING((o.TotalAmount / 1.03) * 0.03), 0, o.TotalAmount,
       DATEADD(minute, 15, DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime)), DATEADD(minute, 4, DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime)), DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime), DATEADD(minute, 4, DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime))
FROM @Orders o JOIN dbo.Events e ON e.Id = o.EventId;

INSERT dbo.OrderItems (Id, OrderId, EventId, TicketTypeId, SeatId, Quantity, UnitPrice, TotalPrice)
SELECT NEWID(), o.Id, o.EventId, o.TicketTypeId, o.SeatId, 1, tt.Price, tt.Price
FROM @Orders o JOIN @TicketTypes tt ON tt.Id = o.TicketTypeId;

INSERT dbo.Payments (Id, OrderId, UserId, Gateway, TransactionCode, GatewayTransactionId, Amount, Currency, Status, PaymentUrl, PaidAt, CreatedAt)
SELECT NEWID(), o.Id, o.UserId, CASE WHEN o.OrderNo % 3 = 0 THEN N'VNPay' WHEN o.OrderNo % 3 = 1 THEN N'MoMo' ELSE N'ZaloPay' END,
       N'TXN' + RIGHT(N'000000' + CONVERT(nvarchar(6), o.OrderNo), 6), N'GW' + CONVERT(nvarchar(36), NEWID()), o.TotalAmount, N'VND', N'Success', NULL,
       DATEADD(minute, 4, DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime)), DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime)
FROM @Orders o JOIN dbo.Events e ON e.Id = o.EventId;

INSERT dbo.Tickets (Id, EventId, TicketTypeId, OrderId, UserId, SeatId, TicketCode, QrToken, Status, IssuedAt, CheckedInAt)
SELECT NEWID(), o.EventId, o.TicketTypeId, o.Id, o.UserId, o.SeatId,
       N'TKT-' + RIGHT(N'000000' + CONVERT(nvarchar(6), o.OrderNo), 6), CONVERT(nvarchar(36), NEWID()),
       CASE WHEN e.Status = N'Completed' THEN N'Used' ELSE N'Active' END,
       DATEADD(minute, 5, DATEADD(day, -20 - (o.OrderNo % 15), e.StartTime)), CASE WHEN e.Status = N'Completed' THEN DATEADD(minute, 30, e.StartTime) END
FROM @Orders o JOIN dbo.Events e ON e.Id = o.EventId;

UPDATE ess SET Status = N'Sold'
FROM dbo.EventSeatStatuses ess JOIN @Orders o ON o.SeatId = ess.SeatId AND o.EventId = ess.EventId;

UPDATE tt SET SoldQuantity = x.SoldCount
FROM dbo.TicketTypes tt
JOIN (SELECT TicketTypeId, COUNT(*) AS SoldCount FROM @Orders GROUP BY TicketTypeId) x ON x.TicketTypeId = tt.Id;

INSERT dbo.CheckInLogs (Id, TicketId, EventId, CheckedInBy, CheckInTime, Method, Note)
SELECT NEWID(), t.Id, t.EventId, @AdminId, t.CheckedInAt, CASE WHEN ROW_NUMBER() OVER (ORDER BY t.IssuedAt) % 4 = 0 THEN N'Manual' ELSE N'QR' END, N'Check-in thành công'
FROM dbo.Tickets t WHERE t.Status = N'Used';

;WITH r AS
(
    SELECT TOP (24) ROW_NUMBER() OVER (ORDER BY ess.SeatId) AS No, ess.EventId, ess.TicketTypeId, ess.SeatId
    FROM dbo.EventSeatStatuses ess
    JOIN @Events e ON e.Id = ess.EventId AND e.Status = N'OnSale'
    WHERE ess.Status = N'Available'
)
INSERT dbo.Reservations (Id, UserId, EventId, TicketTypeId, SeatId, OrderId, Quantity, Status, ExpiresAt, CreatedAt)
SELECT NEWID(), u.Id, r.EventId, r.TicketTypeId, r.SeatId, NULL, 1, N'Active', DATEADD(minute, 12, @Now), DATEADD(minute, -3, @Now)
FROM r JOIN @Users u ON u.UserType = 'Customer' AND u.UserNo = r.No;

UPDATE ess SET Status = N'Reserved'
FROM dbo.EventSeatStatuses ess
JOIN dbo.Reservations r ON r.EventId = ess.EventId AND r.SeatId = ess.SeatId AND r.Status = N'Active';

UPDATE tt SET ReservedQuantity = x.ReservedCount
FROM dbo.TicketTypes tt
JOIN (SELECT TicketTypeId, COUNT(*) AS ReservedCount FROM dbo.Reservations WHERE Status = N'Active' GROUP BY TicketTypeId) x ON x.TicketTypeId = tt.Id;

INSERT dbo.EventImages (Id, EventId, ImageUrl, SortOrder)
SELECT NEWID(), e.Id,
       CASE i.SortOrder WHEN 0 THEN N'https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?auto=format&fit=crop&w=1200&q=80' ELSE N'https://images.unsplash.com/photo-1501386761578-eac5c94b800a?auto=format&fit=crop&w=1200&q=80' END,
       i.SortOrder
FROM @Events e CROSS JOIN (VALUES (0),(1)) i(SortOrder);

COMMIT TRANSACTION;

SELECT N'Eventix seed completed' AS Result,
       (SELECT COUNT(*) FROM dbo.Users WHERE Email LIKE '%@seed.eventix.local') AS Users,
       (SELECT COUNT(*) FROM dbo.Events WHERE Slug LIKE 'seed-event-%') AS Events,
       (SELECT COUNT(*) FROM dbo.TicketTypes tt JOIN dbo.Events e ON e.Id = tt.EventId WHERE e.Slug LIKE 'seed-event-%') AS TicketTypes,
       (SELECT COUNT(*) FROM dbo.Seats WHERE VenueZoneId IS NULL) AS TicketTypeBasedSeats,
       (SELECT COUNT(*) FROM dbo.VenueZones) AS VenueZonesInserted;
GO
