namespace HealthCheckApi.Data;
using HealthCheckApi.Models;
using Microsoft.EntityFrameworkCore;

public class ItemsDbContext : DbContext {
public DbSet<Item> Items { get; set; } 

public ItemsDbContext(DbContextOptions<ItemsDbContext> options) : base(options) { }
}