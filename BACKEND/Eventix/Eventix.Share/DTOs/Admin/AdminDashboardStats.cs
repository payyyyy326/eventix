namespace Eventix.Share.Admin
{
    /// <summary>
    /// Thống kê tổng quan hệ thống dành cho admin dashboard.
    /// </summary>
    public class AdminDashboardStats
    {
        // ── Người dùng ────────────────────────────────────────────────────────
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BannedUsers { get; set; }
        public int NewUsersThisMonth { get; set; }

        // ── Organizer ─────────────────────────────────────────────────────────
        public int TotalOrganizers { get; set; }
        public int PendingOrganizers { get; set; }
        public int ApprovedOrganizers { get; set; }
        public int RejectedOrganizers { get; set; }

        // ── Sự kiện ───────────────────────────────────────────────────────────
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int CompletedEvents { get; set; }

        // ── Doanh thu ─────────────────────────────────────────────────────────
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalOrders { get; set; }
    }

    /// <summary>
    /// Thống kê của một organizer cụ thể — dùng trong danh sách organizer của admin.
    /// </summary>
    public class AdminOrganizerStatsResponse
    {
        public Guid OrganizerProfileId { get; set; }
        public Guid UserId { get; set; }
        public string OrganizationName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
        public string? ContactEmail { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>Request ban/unban user.</summary>
    public class AdminBanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>Request filter danh sách users.</summary>
    public class AdminUserFilterRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Role { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
