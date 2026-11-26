using System.Security.Claims;
using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/guests")]
[Authorize]
public class GuestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GuestsController(AppDbContext db) => _db = db;

    // ===== Guests =====

    [HttpGet]
    public async Task<ActionResult<PagedResult<GuestDto>>> Search(string? q, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Guests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            // ILike is a case-insensitive "contains" search in PostgreSQL
            query = query.Where(g =>
                EF.Functions.ILike(g.FullName, term) ||
                EF.Functions.ILike(g.Phone, term) ||
                (g.Email != null && EF.Functions.ILike(g.Email, term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(g => g.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GuestDto(g.Id, g.FullName, g.Phone, g.Email, g.IdDocuments.Count))
            .ToListAsync();

        return new PagedResult<GuestDto>(items, total, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuestDetailDto>> Get(int id)
    {
        var guest = await _db.Guests.AsNoTracking()
            .Include(g => g.IdDocuments).ThenInclude(d => d.VerifiedBy)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (guest is null) return NotFound(new { message = "Guest not found" });
        return ToDetail(guest);
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost]
    public async Task<ActionResult<GuestDetailDto>> Create(GuestSaveRequest req)
    {
        var name = req.FullName.Trim();
        var phone = req.Phone.Trim();
        if (await _db.Guests.AnyAsync(g => g.Phone == phone && g.FullName.ToLower() == name.ToLower()))
            return Conflict(new { message = "A guest with this name and phone already exists" });

        var entity = new Guest { FullName = name, Phone = phone, Email = CleanEmail(req.Email) };
        _db.Guests.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDetail(entity));
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GuestDetailDto>> Update(int id, GuestSaveRequest req)
    {
        var entity = await _db.Guests
            .Include(g => g.IdDocuments).ThenInclude(d => d.VerifiedBy)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null) return NotFound(new { message = "Guest not found" });

        var name = req.FullName.Trim();
        var phone = req.Phone.Trim();
        if (await _db.Guests.AnyAsync(g =>
                g.Phone == phone && g.FullName.ToLower() == name.ToLower() && g.Id != id))
            return Conflict(new { message = "A guest with this name and phone already exists" });

        entity.FullName = name;
        entity.Phone = phone;
        entity.Email = CleanEmail(req.Email);
        await _db.SaveChangesAsync();
        return ToDetail(entity);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Guests.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Guest not found" });

        if (await _db.Reservations.AnyAsync(r => r.GuestId == id))
            return Conflict(new { message = "This guest has reservations and cannot be deleted" });

        _db.Guests.Remove(entity);   // their ID documents are deleted with them
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== ID documents =====

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("{guestId:int}/id-documents")]
    public async Task<ActionResult<GuestIdDocumentDto>> AddDocument(int guestId, GuestIdDocumentSaveRequest req)
    {
        if (!await _db.Guests.AnyAsync(g => g.Id == guestId))
            return NotFound(new { message = "Guest not found" });

        if (!TryParseType(req.IdType, out var type))
            return BadRequest(new { message = "Unknown ID type. Use NationalId, Passport or DrivingLicense" });

        var number = req.IdNumber.Trim();
        var country = req.IssuingCountry.Trim();
        if (await _db.GuestIdDocuments.AnyAsync(d =>
                d.IdType == type && d.IdNumber == number && d.IssuingCountry == country))
            return Conflict(new { message = "This ID is already registered" });

        var doc = new GuestIdDocument
        {
            GuestId = guestId,
            IdType = type,
            IdNumber = number,
            IssuingCountry = country,
            ExpiryDate = req.ExpiryDate
        };
        _db.GuestIdDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return ToDto(doc);
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPut("{guestId:int}/id-documents/{docId:int}")]
    public async Task<ActionResult<GuestIdDocumentDto>> UpdateDocument(
        int guestId, int docId, GuestIdDocumentSaveRequest req)
    {
        var doc = await _db.GuestIdDocuments.Include(d => d.VerifiedBy)
            .FirstOrDefaultAsync(d => d.Id == docId && d.GuestId == guestId);
        if (doc is null) return NotFound(new { message = "ID document not found" });

        if (!TryParseType(req.IdType, out var type))
            return BadRequest(new { message = "Unknown ID type. Use NationalId, Passport or DrivingLicense" });

        var number = req.IdNumber.Trim();
        var country = req.IssuingCountry.Trim();
        if (await _db.GuestIdDocuments.AnyAsync(d =>
                d.IdType == type && d.IdNumber == number && d.IssuingCountry == country && d.Id != docId))
            return Conflict(new { message = "This ID is already registered" });

        // if anything about the ID changed, it must be checked again
        var changed = doc.IdType != type || doc.IdNumber != number
                   || doc.IssuingCountry != country || doc.ExpiryDate != req.ExpiryDate;

        doc.IdType = type;
        doc.IdNumber = number;
        doc.IssuingCountry = country;
        doc.ExpiryDate = req.ExpiryDate;
        if (changed)
        {
            doc.VerifiedBy = null;
            doc.VerifiedById = null;
            doc.VerifiedAt = null;
        }

        await _db.SaveChangesAsync();
        return ToDto(doc);
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("{guestId:int}/id-documents/{docId:int}/verify")]
    public async Task<ActionResult<GuestIdDocumentDto>> VerifyDocument(int guestId, int docId)
    {
        var doc = await _db.GuestIdDocuments
            .FirstOrDefaultAsync(d => d.Id == docId && d.GuestId == guestId);
        if (doc is null) return NotFound(new { message = "ID document not found" });

        if (IsExpired(doc))
            return BadRequest(new { message = "This ID has expired and cannot be verified" });

        // who is logged in? This comes from the token's claims
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        doc.VerifiedBy = await _db.Users.FindAsync(userId);
        doc.VerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(doc);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{guestId:int}/id-documents/{docId:int}")]
    public async Task<IActionResult> DeleteDocument(int guestId, int docId)
    {
        var doc = await _db.GuestIdDocuments
            .FirstOrDefaultAsync(d => d.Id == docId && d.GuestId == guestId);
        if (doc is null) return NotFound(new { message = "ID document not found" });

        if (await _db.Stays.AnyAsync(s => s.IdDocumentId == docId))
            return Conflict(new { message = "This ID was used for a stay and cannot be deleted" });

        _db.GuestIdDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== helpers =====

    private static string? CleanEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();

    private static bool TryParseType(string value, out GovernmentIdType type) =>
        Enum.TryParse(value, true, out type) && Enum.IsDefined(type);

    private static bool IsExpired(GuestIdDocument d) =>
        d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    // shows only the last 4 characters, e.g. ******7890
    private static string Mask(string number) =>
        number.Length <= 4
            ? new string('*', number.Length)
            : new string('*', number.Length - 4) + number[^4..];

    private static GuestIdDocumentDto ToDto(GuestIdDocument d) => new(
        d.Id, d.GuestId, d.IdType.ToString(), Mask(d.IdNumber), d.IssuingCountry, d.ExpiryDate,
        IsExpired(d), d.VerifiedAt.HasValue, d.VerifiedBy?.Name, d.VerifiedAt);

    private static GuestDetailDto ToDetail(Guest g) => new(
        g.Id, g.FullName, g.Phone, g.Email,
        g.IdDocuments.OrderBy(d => d.Id).Select(ToDto).ToList());
}