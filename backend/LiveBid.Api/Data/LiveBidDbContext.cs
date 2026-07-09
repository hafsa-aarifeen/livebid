using LiveBid.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Data;

public class LiveBidDbContext(DbContextOptions<LiveBidDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.Property(a => a.StartingPrice).HasPrecision(12, 2);
            entity.Property(a => a.CurrentPrice).HasPrecision(12, 2);
            entity.Property(a => a.MinIncrement).HasPrecision(12, 2);

            // Postgres xmin system column as concurrency token
            entity.Property(a => a.Version).IsRowVersion();

            entity.HasOne(a => a.Seller)
                  .WithMany(u => u.AuctionsCreated)
                  .HasForeignKey(a => a.SellerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.EndsAt);
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.Property(b => b.Amount).HasPrecision(12, 2);

            entity.HasOne(b => b.Auction)
                  .WithMany(a => a.Bids)
                  .HasForeignKey(b => b.AuctionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Bidder)
                  .WithMany(u => u.Bids)
                  .HasForeignKey(b => b.BidderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.AuctionId, b.Amount });
        });
    }
}