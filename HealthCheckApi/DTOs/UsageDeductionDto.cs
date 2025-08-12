using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.DTOs;

public class UsageDeductionDto
{
    [Required(ErrorMessage = "Amount is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }

    [StringLength(100, ErrorMessage = "Reason cannot exceed 100 characters")]
    public string? Reason { get; set; }

    [StringLength(50, ErrorMessage = "User cannot exceed 50 characters")]
    public string? User { get; set; }
}