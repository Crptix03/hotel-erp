namespace HotelErp.Api.Models;

public static class ReservationStatus
{
    public const string Confirmed = "Confirmed";
    public const string CheckedIn = "CheckedIn";
    public const string CheckedOut = "CheckedOut";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Confirmed, CheckedIn, CheckedOut, Cancelled };

    // Reservations in these statuses hold rooms, so they reduce availability
    public static readonly List<string> Blocking = new() { Confirmed, CheckedIn };

    // Turns "confirmed" into "Confirmed"; returns null if it isn't a known status
    public static string? Normalize(string? value) =>
        All.FirstOrDefault(s => s.Equals(value, StringComparison.OrdinalIgnoreCase));
}