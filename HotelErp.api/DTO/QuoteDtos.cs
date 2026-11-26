namespace HotelErp.Api.DTOs;

public record AppliedRuleDto(int RuleId, string RuleType, string Description, decimal Amount);

public record NightQuoteDto(
    DateOnly Date, decimal BaseRate, decimal PlanPrice,
    List<AppliedRuleDto> AppliedRules, decimal Rate);

public record QuoteDto(
    int RoomTypeId, string RoomTypeName, int RatePlanId, string RatePlanName,
    DateOnly CheckIn, DateOnly CheckOut, int Nights,
    List<NightQuoteDto> Breakdown, decimal Total, decimal AverageNightly);