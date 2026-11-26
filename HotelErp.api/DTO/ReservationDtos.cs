using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

public class ReservationItemRequest
{
    [Range(1, int.MaxValue)] public int RoomTypeId { get; set; }
    [Range(1, int.MaxValue)] public int RatePlanId { get; set; }
    [Range(1, 10)] public int Quantity { get; set; } = 1;   // how many rooms of this type
}

public class ReservationCreateRequest
{
    [Range(1, int.MaxValue)] public int PropertyId { get; set; }
    [Range(1, int.MaxValue)] public int GuestId { get; set; }
    [Required] public string Source { get; set; } = string.Empty;   // Phone, WalkIn or Online
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    [Required, MinLength(1), MaxLength(10)]
    public List<ReservationItemRequest> Items { get; set; } = new();
}

public record ReservationItemDto(
    int Id, int RoomTypeId, string RoomTypeName, int RatePlanId, string RatePlanName,
    int? RoomId, string? RoomNo, int Nights, decimal NightlyRate, decimal Total);

public record ReservationDto(
    int Id, string ReservationNo, int PropertyId, string PropertyName,
    int GuestId, string GuestName, string Source, DateOnly CheckIn, DateOnly CheckOut,
    int Nights, string Status, decimal TotalAmount, string CreatedBy,
    List<ReservationItemDto> Items);

public record ReservationListItemDto(
    int Id, string ReservationNo, string GuestName, string Source, DateOnly CheckIn, DateOnly CheckOut,
    int Nights, string Status, decimal TotalAmount, int RoomCount);