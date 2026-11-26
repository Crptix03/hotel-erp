namespace HotelErp.Api.Services;

public static class StayRules
{
    public const int MaxNights = 365;

    // Ethiopia is UTC+3. "Today" must follow the hotel's clock, not the server's.
    public const int HotelUtcOffsetHours = 3;

    public static DateOnly Today() =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddHours(HotelUtcOffsetHours));

    // Returns an error message, or null when the dates are fine
    public static string? ValidateDates(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn == default || checkOut == default)
            return "checkIn and checkOut dates are required (format 2026-12-01)";
        if (checkOut <= checkIn)
            return "Check-out must be after check-in";
        if (checkOut.DayNumber - checkIn.DayNumber > MaxNights)
            return $"A stay cannot be longer than {MaxNights} nights";
        return null;
    }
}