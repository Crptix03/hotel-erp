namespace HotelErp.Api.Models;

public class Reservation
{
    public int Id { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public BookingSource Source { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public string Status { get; set; } = ReservationStatus.Confirmed;
    public decimal TotalAmount { get; set; }
    public int CreatedById { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
    public List<ReservationItem> Items { get; set; } = new();
}