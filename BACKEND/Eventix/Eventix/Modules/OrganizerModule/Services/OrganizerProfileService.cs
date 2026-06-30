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

            if (!admin) throw new BadRequestException(SystemError.UNAUTHORIZED);

            var organizerProfile = _context.OrganizerProfiles
                .FirstOrDefault(x => x.Id == organizerProfileId);
            if (organizerProfile == null) throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

            organizerProfile.Status = OrganizerStatus.APPROVED;
            organizerProfile.ApprovedBy = adminId;
            organizerProfile.ApprovedAt = DateTime.UtcNow;

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
                CreatedAt = organizerProfile.CreatedAt,
                ApprovedBy = organizerProfile.ApprovedBy,
                ApprovedAt = organizerProfile.ApprovedAt,
                ApprovedByNavigation = new UserResponse
                {
                    Id = adminId,
                    FullName = _context.Users.Where(u => u.Id == adminId).Select(u => u.FullName).FirstOrDefault() ?? RoleConstants.ADMIN
                }
            };
            return response;
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

            if (organizerProfile == null) throw new NotFoundException(SystemError.ORGANIZER_NOT_FOUND);

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
    }
}
