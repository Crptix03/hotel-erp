using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<GuestIdDocument> GuestIdDocuments => Set<GuestIdDocument>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RateRule> RateRules => Set<RateRule>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<FolioLine> FolioLines => Set<FolioLine>();
    public DbSet<GuestRequest> GuestRequests => Set<GuestRequest>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Property>().HasIndex(p => p.Code).IsUnique();
        b.Entity<RoomType>().Property(r => r.BaseRate).HasPrecision(18, 2);
        b.Entity<RoomType>().Property(r => r.SizeSqm).HasPrecision(8, 2);
        b.Entity<Room>().HasIndex(r => new { r.PropertyId, r.RoomNo }).IsUnique();
        b.Entity<Room>().Property(r => r.Status).HasConversion<string>();
        b.Entity<RatePlan>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<RateRule>().Property(x => x.AdjustmentValue).HasPrecision(18, 2);
        b.Entity<RateRule>().Property(x => x.RuleType).HasConversion<string>();
        b.Entity<Reservation>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Entity<Reservation>().Property(x => x.Source).HasConversion<string>();
        b.Entity<Reservation>().HasIndex(x => x.ReservationNo).IsUnique();
        b.Entity<ReservationItem>().Property(x => x.NightlyRate).HasPrecision(18, 2);
        b.Entity<Folio>().Property(x => x.Balance).HasPrecision(18, 2);
        b.Entity<FolioLine>().Property(x => x.Amount).HasPrecision(18, 2);
        b.Entity<FolioLine>().Property(x => x.Type).HasConversion<string>();
        b.Entity<FolioLine>().Property(x => x.PaymentMethod).HasConversion<string>();
        b.Entity<GuestRequest>().Property(x => x.Charge).HasPrecision(18, 2);
        b.Entity<GuestRequest>().Property(x => x.RequestType).HasConversion<string>();
        b.Entity<GuestIdDocument>().Property(x => x.IdType).HasConversion<string>();
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
    }
}