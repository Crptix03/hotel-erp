using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

public record PropertyDto(int Id, string Code, string Name, string Address, bool IsActive, int RoomCount);

public class PropertySaveRequest
{
    [Required, StringLength(20)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(250)] public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public record RoomDto(
    int Id, int PropertyId, string PropertyName, int RoomTypeId, string RoomTypeName,
    string RoomNo, int Floor, string Status);

public class RoomSaveRequest
{
    [Range(1, int.MaxValue)] public int PropertyId { get; set; }
    [Range(1, int.MaxValue)] public int RoomTypeId { get; set; }
    [Required, StringLength(20)] public string RoomNo { get; set; } = string.Empty;
    [Range(-5, 200)] public int Floor { get; set; }
}

public class RoomStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
}