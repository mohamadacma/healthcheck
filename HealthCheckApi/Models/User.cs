using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.Models
{

public class User 
{
    public int Id {get; set; }
    [Required(ErrorMessage="Name is required")]
    [StringLength(100,ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name {get; set; } = string.Empty;
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email {get; set; } = string.Empty;
    [Required(ErrorMessage = " Password hash required")]
    public string PasswordHash {get; set; } = string.Empty;
    public List<string> Roles {get; set; } = new List<string>();
    public DateTime CreatedAt {get; set; } = DateTime.UtcNow;
    public bool IsActive {get; set; } = true;
    public DateTime? LastLoginAt {get; set; } 
 }
}