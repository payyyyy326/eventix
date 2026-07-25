using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.OrganizerModule.Interfaces;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Eventix.Share.Event;
using Eventix.Share.Organizer;
using Eventix.Share.User;
using Microsoft.EntityFrameworkCore;
using static Eventix.Share.Common.Constants.SystemConstants;

namespace Eventix.Modules.OrganizerModule.Services
{
    public class OrganizerProfileService : IOrganizerProfileService
    {
        private readonly AppDbContext _context;

        public OrganizerProfileService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<OrganizerProfileResponse> ApproveAsync(Guid organizerProfileId, Guid adminId)
        {
            var admin = await _context.Users
                .Include(a => a.Roles)
                .AnyAsync(x => x.Id == adminId && x.Roles.Any(r => r.Name == RoleConstants.ADMIN));

            if (!admin) throw new ForbiddenException(SystemError.UNAUTHORIZED);

            var organizerProfile = await _context.OrganizerProfiles
                .Include(o => o.User)
                    .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(x => x.Id == organizerProfileId);

            if (organizerProfile == null) throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            organizerProfile.Status    = OrganizerStatus.APPROVED;
            organizerProfile.ApprovedBy = adminId;
            organizerProfile.ApprovedAt = DateTime.UtcNow;

            // Thêm role Organizer cho user nếu chưa có
            var user = organizerProfile.User;
            if (user != null && !user.Roles.Any(r => r.Name == RoleConstants.ORGANIZER))
            {
                var organizerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == RoleConstants.ORGANIZER);

                if (organizerRole != null)
                    user.Roles.Add(organizerRole);
            }

            await _context.SaveChangesAsync();

            var adminName = await _context.Users
                .Where(u => u.Id == adminId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? RoleConstants.ADMIN;

            return new OrganizerProfileResponse
            {
                Id               = organizerProfile.Id,
                UserId           = organizerProfile.UserId,
                OrganizationName = organizerProfile.OrganizationName,
                Description      = organizerProfile.Description,
                ContactEmail     = organizerProfile.ContactEmail,
                ContactPhone     = organizerProfile.ContactPhone,
                Status           = organizerProfile.Status,
                CreatedAt        = organizerProfile.CreatedAt,
                ApprovedBy       = organizerProfile.ApprovedBy,
                ApprovedAt       = organizerProfile.ApprovedAt,
                ApprovedByNavigation = new UserResponse
                {
                    Id       = adminId,
                    FullName = adminName
                }
            };
        }

        public async Task<OrganizerProfileResponse> CreateAsync(CreateOrganizerProfileRequest request, Guid userId)
        {
            var exists = await _context.OrganizerProfiles
             .AnyAsync(x => x.UserId == userId);

            if (exists) throw new BadRequestException(SystemError.ORGANIZER_EXIST);

            var organizerProfile = new OrganizerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationName = request.OrganizationName,
                Description = request.Description,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Status = SystemConstants.OrganizerStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
            };

            _context.OrganizerProfiles.Add(organizerProfile);
            await _context.SaveChangesAsync();

            var response = new OrganizerProfileResponse
            {
                Id = organizerProfile.Id,
                UserId = organizerProfile.UserId,
                OrganizationName = organizerProfile.OrganizationName,
                Description = organizerProfile.Description,
                ContactEmail = organizerProfile.ContactEmail,
                ContactPhone = organizerProfile.ContactPhone,
                Status = organizerProfile.Status,
                CreatedAt = organizerProfile.CreatedAt
            };
            return response;
        }
        public async Task<PaginationResponse<OrganizerEventResponse>> GetEventsByOrganizerAsync(Guid userId, PaginationRequest<OrganizerEventResponse> request)
        {
            var organizer = await _context.OrganizerProfiles.FirstOrDefaultAsync(o => o.UserId == userId);
            if (organizer == null) throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var events = _context.Events
                .Where(e => e.OrganizerId == organizer.Id);

            var eventResponse = events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new OrganizerEventResponse
                {
                    Id = e.Id,
                    Title = e.Title,
                    Slug = e.Slug,
                    ImageUrl = e.ImageUrl,

                    StartTime = e.StartTime,
                    EndTime = e.EndTime,

                    Status = e.Status,
                    ViewCount = e.ViewCount,
                    IsFeatured = e.IsFeatured,

                    CategoryName = e.Category.Name,
                    VenueName = e.Venue.Name,

                    TotalTicketTypes = e.TicketTypes.Count(),
                    TotalTicketsSold = e.TicketTypes.Sum(t => t.SoldQuantity),
                    TotalRevenue = e.TicketTypes.Sum(t => t.SoldQuantity * t.Price),

                    CreatedAt = e.CreatedAt,
                    PublishedAt = e.PublishedAt
                });

