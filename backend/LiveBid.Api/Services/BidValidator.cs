using LiveBid.Api.Models;

namespace LiveBid.Api.Services;

public record BidValidationResult(bool IsValid, string? Error, decimal? MinimumAcceptable = null)
{
    public static BidValidationResult Ok() => new(true, null);
    public static BidValidationResult Fail(string error, decimal? min = null) =>
        new(false, error, min);
}

public static class BidValidator
{
    public static BidValidationResult Validate(
        Auction auction,
        decimal amount,
        Guid bidderId,
        DateTime utcNow)
    {
        if (auction.Status != AuctionStatus.Live)
            return BidValidationResult.Fail("Auction is not live.");

        if (utcNow < auction.StartsAt || utcNow >= auction.EndsAt)
            return BidValidationResult.Fail("Auction is outside its bidding window.");

        var minimumAcceptable = auction.CurrentPrice + auction.MinIncrement;
        if (amount < minimumAcceptable)
            return BidValidationResult.Fail(
                $"Bid must be at least {minimumAcceptable:F2}.", minimumAcceptable);

        if (auction.SellerId == bidderId)
            return BidValidationResult.Fail("Sellers cannot bid on their own auctions.");

        return BidValidationResult.Ok();
    }
}