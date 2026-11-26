using System.Globalization;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;

namespace HotelErp.Api.Services;

public class RateCalculator
{
    // Nights that count as "weekend nights". Change these if the hotel uses other days.
    private static readonly HashSet<DayOfWeek> WeekendNights =
        new() { DayOfWeek.Friday, DayOfWeek.Saturday };

    public const int MaxNights = 365;

    // Pure calculation: it never touches the database, so it is easy to test.
    public QuoteDto Calculate(RoomType roomType, RatePlan plan, DateOnly checkIn, DateOnly checkOut)
    {
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var rules = plan.Rules.OrderBy(r => r.Id).ToList();
        var breakdown = new List<NightQuoteDto>();

        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            var price = roomType.BaseRate + plan.Price;
            var applied = new List<AppliedRuleDto>();

            foreach (var rule in rules)
            {
                if (!Applies(rule, nights, date)) continue;

                var before = price;
                price = rule.AdjustmentType == "Percent"
                    ? price * (1 + rule.AdjustmentValue / 100m)
                    : price + rule.AdjustmentValue;
                price = Math.Max(0m, Math.Round(price, 2, MidpointRounding.AwayFromZero));

                applied.Add(new AppliedRuleDto(rule.Id, rule.RuleType.ToString(), Describe(rule), price - before));
            }

            breakdown.Add(new NightQuoteDto(date, roomType.BaseRate, plan.Price, applied, price));
        }

        var total = breakdown.Sum(n => n.Rate);
        var average = Math.Round(total / nights, 2, MidpointRounding.AwayFromZero);

        return new QuoteDto(
            roomType.Id, roomType.Name, plan.Id, plan.Name,
            checkIn, checkOut, nights, breakdown, total, average);
    }

    // Does this rule apply to this particular night of a stay of `nights` nights?
    private static bool Applies(RateRule rule, int nights, DateOnly night)
    {
        if (nights < rule.MinNights) return false;
        if (rule.MaxNights.HasValue && nights > rule.MaxNights.Value) return false;
        if (rule.ValidFrom.HasValue && night < rule.ValidFrom.Value) return false;
        if (rule.ValidTo.HasValue && night > rule.ValidTo.Value) return false;
        if (rule.RuleType == RateRuleType.Weekend && !WeekendNights.Contains(night.DayOfWeek)) return false;
        return true;   // LengthOfStay and Seasonal are decided by the checks above
    }

    private static string Describe(RateRule r)
    {
        var amount = r.AdjustmentValue.ToString("+0.##;-0.##", CultureInfo.InvariantCulture);
        var value = r.AdjustmentType == "Percent" ? $"{amount}%" : $"{amount} per night";
        return r.RuleType switch
        {
            RateRuleType.LengthOfStay => $"Length of stay {value}",
            RateRuleType.Weekend => $"Weekend night {value}",
            _ => $"Seasonal {value}"
        };
    }
}