using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Services;

public static class FolioMapping
{
    public static IQueryable<Folio> WithDetails(this IQueryable<Folio> query) => query
        .Include(f => f.Lines).ThenInclude(l => l.PostedBy)
        .Include(f => f.Stay).ThenInclude(s => s.ReservationItem).ThenInclude(i => i.Reservation).ThenInclude(r => r.Guest)
        .Include(f => f.Stay).ThenInclude(s => s.ReservationItem).ThenInclude(i => i.Reservation).ThenInclude(r => r.Property)
        .Include(f => f.Stay).ThenInclude(s => s.ReservationItem).ThenInclude(i => i.RoomType)
        .Include(f => f.Stay).ThenInclude(s => s.ReservationItem).ThenInclude(i => i.Room);

    public static FolioDto ToDto(Folio f)
    {
        var item = f.Stay.ReservationItem;
        var reservation = item.Reservation;
        var lines = f.Lines.OrderBy(l => l.Id).ToList();

        // The lines are the source of truth for the totals
        var charges = lines.Where(l => l.Type != FolioLineType.Payment).Sum(l => l.Amount);
        var payments = lines.Where(l => l.Type == FolioLineType.Payment).Sum(l => l.Amount);
        var balance = charges - payments;

        return new FolioDto(
            f.Id, f.StayId, f.Status, f.PayAtCheckout,
            reservation.ReservationNo, reservation.Guest.FullName,
            item.Room!.RoomNo, item.RoomType.Name, reservation.Property.Name,
            reservation.CheckInDate, reservation.CheckOutDate,
            lines.Select(l => new FolioLineDto(
                l.Id, l.Type.ToString(), l.Description, l.Amount,
                l.PaymentMethod?.ToString(), l.PostedAt, l.PostedBy?.Name)).ToList(),
            charges, payments, balance, balance == 0);
    }
}