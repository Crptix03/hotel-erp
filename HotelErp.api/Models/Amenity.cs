namespace HotelErp.Api.Models;

public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<RoomType> RoomTypes { get; set; } = new();
}