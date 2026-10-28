namespace HotelErp.Api.Models;

public class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BedType { get; set; } = string.Empty;
    public int BedCount { get; set; }
    public decimal SizeSqm { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public string? ViewType { get; set; }
    public bool SmokingAllowed { get; set; }
    public decimal BaseRate { get; set; }
    public List<Room> Rooms { get; set; } = new();
}