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
[Authorize]
public class StaysController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CheckInService _checkIn;

    public StaysController(AppDbContext db, CheckInService checkIn)
    {
        _db = db;
        _checkIn = checkIn;
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("/api/reservations/{reservationId:int}/check-in")]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(int reservationId, CheckInRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _checkIn.CheckInAsync(reservationId, req, userId);

        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return result.Value!;
    }

    // Who is in the hotel? Example: /api/stays?propertyId=1&status=CheckedIn
    [HttpGet("/api/stays")]
    public async Task<ActionResult<PagedResult<StayListItemDto>>> Search(
        int? propertyId, string? status, string? q, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Stays.AsNoTracking().AsQueryable();

        if (propertyId.HasValue)
            query = query.Where(s => s.ReservationItem.Reservation.PropertyId == propertyId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var known = StayStatus.Normalize(status);
            if (known is null) return BadRequest(new { message = "Unknown status. Use CheckedIn or CheckedOut" });
            query = query.Where(s => s.Status == known);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.ReservationItem.Reservation.Guest.FullName, term) ||
                EF.Functions.ILike(s.ReservationItem.Reservation.ReservationNo, term) ||
                EF.Functions.ILike(s.ReservationItem.Room!.RoomNo, term));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.ReservationItem.Room!.RoomNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StayListItemDto(
                s.Id,
                s.ReservationItem.Reservation.ReservationNo,
                s.ReservationItem.Reservation.Guest.FullName,
                s.ReservationItem.Room!.RoomNo,
                s.ReservationItem.RoomType.Name,
                s.ReservationItem.Reservation.CheckOutDate,
                s.ActualCheckIn,
                s.Status,
                s.Folio!.Balance))
            .ToListAsync();

        return new PagedResult<StayListItemDto>(items, total, page, pageSize);
    }

    [HttpGet("/api/stays/{id:int}")]
    public async Task<ActionResult<StayDto>> Get(int id)
    {
        var stay = await _db.Stays.AsNoTracking().WithDetails().FirstOrDefaultAsync(s => s.Id == id);
        if (stay is null) return NotFound(new { message = "Stay not found" });
        return StayMapping.ToDto(stay);
    }
}