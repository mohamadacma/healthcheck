using HealthCheckApi.Models;
using HealthCheckApi.DTOs;

namespace HealthCheckApi.Extensions;

public static class ItemExtensions
{
    public static Item ToItem(this CreateItemDto dto) =>
        new Item { Name = dto.Name, Quantity = dto.Quantity, LastUpdated = DateTime.UtcNow };

    public static void UpdateFromDto(this Item item, UpdateItemDto dto)
    {
        item.Name = dto.Name;
        item.Quantity = dto.Quantity;
        item.LastUpdated = DateTime.UtcNow;
    }

    public static ItemResponseDto ToResponseDto(this Item item) =>
        new ItemResponseDto { Id = item.Id, Name = item.Name, Quantity = item.Quantity };

    public static UserResponseDto ToResponseDto(this User user) =>
        new UserResponseDto(
            user.Id,
            user.Name,
            user.Email,
            user.Roles,
            user.LastLoginAt,
            user.CreatedAt,
            user.IsActive
        );
}