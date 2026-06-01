using Eventix.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CheckInLog> CheckInLogs { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<CouponUsage> CouponUsages { get; set; }

    public virtual DbSet<EmailOtp> EmailOtps { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventAitag> EventAitags { get; set; }

    public virtual DbSet<EventImage> EventImages { get; set; }

    public virtual DbSet<EventSeatStatus> EventSeatStatuses { get; set; }

    public virtual DbSet<EventVenue> EventVenues { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrganizerProfile> OrganizerProfiles { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; }

    public virtual DbSet<RefundPolicy> RefundPolicies { get; set; }

    public virtual DbSet<RefundRequest> RefundRequests { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketType> TicketTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserEventInteraction> UserEventInteractions { get; set; }

    public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC072C175FD0");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs).HasConstraintName("FK__AuditLogs__UserI__02C769E9");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carts__3214EC07E0B1B1AC");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Carts__UserId__160F4887");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CartItem__3214EC07FECD2426");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__CartI__19DFD96B");

            entity.HasOne(d => d.Event).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__Event__1AD3FDA4");

            entity.HasOne(d => d.Seat).WithMany(p => p.CartItems).HasConstraintName("FK__CartItems__SeatI__1CBC4616");

            entity.HasOne(d => d.TicketType).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__Ticke__1BC821DD");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC071EA4272C");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<CheckInLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CheckInL__3214EC07D4250B77");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Method).HasDefaultValue("QR");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.CheckInLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Check__7849DB76");

            entity.HasOne(d => d.Event).WithMany(p => p.CheckInLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Event__7755B73D");

            entity.HasOne(d => d.Ticket).WithMany(p => p.CheckInLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Ticke__76619304");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Coupons__3214EC07587D72C3");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Scope).HasDefaultValue("Global");

            entity.HasOne(d => d.Event).WithMany(p => p.Coupons).HasConstraintName("FK__Coupons__EventId__0D7A0286");
        });

        modelBuilder.Entity<CouponUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CouponUs__3214EC07D1607D67");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.UsedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Coupon).WithMany(p => p.CouponUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CouponUsa__Coupo__5CA1C101");

            entity.HasOne(d => d.Order).WithMany(p => p.CouponUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CouponUsa__Order__5E8A0973");

            entity.HasOne(d => d.User).WithMany(p => p.CouponUsages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CouponUsa__UserI__5D95E53A");
        });

        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailOtp__3214EC07B81F9198");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.EmailOtps)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmailOtps__UserI__32767D0B");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC07D6788041");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EventType).HasDefaultValue("GeneralAdmission");
            entity.Property(e => e.Status).HasDefaultValue("Draft");

            entity.HasOne(d => d.Category).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Events__Category__59063A47");

            entity.HasOne(d => d.Organizer).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Events__Organize__5812160E");
        });

        modelBuilder.Entity<EventAitag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventAIT__3214EC078C5161E6");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Event).WithMany(p => p.EventAitags)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventAITa__Event__14E61A24");
        });

        modelBuilder.Entity<EventImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventIma__3214EC077BAC8034");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Event).WithMany(p => p.EventImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventImag__Event__5FB337D6");
        });

        modelBuilder.Entity<EventSeatStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventSea__3214EC073EC73FBE");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Status).HasDefaultValue("Available");

            entity.HasOne(d => d.Event).WithMany(p => p.EventSeatStatuses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventSeat__Event__76969D2E");

            entity.HasOne(d => d.Seat).WithMany(p => p.EventSeatStatuses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventSeat__SeatI__778AC167");
        });

        modelBuilder.Entity<EventVenue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventVen__3214EC072ED679C4");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Event).WithOne(p => p.EventVenue)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventVenu__Event__6A30C649");

            entity.HasOne(d => d.Venue).WithMany(p => p.EventVenues)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventVenu__Venue__6B24EA82");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07DFD7B911");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__7E02B4CC");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC0762EC344E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders).HasConstraintName("FK__Orders__CouponId__2A164134");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__UserId__29221CFB");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07F57FA047");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Event).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Event__2FCF1A8A");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Order__2EDAF651");

            entity.HasOne(d => d.Seat).WithMany(p => p.OrderItems).HasConstraintName("FK__OrderItem__SeatI__31B762FC");

            entity.HasOne(d => d.TicketType).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Ticke__30C33EC3");
        });

        modelBuilder.Entity<OrganizerProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Organize__3214EC077BF9D8FB");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.OrganizerProfileApprovedByNavigations).HasConstraintName("FK__Organizer__Appro__49C3F6B7");

            entity.HasOne(d => d.User).WithOne(p => p.OrganizerProfileUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Organizer__UserI__48CFD27E");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__3214EC07FB97E460");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Currency).HasDefaultValue("VND");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__OrderI__44CA3770");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__UserId__45BE5BA9");
        });

        modelBuilder.Entity<PaymentWebhookLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PaymentW__3214EC0736C99E57");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<RefundPolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefundPo__3214EC079B0D295E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Event).WithOne(p => p.RefundPolicy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefundPol__Event__671F4F74");
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefundRe__3214EC0744CA872E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Order).WithMany(p => p.RefundRequests)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefundReq__Order__6DCC4D03");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RefundRequestReviewedByNavigations).HasConstraintName("FK__RefundReq__Revie__6FB49575");

            entity.HasOne(d => d.User).WithMany(p => p.RefundRequestUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefundReq__UserI__6EC0713C");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservat__3214EC07FDDFBED1");

            entity.HasIndex(e => new { e.EventId, e.SeatId }, "UX_Reservations_ActiveSeat")
                .IsUnique()
                .HasFilter("([SeatId] IS NOT NULL AND [Status]='Active')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.Event).WithMany(p => p.Reservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__Event__3A4CA8FD");

            entity.HasOne(d => d.Order).WithMany(p => p.Reservations).HasConstraintName("FK__Reservati__Order__3D2915A8");

            entity.HasOne(d => d.Seat).WithMany(p => p.Reservations).HasConstraintName("FK__Reservati__SeatI__3C34F16F");

            entity.HasOne(d => d.TicketType).WithMany(p => p.Reservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__Ticke__3B40CD36");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__UserI__395884C4");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC07F15EAE48");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Event).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__EventId__0880433F");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__UserId__09746778");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07280D82EC");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Seats__3214EC07FF0CCB8E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.Venue).WithMany(p => p.Seats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Seats__VenueId__70DDC3D8");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tickets__3214EC07DCA8A06F");

            entity.HasIndex(e => new { e.EventId, e.SeatId }, "UX_Tickets_EventSeat")
                .IsUnique()
                .HasFilter("([SeatId] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Sold");

            entity.HasOne(d => d.Event).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__EventId__540C7B00");

            entity.HasOne(d => d.Order).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__OrderId__55F4C372");

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets).HasConstraintName("FK__Tickets__SeatId__57DD0BE4");

            entity.HasOne(d => d.TicketType).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__TicketT__55009F39");

            entity.HasOne(d => d.User).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tickets__UserId__56E8E7AB");
        });

        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketTy__3214EC07B27532AB");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Event).WithMany(p => p.TicketTypes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TicketTyp__Event__7F2BE32F");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07F132214A");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

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

        modelBuilder.Entity<UserEventInteraction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserEven__3214EC07A41AE895");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Event).WithMany(p => p.UserEventInteractions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserEvent__Event__10216507");

            entity.HasOne(d => d.User).WithMany(p => p.UserEventInteractions).HasConstraintName("FK__UserEvent__UserI__0F2D40CE");
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRefr__3214EC077BD6BB65");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRefre__UserI__44952D46");
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Venues__3214EC0721540A0D");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Venues).HasConstraintName("FK__Venues__CreatedB__6477ECF3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
