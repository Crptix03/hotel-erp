using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Services;

public class CheckInService
{
    private readonly AppDbContext _db;

    public CheckInService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<CheckInResultDto>> CheckInAsync(int reservationId, CheckInRequest req, int userId)
    {
        // ----- 1. validateGovernmentId: checks that need no database -----
        if (!Enum.TryParse<GovernmentIdType>(req.IdType, true, out var idType) || !Enum.IsDefined(idType))
            return Fail(400, "Unknown ID type. Use NationalId, Passport or DrivingLicense");

        var today = StayRules.Today();
        if (req.ExpiryDate.HasValue && req.ExpiryDate.Value < today)
            return Fail(400, "This ID has expired. Ask the guest for a valid ID");

        var idNumber = req.IdNumber.Trim();
        var country = req.IssuingCountry.Trim();

        var choices = req.Rooms ?? new List<RoomChoice>();
        if (choices.Select(c => c.ItemId).Distinct().Count() != choices.Count ||
            choices.Select(c => c.RoomId).Distinct().Count() != choices.Count)
            return Fail(400, "Each item and each room can be listed only once");

        // ----- 2. Lock the hotel, then read the reservation -----
        // We lock BEFORE reading the status, so a second request for the same reservation waits
        // here and then sees that it is already checked in.
        var propertyId = await _db.Reservations
            .Where(r => r.Id == reservationId)
            .Select(r => (int?)r.PropertyId)
            .FirstOrDefaultAsync();
        if (propertyId is null) return Fail(404, "Reservation not found");

        await using var tx = await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)propertyId.Value})");

        var reservation = await _db.Reservations
            .Include(r => r.Items).ThenInclude(i => i.RoomType)
            .FirstAsync(r => r.Id == reservationId);

        if (reservation.Status != ReservationStatus.Confirmed)
            return Fail(409, $"A {reservation.Status} reservation cannot be checked in");
        if (today < reservation.CheckInDate)
            return Fail(409, $"This guest is booked from {reservation.CheckInDate}. Check-in is not possible before that date");
        if (today >= reservation.CheckOutDate)
            return Fail(409, "The dates of this reservation have passed");

        var items = reservation.Items.OrderBy(i => i.Id).ToList();
        foreach (var c in choices)
        {
            if (items.All(i => i.Id != c.ItemId))
                return Fail(400, $"Item {c.ItemId} is not part of this reservation");
        }

        // ----- 3. getAvailableRoom: pick a clean, free room for every booked item -----
        var typeIds = items.Select(i => i.RoomTypeId).Distinct().ToList();
        var rooms = await _db.Rooms
            .Where(r => r.PropertyId == reservation.PropertyId && typeIds.Contains(r.RoomTypeId))
            .ToListAsync();

        // rooms held by OTHER bookings that overlap these dates
        var blocking = ReservationStatus.Blocking;
        var takenIds = await _db.ReservationItems.AsNoTracking()
            .Where(i => i.RoomId != null
                     && i.ReservationId != reservation.Id
                     && blocking.Contains(i.Reservation.Status)
                     && i.Reservation.CheckInDate < reservation.CheckOutDate
                     && reservation.CheckInDate < i.Reservation.CheckOutDate)
            .Select(i => i.RoomId!.Value)
            .ToListAsync();
        var taken = takenIds.ToHashSet();

        var plan = new List<(ReservationItem Item, Room Room)>();
        var picked = new HashSet<int>();

        foreach (var item in items)
        {
            Room? room;
            var choice = choices.FirstOrDefault(c => c.ItemId == item.Id);

            if (choice is not null)
            {
                // the agent chose a room
                room = rooms.FirstOrDefault(r => r.Id == choice.RoomId);
                if (room is null || room.RoomTypeId != item.RoomTypeId)
                    return Fail(400, $"Room {choice.RoomId} is not a '{item.RoomType.Name}' room in this hotel");
                if (room.Status != RoomStatus.Clean)
                    return Fail(409, $"Room {room.RoomNo} is {room.Status} and cannot take a guest now");
                if (taken.Contains(room.Id) || picked.Contains(room.Id))
                    return Fail(409, $"Room {room.RoomNo} is already booked for these dates");
            }
            else
            {
                // choose the lowest-numbered clean room that is free
                room = rooms
                    .Where(r => r.RoomTypeId == item.RoomTypeId
                             && r.Status == RoomStatus.Clean
                             && !taken.Contains(r.Id)
                             && !picked.Contains(r.Id))
                    .OrderBy(r => r.RoomNo)
                    .FirstOrDefault();
                if (room is null)
                    return Fail(409, $"No clean '{item.RoomType.Name}' room is available. Some rooms may still be waiting to be cleaned");
            }

            picked.Add(room.Id);
            plan.Add((item, room));
        }

        // ----- 4. saveIdDocument -----
        var document = await _db.GuestIdDocuments.FirstOrDefaultAsync(d =>
            d.IdType == idType && d.IdNumber == idNumber && d.IssuingCountry == country);

        if (document is not null && document.GuestId != reservation.GuestId)
            return Fail(409, "This ID is registered to a different guest");

        if (document is null)
        {
            document = new GuestIdDocument
            {
                GuestId = reservation.GuestId,
                IdType = idType,
                IdNumber = idNumber,
                IssuingCountry = country
            };
            _db.GuestIdDocuments.Add(document);
        }
        document.ExpiryDate = req.ExpiryDate;
        document.VerifiedById = userId;            // the agent who checked the ID at the desk
        document.VerifiedAt = DateTime.UtcNow;

        // ----- 5. createStay, updateRoomStatus(OCCUPIED), openFolio -----
        var now = DateTime.UtcNow;
        var stays = new List<Stay>();

        foreach (var (item, room) in plan)
        {
            item.RoomId = room.Id;
            room.Status = RoomStatus.Occupied;

            var stay = new Stay
            {
                ReservationItem = item,
                IdDocument = document,
                ActualCheckIn = now,
                Status = StayStatus.CheckedIn
            };

            var folio = new Folio
            {
                Stay = stay,
                PayAtCheckout = true,
                Status = "Open",
                Balance = item.Total       // the amount due so far
            };
            folio.Lines.Add(new FolioLine
            {
                Type = FolioLineType.RoomCharge,
                Description = $"Room {room.RoomNo} ({item.RoomType.Name}), {item.Nights} night(s), " +
                              $"{reservation.CheckInDate:yyyy-MM-dd} to {reservation.CheckOutDate:yyyy-MM-dd}",
                Amount = item.Total
            });

            _db.Stays.Add(stay);
            _db.Folios.Add(folio);
            stays.Add(stay);
        }

        reservation.Status = ReservationStatus.CheckedIn;

        // ----- 6. save() and commit() -----
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var stayIds = stays.Select(s => s.Id).ToList();
        var loaded = await _db.Stays.AsNoTracking().WithDetails()
            .Where(s => stayIds.Contains(s.Id))
            .OrderBy(s => s.Id)
            .ToListAsync();

        return ServiceResult<CheckInResultDto>.Ok(new CheckInResultDto(
            reservation.Id, reservation.ReservationNo, reservation.Status,
            loaded.Select(StayMapping.ToDto).ToList()));
    }

    private static ServiceResult<CheckInResultDto> Fail(int statusCode, string message) =>
        ServiceResult<CheckInResultDto>.Fail(statusCode, message);
}