using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

// Optional: lets the agent pick a specific room for a booked item
public class RoomChoice
{
    [Range(1, int.MaxValue)] public int ItemId { get; set; }
    [Range(1, int.MaxValue)] public int RoomId { get; set; }
}

public class CheckInRequest
{
    [Required] public string IdType { get; set; } = string.Empty;
    [Required, StringLength(50)] public string IdNumber { get; set; } = string.Empty;
    [Required, StringLength(80)] public string IssuingCountry { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public List<RoomChoice>? Rooms { get; set; }
}

public record StayDto(
    int Id, int ReservationId, string ReservationNo, int GuestId, string GuestName,
    int RoomId, string RoomNo, string RoomTypeName,
    DateOnly CheckInDate, DateOnly CheckOutDate, DateTime ActualCheckIn, DateTime? ActualCheckOut,
    string Status, string IdType, string IdNumberMasked, string? IdVerifiedBy,
    int FolioId, decimal Balance, bool PayAtCheckout);

public record CheckInResultDto(int ReservationId, string ReservationNo, string Status, List<StayDto> Stays);

public record StayListItemDto(
    int Id, string ReservationNo, string GuestName, string RoomNo, string RoomTypeName,
    DateOnly CheckOutDate, DateTime ActualCheckIn, string Status, decimal Balance);