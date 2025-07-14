namespace HealthCheckApi.DTOs;

public class ItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}