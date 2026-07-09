using LiveBid.Api.Contracts;
using LiveBid.Api.Data;
using LiveBid.Api.Hubs;
using LiveBid.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Endpoints;

public static class BidEndpoints
{
    private const int MaxRetries = 3;

    public static void MapBidEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auctions/{id:guid}/bids",
            async (Guid id,
                   PlaceBidRequest request,
                   LiveBidDbContext db,
                   IHubContext<AuctionHub> hub) =>
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                // Fresh load each attempt — tracked, because we intend to update
                var auction = await db.Auctions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (auction is null)
                    return Results.NotFound(new { error = "Auction not found." });

                // --- Validation gauntlet ---
                var now = DateTime.UtcNow;

                if (auction.Status != AuctionStatus.Live)
                    return Results.BadRequest(new { error = "Auction is not live." });

                if (now < auction.StartsAt || now >= auction.EndsAt)
                    return Results.BadRequest(new { error = "Auction is outside its bidding window." });

                var minimumAcceptable = auction.CurrentPrice + auction.MinIncrement;
                if (request.Amount < minimumAcceptable)
                    return Results.BadRequest(new
                    {
                        error = $"Bid must be at least {minimumAcceptable:F2}.",
                        minimumAcceptable
                    });

                var bidder = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.BidderId);

                if (bidder is null)
                    return Results.BadRequest(new { error = "Unknown bidder." });

                if (auction.SellerId == bidder.Id)
                    return Results.BadRequest(new { error = "Sellers cannot bid on their own auctions." });

                // --- Apply the bid ---
                var bid = new Bid
                {
                    AuctionId = auction.Id,
                    BidderId = bidder.Id,
                    Amount = request.Amount
                };

                auction.CurrentPrice = request.Amount;
                db.Bids.Add(bid);

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another bid won the race — xmin changed under us.
                    // Reset and retry: re-validate against the NEW price.
                    db.ChangeTracker.Clear();

                    if (attempt == MaxRetries)
                        return Results.Conflict(new
                        {
                            error = "Auction is receiving heavy bidding. Please try again."
                        });

                    continue;
                }

                // --- Broadcast to everyone watching this auction ---
                var bidCount = await db.Bids.CountAsync(b => b.AuctionId == auction.Id);

                var evt = new BidPlacedEvent(
                    bid.Id,
                    auction.Id,
                    bid.Amount,
                    bidder.Username,
                    bid.PlacedAt,
                    auction.CurrentPrice,
                    bidCount);

                await hub.Clients
                    .Group($"auction-{auction.Id}")
                    .SendAsync("BidPlaced", evt);

                return Results.Created($"/api/auctions/{auction.Id}/bids/{bid.Id}", evt);
            }

            return Results.Conflict(new { error = "Unable to place bid." });
        });
    }
}