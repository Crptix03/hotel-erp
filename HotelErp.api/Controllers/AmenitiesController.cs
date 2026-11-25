using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/amenities")]
[Authorize]
public class AmenitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AmenitiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<AmenityDto>>> GetAll() =>
        await _db.Amenities.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AmenityDto(a.Id, a.Name, a.Category))
            .ToListAsync();

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<AmenityDto>> Create(AmenitySaveRequest req)
    {
        if (await _db.Amenities.AnyAsync(a => a.Name == req.Name))
            return Conflict(new { message = "This amenity already exists" });

        var entity = new Amenity { Name = req.Name.Trim(), Category = req.Category.Trim() };
        _db.Amenities.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new AmenityDto(entity.Id, entity.Name, entity.Category));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AmenityDto>> Update(int id, AmenitySaveRequest req)
    {
        var entity = await _db.Amenities.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Amenity not found" });

        if (await _db.Amenities.AnyAsync(a => a.Name == req.Name && a.Id != id))
            return Conflict(new { message = "This amenity already exists" });

        entity.Name = req.Name.Trim();
        entity.Category = req.Category.Trim();
        await _db.SaveChangesAsync();
        return new AmenityDto(entity.Id, entity.Name, entity.Category);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Amenities.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Amenity not found" });

        _db.Amenities.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}