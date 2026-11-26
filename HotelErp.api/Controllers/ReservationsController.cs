using System.Security.Claims;
using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using HotelErp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RateCalculator _calculator;

    public ReservationsController(AppDbContext db, RateCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    // ===== List =====

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationListItemDto>>> Search(
        int? propertyId, int? guestId, string? status, string? q,
        DateOnly? arrivingFrom, DateOnly? arrivingTo, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Reservations.AsNoTracking().AsQueryable();

        if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);
        if (guestId.HasValue) query = query.Where(r => r.GuestId == guestId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var known = ReservationStatus.Normalize(status);
            if (known is null)
                return BadRequest(new { message = "Unknown status. Use Confirmed, CheckedIn, CheckedOut or Cancelled" });
            query = query.Where(r => r.Status == known);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.ReservationNo, term) || EF.Functions.ILike(r.Guest.FullName, term));
        }

        if (arrivingFrom.HasValue) query = query.Where(r => r.CheckInDate >= arrivingFrom.Value);
        if (arrivingTo.HasValue) query = query.Where(r => r.CheckInDate <= arrivingTo.Value);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(r => r.CheckInDate).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.ReservationNo,
                GuestName = r.Guest.FullName,
                r.Source,
                r.CheckInDate,
                r.CheckOutDate,
                r.Status,
                r.TotalAmount,
                RoomCount = r.Items.Count
            })
            .ToListAsync();

        var items = rows.Select(r => new ReservationListItemDto(
            r.Id, r.ReservationNo, r.GuestName, r.Source.ToString(), r.CheckInDate, r.CheckOutDate,
            r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber, r.Status, r.TotalAmount, r.RoomCount)).ToList();

        return new PagedResult<ReservationListItemDto>(items, total, page, pageSize);
    }

    // ===== One reservation =====

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> Get(int id)
    {
        var reservation = await LoadFull(id);
        if (reservation is null) return NotFound(new { message = "Reservation not found" });
        return ToDto(reservation);
    }

    // ===== Create =====

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(ReservationCreateRequest req)
    {
        // 1. Checks that need no database
        var dateError = StayRules.ValidateDates(req.CheckIn, req.CheckOut);
        if (dateError is not null) return BadRequest(new { message = dateError });

        if (!Enum.TryParse<BookingSource>(req.Source, true, out var source) || !Enum.IsDefined(source))
            return BadRequest(new { message = "Unknown source. Use Phone, WalkIn or Online" });

        var today = StayRules.Today();
        if (req.CheckIn < today)
            return BadRequest(new { message = "Check-in cannot be in the past" });

        if (source == BookingSource.WalkIn && req.CheckIn != today)
            return BadRequest(new { message = "A walk-in guest must check in today" });

        // 2. Checks that look things up
        if (!await _db.Properties.AnyAsync(p => p.Id == req.PropertyId && p.IsActive))
            return NotFound(new { message = "Property not found" });

        if (!await _db.Guests.AnyAsync(g => g.Id == req.GuestId))
            return NotFound(new { message = "Guest not found" });

        var typeIds = req.Items.Select(i => i.RoomTypeId).Distinct().ToList();
        var planIds = req.Items.Select(i => i.RatePlanId).Distinct().ToList();

        var roomTypes = await _db.RoomTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var plans = await _db.RatePlans.AsNoTracking().Include(p => p.Rules)
            .Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        if (roomTypes.Count != typeIds.Count)
            return BadRequest(new { message = "One or more room types do not exist" });
        if (plans.Count != planIds.Count)
            return BadRequest(new { message = "One or more rate plans do not exist" });

        foreach (var plan in plans.Values)
        {
            if (req.CheckIn < plan.ValidFrom || req.CheckOut.AddDays(-1) > plan.ValidTo)
                return BadRequest(new { message = $"Rate plan '{plan.Name}' is only valid from {plan.ValidFrom} to {plan.ValidTo}" });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 3. Check availability and save, one booking at a time for this hotel
        await using var tx = await _db.Database.BeginTransactionAsync();

        // The lock is released automatically when the transaction ends
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)req.PropertyId})");

        var wanted = req.Items
            .GroupBy(i => i.RoomTypeId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        foreach (var (roomTypeId, quantity) in wanted)
        {
            var free = await CountAvailable(req.PropertyId, roomTypeId, req.CheckIn, req.CheckOut);
            if (free < quantity)
                return Conflict(new
                {
                    message = $"Only {free} '{roomTypes[roomTypeId].Name}' room(s) available for these dates, but {quantity} requested"
                });
        }

        var nights = req.CheckOut.DayNumber - req.CheckIn.DayNumber;

        var reservation = new Reservation
        {
            ReservationNo = $"TMP-{Guid.NewGuid():N}",   // replaced below, once we know the id
            PropertyId = req.PropertyId,
            GuestId = req.GuestId,
            Source = source,
            CheckInDate = req.CheckIn,
            CheckOutDate = req.CheckOut,
            Status = ReservationStatus.Confirmed,
            CreatedById = userId
        };

        foreach (var line in req.Items)
        {
            var quote = _calculator.Calculate(roomTypes[line.RoomTypeId], plans[line.RatePlanId], req.CheckIn, req.CheckOut);
            for (var n = 0; n < line.Quantity; n++)
            {
                reservation.Items.Add(new ReservationItem
                {
                    RoomTypeId = line.RoomTypeId,
                    RatePlanId = line.RatePlanId,
                    Nights = nights,
                    NightlyRate = quote.AverageNightly,
                    Total = quote.Total
                });
            }
        }
        reservation.TotalAmount = reservation.Items.Sum(i => i.Total);

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();   // now the reservation has an id

        reservation.ReservationNo = $"RES-{today.Year}-{reservation.Id:D6}";
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        var created = await LoadFull(reservation.Id);
        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, ToDto(created!));
    }

    // ===== Cancel =====

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(int id)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation is null) return NotFound(new { message = "Reservation not found" });

        if (reservation.Status != ReservationStatus.Confirmed)
            return Conflict(new { message = $"A {reservation.Status} reservation cannot be cancelled" });

        reservation.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync();

        var updated = await LoadFull(id);
        return ToDto(updated!);
    }

    // ===== helpers =====

    private async Task<int> CountAvailable(int propertyId, int roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var totalRooms = await _db.Rooms.CountAsync(r =>
            r.PropertyId == propertyId && r.RoomTypeId == roomTypeId && r.Status != RoomStatus.OutOfService);

        var blocking = ReservationStatus.Blocking;
        var reserved = await _db.ReservationItems.CountAsync(i =>
            i.RoomTypeId == roomTypeId
            && i.Reservation.PropertyId == propertyId
            && blocking.Contains(i.Reservation.Status)
            && i.Reservation.CheckInDate < checkOut
            && checkIn < i.Reservation.CheckOutDate);

        return Math.Max(0, totalRooms - reserved);
    }

    private Task<Reservation?> LoadFull(int id) =>
        _db.Reservations.AsNoTracking()
            .Include(r => r.Property)
            .Include(r => r.Guest)
            .Include(r => r.CreatedBy)
            .Include(r => r.Items).ThenInclude(i => i.RoomType)
            .Include(r => r.Items).ThenInclude(i => i.RatePlan)
            .Include(r => r.Items).ThenInclude(i => i.Room)
            .FirstOrDefaultAsync(r => r.Id == id);

    private static ReservationDto ToDto(Reservation r) => new(
        r.Id, r.ReservationNo, r.PropertyId, r.Property.Name, r.GuestId, r.Guest.FullName,
        r.Source.ToString(), r.CheckInDate, r.CheckOutDate,
        r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber,
        r.Status, r.TotalAmount, r.CreatedBy.Name,
        r.Items.OrderBy(i => i.Id).Select(i => new ReservationItemDto(
            i.Id, i.RoomTypeId, i.RoomType.Name, i.RatePlanId, i.RatePlan.Name,
            i.RoomId, i.Room?.RoomNo, i.Nights, i.NightlyRate, i.Total)).ToList());
}