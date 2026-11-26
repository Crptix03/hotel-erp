using System.ComponentModel.DataAnnotations;

namespace HotelErp.Api.DTOs;

// Wraps one page of results, so Angular knows how many pages there are
public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public record GuestIdDocumentDto(
    int Id, int GuestId, string IdType, string IdNumberMasked, string IssuingCountry,
    DateOnly? ExpiryDate, bool IsExpired, bool IsVerified, string? VerifiedByName, DateTime? VerifiedAt);

public record GuestDto(int Id, string FullName, string Phone, string? Email, int IdDocumentCount);

public record GuestDetailDto(
    int Id, string FullName, string Phone, string? Email, List<GuestIdDocumentDto> IdDocuments);

public class GuestSaveRequest
{
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
}

public class GuestIdDocumentSaveRequest
{
    [Required] public string IdType { get; set; } = string.Empty;
    [Required, StringLength(50)] public string IdNumber { get; set; } = string.Empty;
    [Required, StringLength(80)] public string IssuingCountry { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
}