using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.DTOs;

public class CreateItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}