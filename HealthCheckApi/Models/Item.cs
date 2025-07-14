
using Microsoft.EntityFrameworkCore;

namespace HealthCheckApi.Models;

public class Item 
{
        public int Id { get; set; }
        public string Name {get; set; } = string.Empty;
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

