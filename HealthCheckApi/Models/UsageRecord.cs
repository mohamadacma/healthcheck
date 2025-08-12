using System.ComponentModel.DataAnnotations;
using HealthCheckApi.Models;


public class UsageRecord
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    [Required]
    public int Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; } //patient use( ex)
    public string? User { get; set; }

    public Item? Item { get; set; }
}