            return await eventResponse.GetPaged(request.CurrentPage, request.PageSize);
        }
        public async Task<OrganizerEventDetailResponse> GetOrganizerEventDetailAsync(Guid userId, Guid eventId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e =>
                    e.Id == eventId &&
                    e.OrganizerId == organizer.Id);

            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            var totalTickets = eventEntity.TicketTypes.Sum(t => t.Quantity);
            var ticketsSold = eventEntity.TicketTypes.Sum(t => t.SoldQuantity);
            var ticketsReserved = eventEntity.TicketTypes.Sum(t => t.ReservedQuantity);

            return new OrganizerEventDetailResponse
            {
                Id = eventEntity.Id,

                Title = eventEntity.Title,
                Slug = eventEntity.Slug,

                Summary = eventEntity.Summary,
                Description = eventEntity.Description,

                ImageUrl = eventEntity.ImageUrl,
                BannerUrl = eventEntity.BannerUrl,

                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,

                Status = eventEntity.Status,

                IsFeatured = eventEntity.IsFeatured,
                ViewCount = eventEntity.ViewCount,

                CategoryName = eventEntity.Category.Name,
                VenueId = eventEntity.VenueId,
                VenueName = eventEntity.Venue.Name,
                VenueCity = eventEntity.Venue.City,

                TicketTypeCount = eventEntity.TicketTypes.Count,
                TotalTickets = totalTickets,
                TicketsSold = ticketsSold,
                TicketsReserved = ticketsReserved,
                TicketsRemaining = totalTickets - ticketsSold - ticketsReserved,

                Revenue = eventEntity.TicketTypes
                    .Sum(t => t.Price * t.SoldQuantity),

                CreatedAt = eventEntity.CreatedAt,
                PublishedAt = eventEntity.PublishedAt
            };
        }
        public async Task<PaginationResponse<OrganizerProfileResponse>> GetAllAsync(string? status, PaginationRequest<OrganizerProfileResponse> request)
        {
            var organizers = _context.OrganizerProfiles.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                organizers = organizers.Where(x => x.Status == status);
            }

            var responseOrganizers = organizers
                .AsNoTracking()
                .Select(x => new OrganizerProfileResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    OrganizationName = x.OrganizationName,
                    Description = x.Description,
                    ContactEmail = x.ContactEmail,
                    ContactPhone = x.ContactPhone,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                });

            var response = await responseOrganizers
                .GetPaged(request.CurrentPage, request.PageSize);

            return response;
        }

        public async Task<OrganizerProfileResponse> GetMyProfileAsync(Guid userId)
        {
            var organizerProfile = await _context.OrganizerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (organizerProfile == null)
                throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            return new OrganizerProfileResponse
            {
                Id = organizerProfile.Id,
                UserId = organizerProfile.UserId,
                OrganizationName = organizerProfile.OrganizationName,
                Description = organizerProfile.Description,
                ContactEmail = organizerProfile.ContactEmail,
                ContactPhone = organizerProfile.ContactPhone,
                Status = organizerProfile.Status,
                ApprovedBy = organizerProfile.ApprovedBy,
                ApprovedAt = organizerProfile.ApprovedAt,
                CreatedAt = organizerProfile.CreatedAt
            };
        }
        public async Task<OrganizerProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateOrganizerProfileRequest request)
        {
            var profile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            var organizationName = request.OrganizationName.Trim();

            var duplicateName = await _context.OrganizerProfiles
                .AnyAsync(x =>
                    x.Id != profile.Id &&
                    x.OrganizationName.ToLower() == organizationName.ToLower());

            if (duplicateName)
            {
                throw new BadRequestException(
                    "Organization name already exists.");
            }

            profile.OrganizationName = organizationName;
            profile.Description = request.Description?.Trim();
            profile.ContactEmail = request.ContactEmail?.Trim();
            profile.ContactPhone = request.ContactPhone?.Trim();

            await _context.SaveChangesAsync();

            return new OrganizerProfileResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                OrganizationName = profile.OrganizationName,
                Description = profile.Description,
                ContactEmail = profile.ContactEmail,
                ContactPhone = profile.ContactPhone,
                Status = profile.Status,
                ApprovedBy = profile.ApprovedBy,
                ApprovedAt = profile.ApprovedAt,
                CreatedAt = profile.CreatedAt
            };
        }
        public async Task<OrganizerProfileResponse> RejectAsync(Guid organizerProfileId, Guid adminId)
        {
            var organizer = await _context.OrganizerProfiles
            .FirstOrDefaultAsync(x => x.Id == organizerProfileId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            organizer.Status = OrganizerStatus.REJECTED;
            organizer.ApprovedBy = adminId;
            organizer.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new OrganizerProfileResponse
            {
                Id = organizer.Id,
                UserId = organizer.UserId,
                OrganizationName = organizer.OrganizationName,
                Description = organizer.Description,
                ContactEmail = organizer.ContactEmail,
                ContactPhone = organizer.ContactPhone,
                Status = organizer.Status,
                CreatedAt = organizer.CreatedAt,
                ApprovedBy = organizer.ApprovedBy,
                ApprovedAt = organizer.ApprovedAt
            };
            return response;
        }
        public async Task<List<string>> GetEventSectionsAsync(Guid userId, Guid eventId)
        {
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.Id == eventId &&
                    e.OrganizerId == organizer.Id);

            if (eventEntity == null)
                throw new BadRequestException(SystemError.EVENT_NOT_FOUND);

            return await _context.Seats
                .Where(s => s.VenueId == eventEntity.VenueId)
                .Where(s => !string.IsNullOrWhiteSpace(s.Section))
                .Select(s => s.Section!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }
    }
}
