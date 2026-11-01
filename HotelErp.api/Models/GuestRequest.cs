namespace HotelErp.Api.Models;

public class GuestRequest
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;
    public RequestType RequestType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? AssignedToId { get; set; }
    public AppUser? AssignedTo { get; set; }
    public decimal? Charge { get; set; }
}