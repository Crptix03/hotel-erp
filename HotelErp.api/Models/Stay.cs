namespace HotelErp.Api.Models;

public class Stay
{
    public int Id { get; set; }
    public int ReservationItemId { get; set; }
    public ReservationItem ReservationItem { get; set; } = null!;
    public int IdDocumentId { get; set; }
    public GuestIdDocument IdDocument { get; set; } = null!;
    public DateTime ActualCheckIn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualCheckOut { get; set; }
    public string Status { get; set; } = "CheckedIn";
    public Folio? Folio { get; set; }
    public List<GuestRequest> Requests { get; set; } = new();
}