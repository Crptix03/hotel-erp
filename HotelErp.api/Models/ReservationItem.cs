namespace HotelErp.Api.Models;

public class ReservationItem
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public int RatePlanId { get; set; }
    public RatePlan RatePlan { get; set; } = null!;
    public int Nights { get; set; }
    public decimal NightlyRate { get; set; }
    public Stay? Stay { get; set; }
}