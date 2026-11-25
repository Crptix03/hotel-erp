using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db) => _db = db;

    // Optional filters come from the address, e.g. /api/rooms?propertyId=1&status=Clean
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll(int? propertyId, int? roomTypeId, string? status)
    {
        var query = _db.Rooms.AsNoTracking()
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .AsQueryable();

        if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);
        if (roomTypeId.HasValue) query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RoomStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
                return BadRequest(new { message = "Unknown status. Use Clean, Dirty, Occupied or OutOfService" });
            query = query.Where(r => r.Status == parsed);
        }

        var rooms = await query.OrderBy(r => r.Property.Name).ThenBy(r => r.RoomNo).ToListAsync();
        return rooms.Select(ToDto).ToList();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Dictionary<string, int>>> Summary(int? propertyId)
    {
        var query = _db.Rooms.AsNoTracking();
        if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);

        var counts = await query
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // make sure every status appears, even with 0 rooms
        return Enum.GetValues<RoomStatus>().ToDictionary(
            s => s.ToString(),
            s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> Get(int id)
    {
        var room = await Load(id);
        if (room is null) return NotFound(new { message = "Room not found" });
        return ToDto(room);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(RoomSaveRequest req)
    {
        var error = await Validate(req, 0);
        if (error is not null) return error;

        var entity = new Room
        {
            PropertyId = req.PropertyId,
            RoomTypeId = req.RoomTypeId,
            RoomNo = req.RoomNo.Trim(),
            Floor = req.Floor,
            Status = RoomStatus.Clean
        };
        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync();

        var created = await Load(entity.Id);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(created!));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, RoomSaveRequest req)
    {
        var entity = await _db.Rooms.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Room not found" });

        var error = await Validate(req, id);
        if (error is not null) return error;

        entity.PropertyId = req.PropertyId;
        entity.RoomTypeId = req.RoomTypeId;
        entity.RoomNo = req.RoomNo.Trim();
        entity.Floor = req.Floor;
        await _db.SaveChangesAsync();

        var updated = await Load(id);
        return ToDto(updated!);
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<RoomDto>> ChangeStatus(int id, RoomStatusRequest req)
    {
        if (!Enum.TryParse<RoomStatus>(req.Status, true, out var newStatus) || !Enum.IsDefined(newStatus))
            return BadRequest(new { message = "Unknown status. Use Clean, Dirty or OutOfService" });

        if (newStatus == RoomStatus.Occupied)
            return BadRequest(new { message = "A room becomes Occupied automatically at check-in" });

        var room = await _db.Rooms
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (room is null) return NotFound(new { message = "Room not found" });

        if (room.Status == RoomStatus.Occupied)
            return Conflict(new { message = "This room is occupied. Check the guest out first" });

        room.Status = newStatus;
        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Rooms.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Room not found" });

        if (await _db.ReservationItems.AnyAsync(i => i.RoomId == id))
            return Conflict(new { message = "This room is used by reservations and cannot be deleted" });

        _db.Rooms.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- helpers ----

    private Task<Room?> Load(int id) =>
        _db.Rooms.AsNoTracking()
            .Include(r => r.Property)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

    // roomId is 0 when adding a new room
    private async Task<ActionResult?> Validate(RoomSaveRequest req, int roomId)
    {
        if (!await _db.Properties.AnyAsync(p => p.Id == req.PropertyId))
            return BadRequest(new { message = "Property does not exist" });

        if (!await _db.RoomTypes.AnyAsync(t => t.Id == req.RoomTypeId))
            return BadRequest(new { message = "Room type does not exist" });

        var roomNo = req.RoomNo.Trim();
        if (await _db.Rooms.AnyAsync(r =>
                r.PropertyId == req.PropertyId && r.RoomNo == roomNo && r.Id != roomId))
            return Conflict(new { message = "This room number already exists in this property" });

        return null;
    }

    private static RoomDto ToDto(Room r) => new(
        r.Id, r.PropertyId, r.Property.Name, r.RoomTypeId, r.RoomType.Name,
        r.RoomNo, r.Floor, r.Status.ToString());
}