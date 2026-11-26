using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using HotelErp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RateCalculator _calculator;

    public AvailabilityController(AppDbContext db, RateCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    [HttpGet]
    public async Task<ActionResult<AvailabilityDto>> Search(
        int propertyId, DateOnly checkIn, DateOnly checkOut,
        int adults = 1, int children = 0, int? ratePlanId = null)
    {
        var dateError = StayRules.ValidateDates(checkIn, checkOut);
        if (dateError is not null) return BadRequest(new { message = dateError });

        if (adults < 1 || children < 0)
            return BadRequest(new { message = "Adults must be at least 1 and children cannot be negative" });

        if (!await _db.Properties.AnyAsync(p => p.Id == propertyId && p.IsActive))
            return NotFound(new { message = "Property not found" });

        RatePlan? plan = null;
        if (ratePlanId.HasValue)
        {
            plan = await _db.RatePlans.AsNoTracking().Include(p => p.Rules)
                .FirstOrDefaultAsync(p => p.Id == ratePlanId.Value);
            if (plan is null) return NotFound(new { message = "Rate plan not found" });
            if (checkIn < plan.ValidFrom || checkOut.AddDays(-1) > plan.ValidTo)
                return BadRequest(new { message = $"This rate plan is only valid from {plan.ValidFrom} to {plan.ValidTo}" });
        }

        var blocking = ReservationStatus.Blocking;

        // How many sellable rooms of each type does this property have?
        var roomCounts = await _db.Rooms.AsNoTracking()
            .Where(r => r.PropertyId == propertyId && r.Status != RoomStatus.OutOfService)
            .GroupBy(r => r.RoomTypeId)
            .Select(g => new { RoomTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count);

        // How many booked items of each type overlap these dates?
        // (an item with no room yet counts against its room type)
        var reservedCounts = await _db.ReservationItems.AsNoTracking()
            .Where(i => blocking.Contains(i.Reservation.Status)
                     && i.Reservation.CheckInDate < checkOut
                     && checkIn < i.Reservation.CheckOutDate
                     && i.Reservation.PropertyId == propertyId)
            .GroupBy(i => i.RoomTypeId)
            .Select(g => new { RoomTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count);

        var roomTypes = await _db.RoomTypes.AsNoTracking()
            .Include(t => t.Amenities)
            .Where(t => t.MaxAdults >= adults && t.MaxChildren >= children)
            .OrderBy(t => t.BaseRate)
            .ToListAsync();

        var items = new List<RoomTypeAvailabilityDto>();
        foreach (var t in roomTypes)
        {
            roomCounts.TryGetValue(t.Id, out var total);
            if (total == 0) continue;   // this property has no sellable rooms of this type

            reservedCounts.TryGetValue(t.Id, out var reserved);
            var available = Math.Max(0, total - reserved);

            QuoteSummaryDto? quote = null;
            if (plan is not null && available > 0)
            {
                var q = _calculator.Calculate(t, plan, checkIn, checkOut);
                quote = new QuoteSummaryDto(plan.Id, plan.Name, q.Total, q.AverageNightly);
            }

            items.Add(new RoomTypeAvailabilityDto(
                t.Id, t.Name, t.Description, t.BedType, t.BedCount, t.MaxAdults, t.MaxChildren, t.BaseRate,
                t.Amenities.OrderBy(a => a.Name).Select(a => new AmenityDto(a.Id, a.Name, a.Category)).ToList(),
                total, reserved, available, quote));
        }

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return new AvailabilityDto(propertyId, checkIn, checkOut, nights, items);
    }

    // The individual rooms of one type that no booking has taken for these dates
    [HttpGet("rooms")]
    public async Task<ActionResult<List<FreeRoomDto>>> FreeRooms(
        int propertyId, int roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var dateError = StayRules.ValidateDates(checkIn, checkOut);
        if (dateError is not null) return BadRequest(new { message = dateError });

        if (!await _db.Properties.AnyAsync(p => p.Id == propertyId && p.IsActive))
            return NotFound(new { message = "Property not found" });
        if (!await _db.RoomTypes.AnyAsync(t => t.Id == roomTypeId))
            return NotFound(new { message = "Room type not found" });

        var blocking = ReservationStatus.Blocking;

        var taken = _db.ReservationItems.AsNoTracking()
            .Where(i => i.RoomId != null
                     && blocking.Contains(i.Reservation.Status)
                     && i.Reservation.CheckInDate < checkOut
                     && checkIn < i.Reservation.CheckOutDate)
            .Select(i => i.RoomId!.Value);

        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.PropertyId == propertyId
                     && r.RoomTypeId == roomTypeId
                     && r.Status != RoomStatus.OutOfService
                     && !taken.Contains(r.Id))
            .OrderBy(r => r.RoomNo)
            .ToListAsync();

        return rooms
            .Select(r => new FreeRoomDto(r.Id, r.RoomNo, r.Floor, r.Status.ToString(), r.Status == RoomStatus.Clean))
            .ToList();
    }
}