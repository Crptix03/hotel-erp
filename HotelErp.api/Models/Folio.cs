namespace HotelErp.Api.Models;

public class Folio
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;
    public bool PayAtCheckout { get; set; } = true;
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Open";
    public List<FolioLine> Lines { get; set; } = new();
}