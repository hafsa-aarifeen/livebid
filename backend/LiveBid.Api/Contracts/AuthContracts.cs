namespace LiveBid.Api.Contracts;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, Guid UserId, string Username);