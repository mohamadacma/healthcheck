namespace HealthCheckApi.DTOs;

public class ItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }

    public string? Category { get; set; }
    public string? Location { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? ReorderLevel { get; set; }
    public DateTime? LastUsed { get; set; } // from usageHistory
}