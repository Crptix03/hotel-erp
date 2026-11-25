using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

// What the API sends back
public record AmenityDto(int Id, string Name, string Category);

public record RoomTypeDto(
    int Id, string Name, string? Description, string BedType, int BedCount,
    decimal SizeSqm, int MaxAdults, int MaxChildren, string? ViewType,
    bool SmokingAllowed, decimal BaseRate, List<AmenityDto> Amenities);

// What the API receives when adding or editing (the attributes are the validation rules)
public class RoomTypeSaveRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Required, StringLength(50)] public string BedType { get; set; } = string.Empty;
    [Range(1, 10)] public int BedCount { get; set; }
    [Range(1, 500)] public decimal SizeSqm { get; set; }
    [Range(1, 20)] public int MaxAdults { get; set; }
    [Range(0, 20)] public int MaxChildren { get; set; }
    [StringLength(50)] public string? ViewType { get; set; }
    public bool SmokingAllowed { get; set; }
    [Range(0, 1000000)] public decimal BaseRate { get; set; }
    public List<int> AmenityIds { get; set; } = new();
}

public class AmenitySaveRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
}