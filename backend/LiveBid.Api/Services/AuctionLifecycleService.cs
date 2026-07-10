using LiveBid.Api.Contracts;
using LiveBid.Api.Data;
using LiveBid.Api.Hubs;
using LiveBid.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Services;

public class AuctionLifecycleService(
    IServiceScopeFactory scopeFactory,
    IHubContext<AuctionHub> hub,
    ILogger<AuctionLifecycleService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Auction lifecycle service started");

        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await TransitionAuctionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Never let one bad tick kill the worker
                logger.LogError(ex, "Lifecycle tick failed");
            }
        }
    }

    private async Task TransitionAuctionsAsync(CancellationToken ct)
    {
        // BackgroundService is a singleton; DbContext is scoped.
        // A fresh scope per tick is the standard pattern.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LiveBidDbContext>();

        var now = DateTime.UtcNow;

        // --- Scheduled → Live ---
        var toStart = await db.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartsAt <= now)
            .ToListAsync(ct);

        foreach (var auction in toStart)
        {
            auction.Status = AuctionStatus.Live;
        }

        // --- Live → Ended ---
        var toEnd = await db.Auctions
            .Where(a => a.Status == AuctionStatus.Live && a.EndsAt <= now)
            .Include(a => a.Bids.OrderByDescending(b => b.Amount).Take(1))
            .ThenInclude(b => b.Bidder)
            .ToListAsync(ct);

        foreach (var auction in toEnd)
        {
            auction.Status = AuctionStatus.Ended;
            var winningBid = auction.Bids.FirstOrDefault();
            auction.WinningBidId = winningBid?.Id;
        }

        if (toStart.Count == 0 && toEnd.Count == 0) return;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A bid raced our status write; skip this tick — the next
            // tick (5s away) will retry with fresh state.
            logger.LogWarning("Lifecycle transition lost a concurrency race; retrying next tick");
            return;
        }

        // Broadcast only after the DB commit succeeded
        foreach (var auction in toStart)
        {
            logger.LogInformation("Auction {Id} is now Live", auction.Id);
            await hub.Clients
                .Group($"auction-{auction.Id}")
                .SendAsync("AuctionStarted",
                    new AuctionStartedEvent(auction.Id, auction.EndsAt), ct);
        }

        foreach (var auction in toEnd)
        {
            var winningBid = auction.Bids.FirstOrDefault();
            logger.LogInformation("Auction {Id} ended at {Price}", auction.Id, auction.CurrentPrice);
            await hub.Clients
                .Group($"auction-{auction.Id}")
                .SendAsync("AuctionEnded",
                    new AuctionEndedEvent(
                        auction.Id,
                        auction.CurrentPrice,
                        winningBid?.Bidder?.Username,
                        winningBid?.Id), ct);
        }
    }
}