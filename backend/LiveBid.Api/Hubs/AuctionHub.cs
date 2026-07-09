using Microsoft.AspNetCore.SignalR;

namespace LiveBid.Api.Hubs;

public class AuctionHub : Hub
{
    // Clients call this when they open an auction detail page
    public async Task JoinAuction(string auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }

    // Called when they navigate away
    public async Task LeaveAuction(string auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }
}