namespace Eventix.Share.Admin
{
    /// <summary>
    /// Chi tiết hồ sơ organizer kèm thống kê — dành cho admin.
    /// </summary>
    public class AdminOrganizerDetailResponse
    {
        // ── Hồ sơ tổ chức ────────────────────────────────────────────────────
        public Guid OrganizerProfileId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
        public string? UserAvatarUrl { get; set; }
        public string OrganizationName { get; set; } = null!;
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }

        // ── Thống kê ─────────────────────────────────────────────────────────
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
