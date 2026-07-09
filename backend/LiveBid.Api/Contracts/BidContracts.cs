namespace LiveBid.Api.Contracts;

public record PlaceBidRequest(decimal Amount, Guid BidderId);

public record BidPlacedEvent(
    Guid BidId,
    Guid AuctionId,
    decimal Amount,
    string Bidder,
    DateTime PlacedAt,
    decimal NewCurrentPrice,
    int BidCount);