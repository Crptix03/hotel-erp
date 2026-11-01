namespace HotelErp.Api.Models;

public class Reservation
{
    public int Id { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public BookingSource Source { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public string Status { get; set; } = "Confirmed";
    public decimal TotalAmount { get; set; }
    public int CreatedById { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
    public List<ReservationItem> Items { get; set; } = new();
}