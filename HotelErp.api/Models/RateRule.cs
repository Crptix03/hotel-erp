namespace HotelErp.Api.Models;

public class RateRule
{
    public int Id { get; set; }
    public int RatePlanId { get; set; }
    public RatePlan RatePlan { get; set; } = null!;
    public RateRuleType RuleType { get; set; }
    public int MinNights { get; set; }
    public int? MaxNights { get; set; }
    public string AdjustmentType { get; set; } = "Percent";
    public decimal AdjustmentValue { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}