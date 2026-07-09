namespace LiveBid.Api.Models;

public enum AuctionStatus
{
    Scheduled,
    Live,
    Ended,
    Cancelled
}

public class Auction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinIncrement { get; set; } = 1.00m;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Scheduled;

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    public Guid? WinningBidId { get; set; }

    // Optimistic concurrency token — critical for simultaneous bids
    public uint Version { get; set; }

    public List<Bid> Bids { get; set; } = [];
}