namespace HotelErp.Api.Models;

public class RatePlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MealPlan { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public List<RateRule> Rules { get; set; } = new();
}