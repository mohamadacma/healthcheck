public record UserResponseDto(
    int Id,
    string Name,
    string Email,
    List<string> Roles,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    bool IsActive

);