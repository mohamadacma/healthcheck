
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HealthCheckApi.Models;

public class Item 
{
        public int Id { get; set; }
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name {get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int Quantity {get; set; } 
        public DateTime LastUpdated { get; set; }

        //constructor
        public Item () {}
        
        public Item(string name, int quantity)
        {
            Name = name;
            Quantity =  quantity;
            LastUpdated = DateTime.UtcNow;
        }

}

