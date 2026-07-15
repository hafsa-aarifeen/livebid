using LiveBid.Api.Models;
using LiveBid.Api.Services;

namespace LiveBid.Api.Tests;

public class BidValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BidderId = Guid.NewGuid();

    private static Auction LiveAuction(decimal currentPrice = 100m, decimal minIncrement = 5m) =>
        new()
        {
            Status = AuctionStatus.Live,
            StartsAt = Now.AddHours(-1),
            EndsAt = Now.AddHours(1),
            CurrentPrice = currentPrice,
            MinIncrement = minIncrement,
            SellerId = SellerId,
        };

    [Fact]
    public void Accepts_valid_bid()
    {
        var result = BidValidator.Validate(LiveAuction(), 105m, BidderId, Now);

        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(AuctionStatus.Scheduled)]
    [InlineData(AuctionStatus.Ended)]
    [InlineData(AuctionStatus.Cancelled)]
    public void Rejects_when_not_live(AuctionStatus status)
    {
        var auction = LiveAuction();
        auction.Status = status;

        var result = BidValidator.Validate(auction, 105m, BidderId, Now);

        Assert.False(result.IsValid);
        Assert.Equal("Auction is not live.", result.Error);
    }

    [Fact]
    public void Rejects_bid_after_end_time_even_if_status_still_live()
    {
        // The lifecycle service polls every 5s — a bid can arrive in the gap
        // where EndsAt has passed but Status hasn't flipped yet.
        var auction = LiveAuction();
        var justAfterEnd = auction.EndsAt.AddSeconds(1);

        var result = BidValidator.Validate(auction, 105m, BidderId, justAfterEnd);

        Assert.False(result.IsValid);
        Assert.Equal("Auction is outside its bidding window.", result.Error);
    }

    [Fact]
    public void Rejects_bid_exactly_at_end_time()
    {
        var auction = LiveAuction();

        var result = BidValidator.Validate(auction, 105m, BidderId, auction.EndsAt);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(104.99)]
    [InlineData(100.00)]
    [InlineData(0.01)]
    public void Rejects_bid_below_minimum(decimal amount)
    {
        var result = BidValidator.Validate(LiveAuction(), amount, BidderId, Now);

        Assert.False(result.IsValid);
        Assert.Equal(105m, result.MinimumAcceptable);
        Assert.StartsWith("Bid must be at least", result.Error);
    }

    [Fact]
    public void Accepts_bid_exactly_at_minimum()
    {
        var result = BidValidator.Validate(LiveAuction(), 105m, BidderId, Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_seller_bidding_on_own_auction()
    {
        var result = BidValidator.Validate(LiveAuction(), 105m, SellerId, Now);

        Assert.False(result.IsValid);
        Assert.Equal("Sellers cannot bid on their own auctions.", result.Error);
    }
}