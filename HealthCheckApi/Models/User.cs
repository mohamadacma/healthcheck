using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.Models

public class User 
{
    public int Id {get; set }
    public string Name {get; set; } = string.Empty;
    public string Email {get; set; } = string.Empty;
    public string PasswordHash {get; set; } = string.Empty;
    public List<string> Roles {get; set; } = new List<string>();
    public DateTime CreatedAt {get; set; } = DateTime.UtcNow;
}