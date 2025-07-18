using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.DTOs;
/// <summary>
/// Data transfer object for creating a new item
/// </summary>
public class CreateItemDto
{
    /// <summary>
    /// The name of the item (required, max 100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The quantity of the item (must be non-negative)
    /// </summary>
    public int Quantity { get; set; }
}