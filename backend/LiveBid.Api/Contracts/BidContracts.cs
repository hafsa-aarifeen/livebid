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

public record AuctionStartedEvent(Guid AuctionId, DateTime EndsAt);

public record AuctionEndedEvent(
    Guid AuctionId,
    decimal FinalPrice,
    string? Winner,
    Guid? WinningBidId);