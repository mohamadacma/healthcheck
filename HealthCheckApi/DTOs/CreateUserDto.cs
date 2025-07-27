
namespace HealthCheckApi.DTOs
{

public record CreateUserDto(
    string Name,
    string Email,
    string Password,
    List<string> Roles
);
}