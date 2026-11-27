namespace HotelErp.Api.Models;

public static class StayStatus
{
    public const string CheckedIn = "CheckedIn";
    public const string CheckedOut = "CheckedOut";

    public static readonly string[] All = { CheckedIn, CheckedOut };

    public static string? Normalize(string? value) =>
        All.FirstOrDefault(s => s.Equals(value, StringComparison.OrdinalIgnoreCase));
}