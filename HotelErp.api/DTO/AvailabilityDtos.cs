namespace HotelErp.Api.DTOs;

public record QuoteSummaryDto(int RatePlanId, string RatePlanName, decimal Total, decimal AverageNightly);

public record RoomTypeAvailabilityDto(
    int RoomTypeId, string RoomTypeName, string? Description, string BedType, int BedCount,
    int MaxAdults, int MaxChildren, decimal BaseRate, List<AmenityDto> Amenities,
    int TotalRooms, int Reserved, int Available, QuoteSummaryDto? Quote);

public record AvailabilityDto(
    int PropertyId, DateOnly CheckIn, DateOnly CheckOut, int Nights,
    List<RoomTypeAvailabilityDto> RoomTypes);

public record FreeRoomDto(int RoomId, string RoomNo, int Floor, string Status, bool ReadyNow);