namespace LiveBid.Api.Models;

public class Bid
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    public Guid AuctionId { get; set; }
    public Auction? Auction { get; set; }

    public Guid BidderId { get; set; }
    public User? Bidder { get; set; }
}