using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/room-types")]
[Authorize]   // every endpoint here needs a logged-in user
public class RoomTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomTypesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RoomTypeDto>>> GetAll()
    {
        var items = await _db.RoomTypes
            .Include(r => r.Amenities)   // also load each room type's amenities
            .AsNoTracking()              // read-only, so it's faster
            .OrderBy(r => r.Name)
            .ToListAsync();
        return items.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomTypeDto>> Get(int id)
    {
        var entity = await _db.RoomTypes
            .Include(r => r.Amenities)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null) return NotFound(new { message = "Room type not found" });
        return ToDto(entity);
    }

    [Authorize(Roles = "Manager")]   // only managers can add
    [HttpPost]
    public async Task<ActionResult<RoomTypeDto>> Create(RoomTypeSaveRequest req)
    {
        if (await _db.RoomTypes.AnyAsync(r => r.Name == req.Name))
            return Conflict(new { message = "A room type with this name already exists" });

        var amenities = await LoadAmenities(req.AmenityIds);
        if (amenities is null)
            return BadRequest(new { message = "One or more amenities do not exist" });

        var entity = new RoomType();
        Apply(entity, req);
        entity.Amenities.AddRange(amenities);

        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomTypeDto>> Update(int id, RoomTypeSaveRequest req)
    {
        var entity = await _db.RoomTypes
            .Include(r => r.Amenities)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null) return NotFound(new { message = "Room type not found" });

        if (await _db.RoomTypes.AnyAsync(r => r.Name == req.Name && r.Id != id))
            return Conflict(new { message = "A room type with this name already exists" });

        var amenities = await LoadAmenities(req.AmenityIds);
        if (amenities is null)
            return BadRequest(new { message = "One or more amenities do not exist" });

        Apply(entity, req);
        entity.Amenities.Clear();
        entity.Amenities.AddRange(amenities);

        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.RoomTypes.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Room type not found" });

        var inUse = await _db.Rooms.AnyAsync(r => r.RoomTypeId == id)
                 || await _db.ReservationItems.AnyAsync(i => i.RoomTypeId == id);
        if (inUse)
            return Conflict(new { message = "This room type is used by rooms or reservations and cannot be deleted" });

        _db.RoomTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- helpers ----

    private async Task<List<Amenity>?> LoadAmenities(List<int> ids)
    {
        var distinct = ids.Distinct().ToList();
        var amenities = await _db.Amenities.Where(a => distinct.Contains(a.Id)).ToListAsync();
        return amenities.Count == distinct.Count ? amenities : null;
    }

    private static void Apply(RoomType e, RoomTypeSaveRequest r)
    {
        e.Name = r.Name.Trim();
        e.Description = r.Description;
        e.BedType = r.BedType.Trim();
        e.BedCount = r.BedCount;
        e.SizeSqm = r.SizeSqm;
        e.MaxAdults = r.MaxAdults;
        e.MaxChildren = r.MaxChildren;
        e.ViewType = r.ViewType;
        e.SmokingAllowed = r.SmokingAllowed;
        e.BaseRate = r.BaseRate;
    }

    private static RoomTypeDto ToDto(RoomType r) => new(
        r.Id, r.Name, r.Description, r.BedType, r.BedCount, r.SizeSqm,
        r.MaxAdults, r.MaxChildren, r.ViewType, r.SmokingAllowed, r.BaseRate,
        r.Amenities.OrderBy(a => a.Name)
                   .Select(a => new AmenityDto(a.Id, a.Name, a.Category))
                   .ToList());
}