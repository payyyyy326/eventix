using Eventix.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CheckInLog> CheckInLogs { get; set; }

    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventImage> EventImages { get; set; }

    public virtual DbSet<EventSeatStatus> EventSeatStatuses { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrganizerProfile> OrganizerProfiles { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketType> TicketTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    public virtual DbSet<VenueSectionLayout> VenueSectionLayouts { get; set; }

    public virtual DbSet<VenueZone> VenueZones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC071EA4272C");

            entity.HasIndex(e => e.Slug, "UQ__Categori__BC7B5FB61865F06C").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug).HasMaxLength(255);
        });

        modelBuilder.Entity<CheckInLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CheckInL__3214EC07D4250B77");

            entity.HasIndex(e => e.EventId, "IX_CheckInLogs_EventId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasDefaultValue("QR");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.CheckInLogs)
                .HasForeignKey(d => d.CheckedInBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Check__7849DB76");

            entity.HasOne(d => d.Ticket).WithMany(p => p.CheckInLogs)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Ticke__76619304");
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailOtp__3214EC07B81F9198");

            entity.HasIndex(e => new { e.Email, e.Purpose }, "IX_EmailOtps_Email_Purpose");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.OtpCode).HasMaxLength(10);
            entity.Property(e => e.Purpose).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.EmailOtps)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmailOtps__UserI__32767D0B");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC07EC04CE22");

            entity.HasIndex(e => e.CategoryId, "IX_Events_CategoryId");

            entity.HasIndex(e => new { e.Status, e.StartTime }, "IX_Events_Status_StartTime");

            entity.HasIndex(e => e.VenueId, "IX_Events_VenueId");

            entity.HasIndex(e => e.ViewCount, "IX_Events_ViewCount");

            entity.HasIndex(e => e.Slug, "UQ_Events_Slug").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Slug).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.Events)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Events_Categories");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EventCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Organizer).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrganizerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Events_OrganizerProfiles");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EventUpdatedByNavigations).HasForeignKey(d => d.UpdatedBy);

            entity.HasOne(d => d.Venue).WithMany(p => p.Events)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Events_Venues");
        });

        modelBuilder.Entity<EventImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventIma__3214EC077BAC8034");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
        });

        modelBuilder.Entity<EventSeatStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventSea__3214EC07CD45FF48");

            entity.HasIndex(e => e.EventId, "IX_EventSeatStatuses_EventId");

            entity.HasIndex(e => e.SeatId, "IX_EventSeatStatuses_SeatId");

            entity.HasIndex(e => e.TicketTypeId, "IX_EventSeatStatuses_TicketTypeId");

            entity.HasIndex(e => new { e.EventId, e.SeatId }, "UQ_EventSeat").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Event).WithMany(p => p.EventSeatStatuses)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventSeatStatuses_Events");

            entity.HasOne(d => d.Seat).WithMany(p => p.EventSeatStatuses)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventSeatStatuses_Seats");

            entity.HasOne(d => d.TicketType).WithMany(p => p.EventSeatStatuses)
                .HasForeignKey(d => d.TicketTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventSeatStatuses_TicketTypes");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC0762EC344E");

            entity.HasIndex(e => e.Status, "IX_Orders_Status");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.HasIndex(e => e.OrderCode, "UQ__Orders__999B5229088109BF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderCode).HasMaxLength(100);
            entity.Property(e => e.ServiceFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__UserId__29221CFB");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07F57FA047");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Order__2EDAF651");
        });

        modelBuilder.Entity<OrganizerProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Organize__3214EC077BF9D8FB");

            entity.HasIndex(e => e.UserId, "UQ__Organize__1788CC4DF8873FCF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.OrganizationName).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.OrganizerProfileApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__Organizer__Appro__49C3F6B7");

            entity.HasOne(d => d.User).WithOne(p => p.OrganizerProfileUser)
                .HasForeignKey<OrganizerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Organizer__UserI__48CFD27E");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC07FB97E460");

            entity.HasIndex(e => e.OrderId, "IX_Payments_OrderId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.Gateway).HasMaxLength(50);
            entity.Property(e => e.GatewayTransactionId).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionCode).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__OrderI__44CA3770");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__UserId__45BE5BA9");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservat__3214EC07FDDFBED1");

            entity.HasIndex(e => new { e.ExpiresAt, e.Status }, "IX_Reservations_ExpiresAt");

            entity.HasIndex(e => new { e.EventId, e.SeatId }, "UX_Reservations_ActiveSeat")
                .IsUnique()
                .HasFilter("([SeatId] IS NOT NULL AND [Status]='Active')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Order).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Reservati__Order__3D2915A8");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__UserI__395884C4");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07280D82EC");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6B749F1FB").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Seats__3214EC07FF0CCB8E");

            entity.HasIndex(e => new { e.VenueId, e.Section, e.Row, e.Number }, "UQ_Seats").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.Row).HasMaxLength(50);
            entity.Property(e => e.Section).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            entity.Property(e => e.Xposition)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("XPosition");
            entity.Property(e => e.Yposition)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("YPosition");

            entity.HasOne(d => d.Venue).WithMany(p => p.Seats)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Seats__VenueId__70DDC3D8");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tickets__3214EC07DCA8A06F");

            entity.HasIndex(e => e.EventId, "IX_Tickets_EventId");

            entity.HasIndex(e => e.UserId, "IX_Tickets_UserId");

            entity.HasIndex(e => e.QrToken, "UQ__Tickets__517D24C53EB953E9").IsUnique();

            entity.HasIndex(e => e.TicketCode, "UQ__Tickets__598CF7A39622F696").IsUnique();

            entity.HasIndex(e => new { e.EventId, e.SeatId }, "UX_Tickets_EventSeat")
                .IsUnique()
                .HasFilter("([SeatId] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.QrToken).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Sold");
            entity.Property(e => e.TicketCode).HasMaxLength(100);

            entity.HasOne(d => d.Order).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__OrderId__55F4C372");

            entity.HasOne(d => d.User).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__UserId__56E8E7AB");
        });

        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketTy__3214EC078AA5A793");

            entity.HasIndex(e => e.EventId, "IX_TicketTypes_EventId");

            entity.HasIndex(e => new { e.EventId, e.Section }, "IX_TicketTypes_EventId_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsSeatRequired).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Section).HasMaxLength(100);

            entity.HasOne(d => d.Event).WithMany(p => p.TicketTypes)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TicketTypes_Events");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07F132214A");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105342ED2E389").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__4222D4EF"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__412EB0B6"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF2760AD8F16A7B5");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRefr__3214EC077BD6BB65");

            entity.HasIndex(e => e.Token, "IX_UserRefreshTokens_Token");

            entity.HasIndex(e => e.UserId, "IX_UserRefreshTokens_UserId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Token).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRefre__UserI__44952D46");
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Venues__3214EC0721540A0D");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Venues)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Venues__CreatedB__6477ECF3");
        });
        modelBuilder.Entity<VenueSectionLayout>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VenueSec__3214EC07ADE50E52");

            // Unique index cũ theo VenueId+Section bị loại bỏ vì
            // nhiều TicketType có thể có cùng section name trong cùng venue.
            // Thay bằng index không unique.
            entity.HasIndex(e => new { e.VenueId, e.Section }, "IX_VenueSectionLayouts_VenueId_Section")
                  .IsUnique(false);

            entity.HasIndex(e => e.TicketTypeId, "IX_VenueSectionLayouts_TicketTypeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .HasDefaultValue("#60A5FA");
            entity.Property(e => e.Section).HasMaxLength(100);

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueSectionLayouts)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VenueSectionLayouts_Venues");

            entity.HasOne(d => d.VenueZone).WithMany(p => p.VenueSectionLayouts)
                .HasForeignKey(d => d.VenueZoneId)
                .HasConstraintName("FK_VenueSectionLayouts_VenueZones");

            entity.HasOne(d => d.TicketType).WithMany(p => p.VenueSectionLayouts)
                .HasForeignKey(d => d.TicketTypeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VenueSectionLayouts_TicketTypes");
        });

        modelBuilder.Entity<VenueZone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VenueZon__3214EC07543B345D");

            entity.HasIndex(e => new { e.VenueId, e.Name }, "IX_VenueZones_VenueId_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .HasDefaultValue("#60A5FA");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.HasSeats).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueZones)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VenueZones_Venues");
        });

        OnModelCreatingPartial(modelBuilder);

    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
