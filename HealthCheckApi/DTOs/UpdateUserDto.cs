
public record UpdateUserDto(
    string? Name,
    string? Email,
    string? Password,
    List<string>? Roles
);