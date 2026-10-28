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

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Property>().HasIndex(p => p.Code).IsUnique();
        b.Entity<RoomType>().Property(r => r.BaseRate).HasPrecision(18, 2);
        b.Entity<RoomType>().Property(r => r.SizeSqm).HasPrecision(8, 2);
        b.Entity<Room>().HasIndex(r => new { r.PropertyId, r.RoomNo }).IsUnique();
        b.Entity<Room>().Property(r => r.Status).HasConversion<string>();
    }
}