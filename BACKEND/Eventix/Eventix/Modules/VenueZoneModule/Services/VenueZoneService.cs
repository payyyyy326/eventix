using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Modules.VenueZoneModule.Interfaces;
using Eventix.Share.VenueZone;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.VenueZoneModule.Services
{
    public class VenueZoneService : IVenueZoneService
    {
        private readonly AppDbContext _context;

        public VenueZoneService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VenueZoneResponse>> GetZonesByVenueAsync(Guid venueId)
        {
            var venueExists = await _context.Venues.AnyAsync(v => v.Id == venueId);

            if (!venueExists)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            return await _context.VenueZones
                .Where(z => z.VenueId == venueId)
                .OrderBy(z => z.SortOrder)
                .ThenBy(z => z.Name)
                .Select(z => new VenueZoneResponse
                {
                    Id = z.Id,
                    VenueId = z.VenueId,
                    Name = z.Name,
                    HasSeats = z.HasSeats,
                    Capacity = z.Capacity,
                    Color = z.Color,
                    SortOrder = z.SortOrder,
                    SeatCount = z.Seats.Count
                })
                .ToListAsync();
        }

        public async Task<VenueZoneResponse> CreateZoneAsync(Guid venueId, CreateVenueZoneRequest request)
        {
            var venue = await _context.Venues
              .FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            if (request.Capacity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var capacity = request.HasSeats ? 0 : request.Capacity;

            if (!request.HasSeats && capacity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            if (!request.HasSeats)
            {
                var currentTotalCapacity = await _context.VenueZones
                    .Where(z => z.VenueId == venueId)
                    .SumAsync(z => z.Capacity);

                if (currentTotalCapacity + capacity > venue.Capacity)
                    throw new BadRequestException(SystemError.INVALID_QUANTITY);
            }

            var name = request.Name.Trim();

            var duplicated = await _context.VenueZones.AnyAsync(z =>
                z.VenueId == venueId &&
                z.Name == name);

            if (duplicated)
                throw new BadRequestException(SystemError.DUPLICATE_DATA);


            var zone = new VenueZone
            {
                Id = Guid.NewGuid(),
                VenueId = venueId,
                Name = name,
                HasSeats = request.HasSeats,
                Capacity = capacity,
                Color = string.IsNullOrWhiteSpace(request.Color)
                    ? "#60A5FA"
                    : request.Color,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            await _context.VenueZones.AddAsync(zone);
            await _context.SaveChangesAsync();

            return new VenueZoneResponse
            {
                Id = zone.Id,
                VenueId = zone.VenueId,
                Name = zone.Name,
                HasSeats = zone.HasSeats,
                Capacity = zone.Capacity,
                Color = zone.Color,
                SortOrder = zone.SortOrder,
                SeatCount = 0
            };
        }

        public async Task<VenueZoneResponse> UpdateZoneAsync(Guid zoneId, UpdateVenueZoneRequest request)
        {
            var zone = await _context.VenueZones
                .Include(z => z.Seats)
                .FirstOrDefaultAsync(z => z.Id == zoneId);

            if (zone == null)
                throw new BadRequestException(SystemError.ZONE_NOT_FOUND);

            if (request.Capacity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var venueCapacity = await _context.Venues
                .Where(v => v.Id == zone.VenueId)
                .Select(v => v.Capacity)
                .FirstAsync();

            var capacity = request.HasSeats ? zone.Seats.Count : request.Capacity;

            if (!request.HasSeats && capacity <= 0)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            var otherZonesCapacity = await _context.VenueZones
                .Where(z => z.VenueId == zone.VenueId && z.Id != zoneId)
                .SumAsync(z => z.Capacity);

            if (otherZonesCapacity + capacity > venueCapacity)
                throw new BadRequestException(SystemError.INVALID_QUANTITY);

            zone.Name = request.Name.Trim();
            zone.HasSeats = request.HasSeats;
            zone.Capacity = capacity;
            zone.Color = string.IsNullOrWhiteSpace(request.Color)
                ? "#60A5FA"
                : request.Color;
            zone.SortOrder = request.SortOrder;
            zone.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new VenueZoneResponse
            {
                Id = zone.Id,
                VenueId = zone.VenueId,
                Name = zone.Name,
                HasSeats = zone.HasSeats,
                Capacity = zone.Capacity,
                Color = zone.Color,
                SortOrder = zone.SortOrder,
                SeatCount = zone.Seats.Count
            };
        }

        public async Task<List<SeatImportStatusResponse>> GetSeatImportStatusAsync(Guid venueId)
        {
            var venueExists = await _context.Venues
                .AnyAsync(v => v.Id == venueId);

            if (!venueExists)
                throw new BadRequestException(SystemError.VENUE_NOT_FOUND);

            return await _context.VenueZones
                .Where(z => z.VenueId == venueId)
                .OrderBy(z => z.SortOrder)
                .ThenBy(z => z.Name)
                .Select(z => new SeatImportStatusResponse
                {
                    VenueZoneId = z.Id,

                    ZoneName = z.Name,

                    HasSeats = z.HasSeats,

                    Capacity = z.Capacity,

                    ImportedSeats = z.Seats.Count,

                    Completed =
                        !z.HasSeats ||
                        z.Seats.Count == z.Capacity
                })
                .ToListAsync();
        }

        public async Task<List<ZoneAvailableCapacityResponse>> GetZoneAvailableCapacityAsync(Guid eventId, Guid userId)
        {
            // Validate organizer quyền truy cập event
            var organizer = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizer == null)
                throw new BadRequestException(SystemError.ORGANIZER_NOT_FOUND);

            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizer.Id);

            if (eventEntity == null)
                throw new NotFoundException(SystemError.EVENT_NOT_FOUND);

            // Lấy tất cả zone của venue thuộc event này
            var zones = await _context.VenueZones
                .AsNoTracking()
                .Where(z => z.VenueId == eventEntity.VenueId)
                .OrderBy(z => z.SortOrder)
                .ThenBy(z => z.Name)
                .ToListAsync();

            if (!zones.Any())
                return new List<ZoneAvailableCapacityResponse>();

            var zoneIds = zones.Select(z => z.Id).ToList();

            // Lấy tổng Quantity đã được allocate cho từng zone trong event này
            var allocatedPerZone = await _context.TicketTypes
                .AsNoTracking()
                .Where(tt => tt.EventId == eventId && tt.VenueZoneId != null && zoneIds.Contains(tt.VenueZoneId.Value))
                .GroupBy(tt => tt.VenueZoneId!.Value)
                .Select(g => new { ZoneId = g.Key, AllocatedQuantity = g.Sum(tt => tt.Quantity) })
                .ToDictionaryAsync(x => x.ZoneId, x => x.AllocatedQuantity);

            return zones.Select(z =>
            {
                var allocated = allocatedPerZone.TryGetValue(z.Id, out var qty) ? qty : 0;
                var available = Math.Max(0, z.Capacity - allocated);

                return new ZoneAvailableCapacityResponse
                {
                    VenueZoneId = z.Id,
                    ZoneName = z.Name,
                    HasSeats = z.HasSeats,
                    Capacity = z.Capacity,
                    AllocatedQuantity = allocated,
                    AvailableSlots = available
                };
            }).ToList();
        }
    }
}