using LiveBid.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(LiveBidDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            // Backfill: give pre-auth seed users a real password
            var placeholders = await db.Users
                .Where(u => u.PasswordHash == "placeholder")
                .ToListAsync();
            foreach (var u in placeholders)
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            if (placeholders.Count > 0) await db.SaveChangesAsync();
            return;
        }

        var alice = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        var bob = new User
        {
            Username = "bob",
            Email = "bob@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };

        var now = DateTime.UtcNow;

        var auctions = new List<Auction>
        {
            new()
            {
                Title = "Vintage Mechanical Keyboard",
                Description = "1987 IBM Model M, fully restored. Buckling springs, original cable.",
                StartingPrice = 50.00m,
                CurrentPrice = 50.00m,
                MinIncrement = 5.00m,
                StartsAt = now.AddMinutes(-30),
                EndsAt = now.AddHours(2),
                Status = AuctionStatus.Live,
                Seller = alice
            },
            new()
            {
                Title = "Sri Lankan Ceylon Tea Chest",
                Description = "Antique wooden tea chest from a Nuwara Eliya estate, early 1900s.",
                StartingPrice = 120.00m,
                CurrentPrice = 120.00m,
                MinIncrement = 10.00m,
                StartsAt = now.AddMinutes(-10),
                EndsAt = now.AddHours(6),
                Status = AuctionStatus.Live,
                Seller = bob
            },
            new()
            {
                Title = "First-Edition Programming Book",
                Description = "The Pragmatic Programmer, 1st edition, 1999. Good condition.",
                StartingPrice = 30.00m,
                CurrentPrice = 30.00m,
                MinIncrement = 2.00m,
                StartsAt = now.AddHours(3),
                EndsAt = now.AddDays(1),
                Status = AuctionStatus.Scheduled,
                Seller = alice
            }
        };

        db.Users.AddRange(alice, bob);
        db.Auctions.AddRange(auctions);
        await db.SaveChangesAsync();
    }
}