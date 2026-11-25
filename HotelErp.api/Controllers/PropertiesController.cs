using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PropertiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<PropertyDto>>> GetAll() =>
        await _db.Properties.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyDto(p.Id, p.Code, p.Name, p.Address, p.IsActive, p.Rooms.Count))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDto>> Get(int id)
    {
        var dto = await _db.Properties.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyDto(p.Id, p.Code, p.Name, p.Address, p.IsActive, p.Rooms.Count))
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound(new { message = "Property not found" });
        return dto;
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<PropertyDto>> Create(PropertySaveRequest req)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.Properties.AnyAsync(p => p.Code == code))
            return Conflict(new { message = "A property with this code already exists" });

        var entity = new Property
        {
            Code = code,
            Name = req.Name.Trim(),
            Address = req.Address.Trim(),
            IsActive = req.IsActive
        };
        _db.Properties.Add(entity);
        await _db.SaveChangesAsync();

        var dto = new PropertyDto(entity.Id, entity.Code, entity.Name, entity.Address, entity.IsActive, 0);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyDto>> Update(int id, PropertySaveRequest req)
    {
        var entity = await _db.Properties.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Property not found" });

        var code = req.Code.Trim().ToUpperInvariant();
        if (await _db.Properties.AnyAsync(p => p.Code == code && p.Id != id))
            return Conflict(new { message = "A property with this code already exists" });

        entity.Code = code;
        entity.Name = req.Name.Trim();
        entity.Address = req.Address.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync();

        var roomCount = await _db.Rooms.CountAsync(r => r.PropertyId == id);
        return new PropertyDto(entity.Id, entity.Code, entity.Name, entity.Address, entity.IsActive, roomCount);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Properties.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Property not found" });

        if (await _db.Rooms.AnyAsync(r => r.PropertyId == id))
            return Conflict(new { message = "This property still has rooms. Delete or move them first" });

        _db.Properties.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}