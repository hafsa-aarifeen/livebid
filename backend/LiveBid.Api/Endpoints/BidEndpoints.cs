using System.Security.Claims;
using LiveBid.Api.Contracts;
using LiveBid.Api.Data;
using LiveBid.Api.Hubs;
using LiveBid.Api.Models;
using LiveBid.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LiveBid.Api.Endpoints;

public static class BidEndpoints
{
    private const int MaxRetries = 3;

    public static void MapBidEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auctions/{id:guid}/bids",
            async (Guid id,
                   PlaceBidRequest request,
                   ClaimsPrincipal principal,
                   LiveBidDbContext db,
                   IHubContext<AuctionHub> hub) =>
        {
            var bidderIdRaw =
                principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(bidderIdRaw, out var bidderId))
                return Results.Unauthorized();

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                // Fresh load each attempt — tracked, because we intend to update
                var auction = await db.Auctions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (auction is null)
                    return Results.NotFound(new { error = "Auction not found." });

                // --- Validation gauntlet ---
                var validation = BidValidator.Validate(
                    auction, request.Amount, bidderId, DateTime.UtcNow);

                if (!validation.IsValid)
                    return Results.BadRequest(new
                    {
                        error = validation.Error,
                        minimumAcceptable = validation.MinimumAcceptable
                    });

                var bidder = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == bidderId);

                if (bidder is null)
                    return Results.BadRequest(new { error = "Unknown bidder." });

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
        }).RequireAuthorization();
    }
}