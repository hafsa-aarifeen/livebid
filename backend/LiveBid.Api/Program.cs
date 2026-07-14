using System.Text;
using LiveBid.Api.Data;
using LiveBid.Api.Endpoints;
using LiveBid.Api.Hubs;
using LiveBid.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LiveBidDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LiveBid")));

builder.Services.AddSignalR();
builder.Services.AddHostedService<AuctionLifecycleService>();
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:5500",
                "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Seed on startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LiveBidDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/", () => "LiveBid API is running");
app.MapAuctionEndpoints();
app.MapBidEndpoints();
app.MapAuthEndpoints();
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();