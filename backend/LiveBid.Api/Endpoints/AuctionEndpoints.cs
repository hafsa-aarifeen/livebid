using LiveBid.Api.Data;
using LiveBid.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Endpoints;

public static class AuctionEndpoints
{
    public static void MapAuctionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auctions");

        // GET /api/auctions?status=Live
        group.MapGet("/", async (LiveBidDbContext db, AuctionStatus? status) =>
        {
            var query = db.Auctions.AsNoTracking();

            if (status is not null)
                query = query.Where(a => a.Status == status);

            var auctions = await query
                .OrderBy(a => a.EndsAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.ImageUrl,
                    a.CurrentPrice,
                    a.MinIncrement,
                    a.StartsAt,
                    a.EndsAt,
                    Status = a.Status.ToString(),
                    Seller = a.Seller!.Username,
                    BidCount = a.Bids.Count
                })
                .ToListAsync();

            return Results.Ok(auctions);
        });

        // GET /api/auctions/{id} — detail with bid history
        group.MapGet("/{id:guid}", async (LiveBidDbContext db, Guid id) =>
        {
            var auction = await db.Auctions
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.ImageUrl,
                    a.StartingPrice,
                    a.CurrentPrice,
                    a.MinIncrement,
                    a.StartsAt,
                    a.EndsAt,
                    Status = a.Status.ToString(),
                    Seller = a.Seller!.Username,
                    Bids = a.Bids
                        .OrderByDescending(b => b.Amount)
                        .Select(b => new
                        {
                            b.Id,
                            b.Amount,
                            b.PlacedAt,
                            Bidder = b.Bidder!.Username
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return auction is null ? Results.NotFound() : Results.Ok(auction);
        });
    }
}