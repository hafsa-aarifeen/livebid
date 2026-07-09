using LiveBid.Api.Data;
using LiveBid.Api.Endpoints;
using LiveBid.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LiveBidDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LiveBid")));

builder.Services.AddSignalR();

var app = builder.Build();

// Seed on startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LiveBidDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/", () => "LiveBid API is running");
app.MapAuctionEndpoints();
app.MapBidEndpoints();
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();