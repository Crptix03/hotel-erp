namespace HotelErp.Api.Models;

public class GuestIdDocument
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public GovernmentIdType IdType { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public string IssuingCountry { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public int? VerifiedById { get; set; }
    public AppUser? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}