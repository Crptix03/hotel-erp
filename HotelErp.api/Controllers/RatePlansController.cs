using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Controllers;

[ApiController]
[Route("api/rate-plans")]
[Authorize]
public class RatePlansController : ControllerBase
{
    private readonly AppDbContext _db;

    public RatePlansController(AppDbContext db) => _db = db;

    // ===== Rate plans =====

    [HttpGet]
    public async Task<ActionResult<List<RatePlanDto>>> GetAll()
    {
        var plans = await _db.RatePlans.AsNoTracking()
            .Include(p => p.Rules)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return plans.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RatePlanDto>> Get(int id)
    {
        var plan = await _db.RatePlans.AsNoTracking()
            .Include(p => p.Rules)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null) return NotFound(new { message = "Rate plan not found" });
        return ToDto(plan);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<RatePlanDto>> Create(RatePlanSaveRequest req)
    {
        var error = await ValidatePlan(req, 0);
        if (error is not null) return error;

        var entity = new RatePlan();
        ApplyPlan(entity, req);
        _db.RatePlans.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RatePlanDto>> Update(int id, RatePlanSaveRequest req)
    {
        var entity = await _db.RatePlans.Include(p => p.Rules).FirstOrDefaultAsync(p => p.Id == id);
        if (entity is null) return NotFound(new { message = "Rate plan not found" });

        var error = await ValidatePlan(req, id);
        if (error is not null) return error;

        ApplyPlan(entity, req);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.RatePlans.FindAsync(id);
        if (entity is null) return NotFound(new { message = "Rate plan not found" });

        if (await _db.ReservationItems.AnyAsync(i => i.RatePlanId == id))
            return Conflict(new { message = "This rate plan is used by reservations and cannot be deleted" });

        _db.RatePlans.Remove(entity);   // its rules are deleted with it
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Rate rules (they belong to one plan) =====

    [Authorize(Roles = "Manager")]
    [HttpPost("{planId:int}/rules")]
    public async Task<ActionResult<RateRuleDto>> AddRule(int planId, RateRuleSaveRequest req)
    {
        if (!await _db.RatePlans.AnyAsync(p => p.Id == planId))
            return NotFound(new { message = "Rate plan not found" });

        var error = ValidateRule(req, out var type, out var adjustmentType);
        if (error is not null) return BadRequest(new { message = error });

        var rule = new RateRule { RatePlanId = planId };
        ApplyRule(rule, req, type, adjustmentType);
        _db.RateRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(ToDto(rule));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{planId:int}/rules/{ruleId:int}")]
    public async Task<ActionResult<RateRuleDto>> UpdateRule(int planId, int ruleId, RateRuleSaveRequest req)
    {
        var rule = await _db.RateRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.RatePlanId == planId);
        if (rule is null) return NotFound(new { message = "Rate rule not found" });

        var error = ValidateRule(req, out var type, out var adjustmentType);
        if (error is not null) return BadRequest(new { message = error });

        ApplyRule(rule, req, type, adjustmentType);
        await _db.SaveChangesAsync();
        return ToDto(rule);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{planId:int}/rules/{ruleId:int}")]
    public async Task<IActionResult> DeleteRule(int planId, int ruleId)
    {
        var rule = await _db.RateRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.RatePlanId == planId);
        if (rule is null) return NotFound(new { message = "Rate rule not found" });

        _db.RateRules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== helpers =====

    private async Task<ActionResult?> ValidatePlan(RatePlanSaveRequest req, int id)
    {
        if (req.ValidFrom == default || req.ValidTo == default)
            return BadRequest(new { message = "Valid from and valid to dates are required" });

        if (req.ValidFrom > req.ValidTo)
            return BadRequest(new { message = "Valid from must be before valid to" });

        var name = req.Name.Trim();
        if (await _db.RatePlans.AnyAsync(p => p.Name == name && p.Id != id))
            return Conflict(new { message = "A rate plan with this name already exists" });

        return null;
    }

    // returns an error message, or null when the rule is fine
    private static string? ValidateRule(RateRuleSaveRequest r, out RateRuleType type, out string adjustmentType)
    {
        adjustmentType = "";

        if (!Enum.TryParse(r.RuleType, true, out type) || !Enum.IsDefined(type))
            return "Unknown rule type. Use LengthOfStay, Weekend or Seasonal";

        if (r.AdjustmentType.Equals("Percent", StringComparison.OrdinalIgnoreCase)) adjustmentType = "Percent";
        else if (r.AdjustmentType.Equals("Fixed", StringComparison.OrdinalIgnoreCase)) adjustmentType = "Fixed";
        else return "Adjustment type must be Percent or Fixed";

        if (adjustmentType == "Percent" && (r.AdjustmentValue < -100 || r.AdjustmentValue > 1000))
            return "A percent adjustment must be between -100 and 1000";

        if (r.MaxNights.HasValue && r.MaxNights.Value < r.MinNights)
            return "Max nights cannot be less than min nights";

        if (r.ValidFrom.HasValue && r.ValidTo.HasValue && r.ValidFrom.Value > r.ValidTo.Value)
            return "Valid from must be before valid to";

        if (type == RateRuleType.Seasonal && (!r.ValidFrom.HasValue || !r.ValidTo.HasValue))
            return "A seasonal rule needs both valid from and valid to dates";

        return null;
    }

    private static void ApplyPlan(RatePlan e, RatePlanSaveRequest r)
    {
        e.Name = r.Name.Trim();
        e.MealPlan = r.MealPlan.Trim();
        e.Price = r.Price;
        e.ValidFrom = r.ValidFrom;
        e.ValidTo = r.ValidTo;
    }

    private static void ApplyRule(RateRule e, RateRuleSaveRequest r, RateRuleType type, string adjustmentType)
    {
        e.RuleType = type;
        e.MinNights = r.MinNights;
        e.MaxNights = r.MaxNights;
        e.AdjustmentType = adjustmentType;
        e.AdjustmentValue = r.AdjustmentValue;
        e.ValidFrom = r.ValidFrom;
        e.ValidTo = r.ValidTo;
    }

    private static RateRuleDto ToDto(RateRule r) => new(
        r.Id, r.RatePlanId, r.RuleType.ToString(), r.MinNights, r.MaxNights,
        r.AdjustmentType, r.AdjustmentValue, r.ValidFrom, r.ValidTo);

    private static RatePlanDto ToDto(RatePlan p) => new(
        p.Id, p.Name, p.MealPlan, p.Price, p.ValidFrom, p.ValidTo,
        p.Rules.OrderBy(r => r.Id).Select(ToDto).ToList());
}