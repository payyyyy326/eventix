using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.VenueModule.Interfaces;
using Eventix.Share.Common.Models;
using Eventix.Share.SeatMap;
using Eventix.Share.User;
using Eventix.Share.Venue;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.VenueModule.Services
{
    public class VenueService : IVenueService
    {
        private readonly AppDbContext _context;

        public VenueService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request, Guid userId)
        {
            var venueExist = await _context.Venues.FirstOrDefaultAsync(v => v.Name.ToLower() == request.Name.ToLower() && v.Address.ToLower() == request.Address.ToLower());
            if (venueExist != null)
            {
                throw new BadRequestException(SystemError.VENUE_EXIST);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newVenue = new Venue
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Address = request.Address,
                    City = request.City,
                    Capacity = request.Capacity,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Venues.Add(newVenue);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new VenueResponse
                {
                    Id = newVenue.Id,
                    Name = newVenue.Name,
                    Address = newVenue.Address,
                    City = newVenue.City,
                    Capacity = newVenue.Capacity,
                    CreatedBy = newVenue.CreatedBy,
                    CreatedAt = newVenue.CreatedAt,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber
                    }
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task DeleteVenueAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<PaginationResponse<VenueResponse>> GetAllVenuesAsync(PaginationRequest<VenueResponse> request, string? search = null)
        {
            var query = _context.Venues.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(v =>
                    v.Name.ToLower().Contains(s) ||
                    v.Address.ToLower().Contains(s) ||
                    (v.City != null && v.City.ToLower().Contains(s)));
            }

            var venues = query.Select(v => new VenueResponse
            {
                Id = v.Id,
                Name = v.Name,
                Address = v.Address,
                City = v.City,
                Capacity = v.Capacity,
                CreatedBy = v.CreatedBy,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                UpdatedBy = v.UpdatedBy,
                User = new UserResponse
                {
                    Id = v.CreatedByNavigation.Id,
                    Email = v.CreatedByNavigation.Email,
                    FullName = v.CreatedByNavigation.FullName,
                    PhoneNumber = v.CreatedByNavigation.PhoneNumber
                }
            });

            return await venues.GetPaged(request.CurrentPage, request.PageSize);
        }

        public async Task<VenueResponse> GetVenueByIdAsync(Guid id)
        {
            var venue = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (venue == null)
            {
                throw new NotFoundException(SystemError.VENUE_NOT_FOUND);
            }

            return new VenueResponse
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                City = venue.City,
                Capacity = venue.Capacity,
                CreatedBy = venue.CreatedBy,
                CreatedAt = venue.CreatedAt,
                UpdatedAt = venue.UpdatedAt,
                UpdatedBy = venue.UpdatedBy,
                User = new UserResponse
                {
                    Id = venue.CreatedByNavigation?.Id ?? Guid.Empty,
                    Email = venue.CreatedByNavigation?.Email ?? string.Empty,
                    FullName = venue.CreatedByNavigation?.FullName ?? string.Empty,
                    PhoneNumber = venue.CreatedByNavigation?.PhoneNumber
                }
            };
        }

        public async Task<VenueResponse> UpdateVenueAsync(Guid id, UpdateVenueRequest request, Guid userId)
        {
            var venueExist = await _context.Venues
                .Include(v => v.CreatedByNavigation)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (venueExist == null)
            {
                throw new NotFoundException(SystemError.VENUE_NOT_FOUND);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                venueExist.Name = request.Name;
                venueExist.Address = request.Address;
                venueExist.City = request.City;
                venueExist.Capacity = request.Capacity;
                venueExist.UpdatedAt = DateTime.UtcNow;
                venueExist.UpdatedBy = userId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new VenueResponse
                {
                    Id = venueExist.Id,
                    Name = venueExist.Name,
                    Address = venueExist.Address,
                    City = venueExist.City,
                    Capacity = venueExist.Capacity,
                    CreatedBy = venueExist.CreatedBy,
                    CreatedAt = venueExist.CreatedAt,
                    UpdatedAt = venueExist.UpdatedAt,
                    UpdatedBy = venueExist.UpdatedBy,
                    User = new UserResponse
                    {
                        Id = venueExist.CreatedByNavigation?.Id ?? Guid.Empty,
                        Email = venueExist.CreatedByNavigation?.Email ?? string.Empty,
                        FullName = venueExist.CreatedByNavigation?.FullName ?? string.Empty,
                        PhoneNumber = venueExist.CreatedByNavigation?.PhoneNumber
                    }
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;

            }
        }

        public async Task<List<VenueSectionLayoutResponse>> GetSeatMapAsync(Guid venueId)
        {
            var venueExists = await _context.Venues.AnyAsync(v => v.Id == venueId);

            if (!venueExists)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            return await _context.VenueSectionLayouts
                .Where(x => x.VenueId == venueId)
                .OrderBy(x => x.Section)
                .Select(x => new VenueSectionLayoutResponse
                {
                    Id = x.Id,
                    VenueId = x.VenueId,
                    TicketTypeId = x.TicketTypeId,
                    Section = x.Section,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    Color = x.Color,
                    IsSeatRequired = x.TicketType != null && x.TicketType.IsSeatRequired,
                    AvailableSeats = x.TicketType == null ? null :
                        x.TicketType.Quantity - x.TicketType.SoldQuantity - x.TicketType.ReservedQuantity,
                    TotalSeats = x.TicketType == null ? null : x.TicketType.Quantity
                })
                .ToListAsync();
        }

        public async Task<List<VenueSectionLayoutResponse>> SaveSeatMapAsync(Guid venueId, List<VenueSectionLayoutRequest> request, Guid userId)
        {
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            // Xóa layout cũ của venue
            var oldLayouts = await _context.VenueSectionLayouts
                .Where(x => x.VenueId == venueId)
                .ToListAsync();

            _context.VenueSectionLayouts.RemoveRange(oldLayouts);

            // Tìm zone tương ứng nếu có (optional - luồng mới không cần zone)
            var zones = await _context.VenueZones
                .Where(z => z.VenueId == venueId)
                .ToListAsync();

            var layouts = request.Select(x =>
            {
                var zone = zones.FirstOrDefault(z => z.Name == x.Section);

                return new VenueSectionLayout
                {
                    Id = Guid.NewGuid(),
                    VenueId = venueId,
                    VenueZoneId = zone?.Id,   // null nếu không có zone (luồng mới)
                    TicketTypeId = null,       // sẽ được liên kết khi event publish
                    Section = x.Section,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    Color = x.Color,
                    CreatedAt = DateTime.UtcNow
                };
            }).ToList();

            await _context.VenueSectionLayouts.AddRangeAsync(layouts);

            venue.UpdatedAt = DateTime.UtcNow;
            venue.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return layouts.Select(x => new VenueSectionLayoutResponse
            {
                Id = x.Id,
                VenueId = x.VenueId,
                Section = x.Section,
                X = x.X,
                Y = x.Y,
                Width = x.Width,
                Height = x.Height,
                Color = x.Color
            }).ToList();
        }
    }
}
