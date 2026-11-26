namespace HotelErp.Api.Models;

public static class ReservationStatus
{
    public const string Confirmed = "Confirmed";
    public const string CheckedIn = "CheckedIn";
    public const string CheckedOut = "CheckedOut";
    public const string Cancelled = "Cancelled";

    // Reservations in these statuses hold rooms, so they reduce availability
    public static readonly List<string> Blocking = new() { Confirmed, CheckedIn };
}