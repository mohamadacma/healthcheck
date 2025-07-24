namespace HealthCheckApi.DTOs

public record LoginRequest(
    string Email,
    string Password
);

public record RegisterRequest(
    string Name,
    string Email,
    string Password
);


public record LoginResponse(
    string Token,   
    string Email,
    string Name,
    string Role,
    DateTime? ExpiresAt
);