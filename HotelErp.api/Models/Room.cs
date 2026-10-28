namespace HotelErp.Api.Models;

public class Room
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    public string RoomNo { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Clean;
}