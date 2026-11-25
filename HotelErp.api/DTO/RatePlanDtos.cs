using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

public record RateRuleDto(
    int Id, int RatePlanId, string RuleType, int MinNights, int? MaxNights,
    string AdjustmentType, decimal AdjustmentValue, DateOnly? ValidFrom, DateOnly? ValidTo);

public record RatePlanDto(
    int Id, string Name, string MealPlan, decimal Price,
    DateOnly ValidFrom, DateOnly ValidTo, List<RateRuleDto> Rules);

public class RatePlanSaveRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(50)] public string MealPlan { get; set; } = string.Empty;
    [Range(0, 1000000)] public decimal Price { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
}

public class RateRuleSaveRequest
{
    [Required] public string RuleType { get; set; } = string.Empty;
    [Range(1, 365)] public int MinNights { get; set; } = 1;
    [Range(1, 365)] public int? MaxNights { get; set; }
    [Required] public string AdjustmentType { get; set; } = "Percent";
    [Range(-1000000, 1000000)] public decimal AdjustmentValue { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}