using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

public class ChargeRequest
{
    [Required, StringLength(200)] public string Description { get; set; } = string.Empty;
    [Range(0.01, 10000000)] public decimal Amount { get; set; }
    public string? Type { get; set; }   // ServiceCharge (the default) or Tax
}

public class PaymentRequest
{
    [Range(0.01, 10000000)] public decimal Amount { get; set; }
    [Required] public string Method { get; set; } = string.Empty;   // Cash, Card, MobileMoney, BankTransfer
    [StringLength(200)] public string? Note { get; set; }
}

public class AdjustmentRequest
{
    [Required, StringLength(200)] public string Reason { get; set; } = string.Empty;
    [Range(-10000000, 10000000)] public decimal Amount { get; set; }   // negative = discount
}

public record FolioLineDto(
    int Id, string Type, string Description, decimal Amount,
    string? PaymentMethod, DateTime? PostedAt, string? PostedBy);

public record FolioDto(
    int Id, int StayId, string Status, bool PayAtCheckout,
    string ReservationNo, string GuestName, string RoomNo, string RoomTypeName, string PropertyName,
    DateOnly CheckInDate, DateOnly CheckOutDate,
    List<FolioLineDto> Lines, decimal TotalCharges, decimal TotalPayments, decimal Balance, bool IsPaidInFull);