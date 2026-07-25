using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Extensions;
using Eventix.Modules.Admin.Interfaces;
using Eventix.Share.Admin;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.Admin.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalUsers      = await _context.Users.CountAsync();
            var activeUsers     = await _context.Users.CountAsync(u => u.Status == SystemConstants.StatusAccount.ACTIVE);
            var bannedUsers     = await _context.Users.CountAsync(u => u.Status == SystemConstants.StatusAccount.BANNED);
            var newUsersMonth   = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth);

            var totalOrgs       = await _context.OrganizerProfiles.CountAsync();
            var pendingOrgs     = await _context.OrganizerProfiles.CountAsync(o => o.Status == SystemConstants.OrganizerStatus.PENDING);
            var approvedOrgs    = await _context.OrganizerProfiles.CountAsync(o => o.Status == SystemConstants.OrganizerStatus.APPROVED);
            var rejectedOrgs    = await _context.OrganizerProfiles.CountAsync(o => o.Status == SystemConstants.OrganizerStatus.REJECTED);

            var totalEvents     = await _context.Events.CountAsync();
            var publishedEvents = await _context.Events.CountAsync(e => e.Status == SystemConstants.EventStatus.Published);
            var ongoingEvents   = await _context.Events.CountAsync(e => e.Status == SystemConstants.EventStatus.Ongoing);
            var completedEvents = await _context.Events.CountAsync(e => e.Status == SystemConstants.EventStatus.Completed);

            var totalRevenue    = await _context.Orders
                .Where(o => o.Status == SystemConstants.OrderStatus.PAID)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var revenueMonth    = await _context.Orders
                .Where(o => o.Status == SystemConstants.OrderStatus.PAID && o.PaidAt >= startOfMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalSold       = await _context.TicketTypes.SumAsync(t => (int?)t.SoldQuantity) ?? 0;
            var totalOrders     = await _context.Orders.CountAsync(o => o.Status == SystemConstants.OrderStatus.PAID);

            return new AdminDashboardStats
            {
                TotalUsers       = totalUsers,
                ActiveUsers      = activeUsers,
                BannedUsers      = bannedUsers,
                NewUsersThisMonth = newUsersMonth,

                TotalOrganizers   = totalOrgs,
                PendingOrganizers = pendingOrgs,
                ApprovedOrganizers = approvedOrgs,
                RejectedOrganizers = rejectedOrgs,

                TotalEvents     = totalEvents,
                PublishedEvents = publishedEvents,
                OngoingEvents   = ongoingEvents,
                CompletedEvents = completedEvents,

                TotalRevenue      = totalRevenue,
                RevenueThisMonth  = revenueMonth,
                TotalTicketsSold  = totalSold,
                TotalOrders       = totalOrders,
            };
        }

        // ── Users ─────────────────────────────────────────────────────────────

        public async Task<PaginationResponse<AdminUserResponse>> GetUsersAsync(AdminUserFilterRequest request)
        {
            var query = _context.Users
                .Include(u => u.Roles)
                .Include(u => u.OrganizerProfileUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(u => u.Status == request.Status);

            if (!string.IsNullOrWhiteSpace(request.Role))
                query = query.Where(u => u.Roles.Any(r => r.Name == request.Role));

            var projected = query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserResponse
                {
                    Id            = u.Id,
                    Email         = u.Email,
                    FullName      = u.FullName,
                    PhoneNumber   = u.PhoneNumber,
                    AvatarUrl     = u.AvatarUrl,
                    Status        = u.Status,
                    EmailVerified = u.EmailVerified,
                    CreatedAt     = u.CreatedAt,
                    UpdatedAt     = u.UpdatedAt,
                    Roles         = u.Roles.Select(r => r.Name).ToList(),
                    HasOrganizerProfile = u.OrganizerProfileUser != null,
                    OrganizerStatus     = u.OrganizerProfileUser != null
                        ? u.OrganizerProfileUser.Status
                        : null
                });

            return await projected.GetPaged(request.CurrentPage, request.PageSize);
        }

        public async Task<AdminUserResponse> BanUserAsync(Guid userId, string reason, Guid adminId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.OrganizerProfileUser)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException(SystemError.USER_NOT_FOUND);

            if (user.Roles.Any(r => r.Name == SystemConstants.RoleConstants.ADMIN))
                throw new BadRequestException("Cannot ban another admin.");

            user.Status    = SystemConstants.StatusAccount.BANNED;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapUser(user);
        }

        public async Task<AdminUserResponse> UnbanUserAsync(Guid userId, Guid adminId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.OrganizerProfileUser)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException(SystemError.USER_NOT_FOUND);

            user.Status    = SystemConstants.StatusAccount.ACTIVE;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapUser(user);
        }

        // ── Organizer requests ────────────────────────────────────────────────

        public async Task<PaginationResponse<AdminOrganizerDetailResponse>> GetOrganizerRequestsAsync(
            string? status, int page, int pageSize)
        {
            var query = _context.OrganizerProfiles
                .Include(o => o.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var projected = query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminOrganizerDetailResponse
                {
                    OrganizerProfileId = o.Id,
                    UserId             = o.UserId,
                    UserEmail          = o.User.Email,
                    UserFullName       = o.User.FullName,
                    UserAvatarUrl      = o.User.AvatarUrl,
                    OrganizationName   = o.OrganizationName,
                    Description        = o.Description,
                    ContactEmail       = o.ContactEmail,
                    ContactPhone       = o.ContactPhone,
                    Status             = o.Status,
                    CreatedAt          = o.CreatedAt,
                    ApprovedAt         = o.ApprovedAt,
                    ApprovedByName     = o.ApprovedByNavigation != null
                        ? o.ApprovedByNavigation.FullName : null,
                    TotalEvents      = _context.Events.Count(e => e.OrganizerId == o.Id),
                    PublishedEvents  = _context.Events.Count(e =>
                        e.OrganizerId == o.Id && e.Status == SystemConstants.EventStatus.Published),
                    TotalTicketsSold = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.TicketTypes)
                        .Sum(t => (int?)t.SoldQuantity) ?? 0,
                    TotalRevenue     = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.TicketTypes)
                        .Sum(t => (decimal?)(t.SoldQuantity * t.Price)) ?? 0
                });

            return await projected.GetPaged(page, pageSize);
        }

        public async Task<AdminOrganizerDetailResponse> GetOrganizerDetailAsync(Guid organizerProfileId)
        {
            var o = await _context.OrganizerProfiles
                .Include(x => x.User)
                .Include(x => x.ApprovedByNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == organizerProfileId)
                ?? throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            var events = await _context.Events
                .Where(e => e.OrganizerId == o.Id)
                .Include(e => e.TicketTypes)
                .AsNoTracking()
                .ToListAsync();

            return new AdminOrganizerDetailResponse
            {
                OrganizerProfileId = o.Id,
                UserId             = o.UserId,
                UserEmail          = o.User.Email,
                UserFullName       = o.User.FullName,
                UserAvatarUrl      = o.User.AvatarUrl,
                OrganizationName   = o.OrganizationName,
                Description        = o.Description,
                ContactEmail       = o.ContactEmail,
                ContactPhone       = o.ContactPhone,
                Status             = o.Status,
                CreatedAt          = o.CreatedAt,
                ApprovedAt         = o.ApprovedAt,
                ApprovedByName     = o.ApprovedByNavigation?.FullName,
                TotalEvents        = events.Count,
                PublishedEvents    = events.Count(e => e.Status == SystemConstants.EventStatus.Published),
                TotalTicketsSold   = events.SelectMany(e => e.TicketTypes).Sum(t => t.SoldQuantity),
                TotalRevenue       = events.SelectMany(e => e.TicketTypes).Sum(t => t.SoldQuantity * t.Price)
            };
        }

        public async Task<AdminOrganizerDetailResponse> ApproveOrganizerAsync(Guid organizerProfileId, Guid adminId)
        {
            var profile = await _context.OrganizerProfiles
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == organizerProfileId)
                ?? throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            profile.Status     = SystemConstants.OrganizerStatus.APPROVED;
            profile.ApprovedBy = adminId;
            profile.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetOrganizerDetailAsync(organizerProfileId);
        }

        public async Task<AdminOrganizerDetailResponse> RejectOrganizerAsync(Guid organizerProfileId, Guid adminId)
        {
            var profile = await _context.OrganizerProfiles
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == organizerProfileId)
                ?? throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            profile.Status     = SystemConstants.OrganizerStatus.REJECTED;
            profile.ApprovedBy = adminId;
            profile.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetOrganizerDetailAsync(organizerProfileId);
        }

        // ── Organizer stats list ──────────────────────────────────────────────

        public async Task<PaginationResponse<AdminOrganizerStatsResponse>> GetOrganizerStatsAsync(int page, int pageSize)
        {
            var query = _context.OrganizerProfiles
                .Include(o => o.User)
                .Where(o => o.Status == SystemConstants.OrganizerStatus.APPROVED)
                .AsNoTracking()
                .Select(o => new AdminOrganizerStatsResponse
                {
                    OrganizerProfileId = o.Id,
                    UserId             = o.UserId,
                    OrganizationName   = o.OrganizationName,
                    UserEmail          = o.User.Email,
                    UserFullName       = o.User.FullName,
                    ContactEmail       = o.ContactEmail,
                    Status             = o.Status,
                    CreatedAt          = o.CreatedAt,
                    TotalEvents        = _context.Events.Count(e => e.OrganizerId == o.Id),
                    PublishedEvents    = _context.Events.Count(e =>
                        e.OrganizerId == o.Id && e.Status == SystemConstants.EventStatus.Published),
                    TotalTicketsSold   = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.TicketTypes)
                        .Sum(t => (int?)t.SoldQuantity) ?? 0,
                    TotalRevenue       = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.TicketTypes)
                        .Sum(t => (decimal?)(t.SoldQuantity * t.Price)) ?? 0
                });

            return await query.GetPaged(page, pageSize);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static AdminUserResponse MapUser(Eventix.Entities.User u) => new()
        {
            Id                  = u.Id,
            Email               = u.Email,
            FullName            = u.FullName,
            PhoneNumber         = u.PhoneNumber,
            AvatarUrl           = u.AvatarUrl,
            Status              = u.Status,
            EmailVerified       = u.EmailVerified,
            CreatedAt           = u.CreatedAt,
            UpdatedAt           = u.UpdatedAt,
            Roles               = u.Roles.Select(r => r.Name).ToList(),
            HasOrganizerProfile = u.OrganizerProfileUser != null,
            OrganizerStatus     = u.OrganizerProfileUser?.Status
        };
    }
}
