using LiveBid.Api.Data;
using LiveBid.Api.Endpoints;
using LiveBid.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LiveBidDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LiveBid")));

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:4200",   // Angular dev server (later)
                "http://localhost:5500",   // test page (Live Server / file preview)
                "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());       // required for SignalR
});

var app = builder.Build();

app.UseCors("Frontend");

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