using LiveBid.Api.Contracts;
using LiveBid.Api.Data;
using LiveBid.Api.Models;
using LiveBid.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LiveBid.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            LiveBidDbContext db,
            TokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                request.Password.Length < 8)
            {
                return Results.BadRequest(new
                {
                    error = "Username, email, and a password of at least 8 characters are required."
                });
            }

            var exists = await db.Users.AnyAsync(u =>
                u.Username == request.Username || u.Email == request.Email);

            if (exists)
                return Results.Conflict(new { error = "Username or email already taken." });

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}",
                new AuthResponse(tokens.CreateToken(user), user.Id, user.Username));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            LiveBidDbContext db,
            TokenService tokens) =>
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Same error for unknown user and wrong password — don't leak
            // which usernames exist (user enumeration).
            if (user is null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Results.Json(
                    new { error = "Invalid username or password." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(
                new AuthResponse(tokens.CreateToken(user), user.Id, user.Username));
        });
    }
}