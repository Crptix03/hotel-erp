using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Properties.AnyAsync()) return;

        var wifi = new Amenity { Name = "Wi-Fi", Category = "Connectivity" };
        var tv = new Amenity { Name = "TV", Category = "Entertainment" };
        var balcony = new Amenity { Name = "Balcony", Category = "Room" };
        var minibar = new Amenity { Name = "Minibar", Category = "Food & Drink" };

        var standard = new RoomType
        {
            Name = "Standard",
            Description = "Comfortable room with a city view",
            BedType = "Double",
            BedCount = 1,
            SizeSqm = 22,
            MaxAdults = 2,
            MaxChildren = 1,
            ViewType = "City",
            SmokingAllowed = false,
            BaseRate = 1500,
            Amenities = { wifi, tv }
        };
        var deluxe = new RoomType
        {
            Name = "Deluxe",
            Description = "Spacious room with a balcony",
            BedType = "King",
            BedCount = 1,
            SizeSqm = 30,
            MaxAdults = 2,
            MaxChildren = 2,
            ViewType = "Garden",
            SmokingAllowed = false,
            BaseRate = 2500,
            Amenities = { wifi, tv, balcony }
        };
        var suite = new RoomType
        {
            Name = "Suite",
            Description = "Two-bed suite with a living area",
            BedType = "King",
            BedCount = 2,
            SizeSqm = 55,
            MaxAdults = 4,
            MaxChildren = 2,
            ViewType = "Garden",
            SmokingAllowed = false,
            BaseRate = 5000,
            Amenities = { wifi, tv, balcony, minibar }
        };

        var hotel = new Property
        {
            Code = "GDH",
            Name = "Gezana Demo Hotel",
            Address = "Addis Ababa, Ethiopia"
        };
        db.Properties.Add(hotel);

        var rooms = new List<Room>();
        for (int i = 1; i <= 4; i++)
            rooms.Add(new Room { Property = hotel, RoomType = standard, RoomNo = $"10{i}", Floor = 1 });
        for (int i = 1; i <= 3; i++)
            rooms.Add(new Room { Property = hotel, RoomType = deluxe, RoomNo = $"20{i}", Floor = 2 });
        rooms.Add(new Room { Property = hotel, RoomType = suite, RoomNo = "301", Floor = 3 });
        db.Rooms.AddRange(rooms);

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2030, 12, 31);
        var roomOnly = new RatePlan { Name = "Room Only", MealPlan = "None", Price = 0, ValidFrom = from, ValidTo = to };
        var bnb = new RatePlan { Name = "Bed & Breakfast", MealPlan = "Breakfast", Price = 300, ValidFrom = from, ValidTo = to };
        roomOnly.Rules.Add(new RateRule
        {
            RuleType = RateRuleType.LengthOfStay,
            MinNights = 7,
            AdjustmentType = "Percent",
            AdjustmentValue = -10
        });
        db.RatePlans.AddRange(roomOnly, bnb);

        db.Users.AddRange(
            new AppUser
            {
                Name = "Hotel Manager",
                Email = "manager@gezana.local",
                Role = "Manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123")
            },
            new AppUser
            {
                Name = "Front Desk",
                Email = "frontdesk@gezana.local",
                Role = "FrontDesk",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Front@123")
            });

        await db.SaveChangesAsync();
    }
}