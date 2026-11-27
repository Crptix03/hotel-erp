using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Services;

public static class StayMapping
{
    // Loads everything a stay needs for display
    public static IQueryable<Stay> WithDetails(this IQueryable<Stay> query) => query
        .Include(s => s.ReservationItem).ThenInclude(i => i.Reservation).ThenInclude(r => r.Guest)
        .Include(s => s.ReservationItem).ThenInclude(i => i.RoomType)
        .Include(s => s.ReservationItem).ThenInclude(i => i.Room)
        .Include(s => s.IdDocument).ThenInclude(d => d.VerifiedBy)
        .Include(s => s.Folio);

    public static StayDto ToDto(Stay s)
    {
        var item = s.ReservationItem;
        var reservation = item.Reservation;
        return new StayDto(
            s.Id, reservation.Id, reservation.ReservationNo, reservation.GuestId, reservation.Guest.FullName,
            item.RoomId!.Value, item.Room!.RoomNo, item.RoomType.Name,
            reservation.CheckInDate, reservation.CheckOutDate, s.ActualCheckIn, s.ActualCheckOut,
            s.Status, s.IdDocument.IdType.ToString(), Mask(s.IdDocument.IdNumber), s.IdDocument.VerifiedBy?.Name,
            s.Folio!.Id, s.Folio.Balance, s.Folio.PayAtCheckout);
    }

    // shows only the last 4 characters, e.g. ********6655
    public static string Mask(string number) =>
        number.Length <= 4
            ? new string('*', number.Length)
            : new string('*', number.Length - 4) + number[^4..];
}