using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/rates")]
[Authorize]
public class RatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RateCalculator _calculator;

    public RatesController(AppDbContext db, RateCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    // Example: /api/rates/quote?roomTypeId=1&ratePlanId=1&checkIn=2026-12-01&checkOut=2026-12-04
    [HttpGet("quote")]
    public async Task<ActionResult<QuoteDto>> Quote(int roomTypeId, int ratePlanId, DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn == default || checkOut == default)
            return BadRequest(new { message = "checkIn and checkOut dates are required (format 2026-12-01)" });

        if (checkOut <= checkIn)
            return BadRequest(new { message = "Check-out must be after check-in" });

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        if (nights > RateCalculator.MaxNights)
            return BadRequest(new { message = $"A stay cannot be longer than {RateCalculator.MaxNights} nights" });

        var roomType = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomTypeId);
        if (roomType is null) return NotFound(new { message = "Room type not found" });

        var plan = await _db.RatePlans.AsNoTracking()
            .Include(p => p.Rules)
            .FirstOrDefaultAsync(p => p.Id == ratePlanId);
        if (plan is null) return NotFound(new { message = "Rate plan not found" });

        // the last night of the stay is the day before check-out
        if (checkIn < plan.ValidFrom || checkOut.AddDays(-1) > plan.ValidTo)
            return BadRequest(new { message = $"This rate plan is only valid from {plan.ValidFrom} to {plan.ValidTo}" });

        return _calculator.Calculate(roomType, plan, checkIn, checkOut);
    }
}