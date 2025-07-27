namespace HealthCheckApi.Data;
using HealthCheckApi.Models;
using Microsoft.EntityFrameworkCore;

public class ItemsDbContext : DbContext {
public DbSet<Item> Items { get; set; } 
public DbSet<User> Users { get; set; }

public ItemsDbContext(DbContextOptions<ItemsDbContext> options) : base(options) { }

// OnModelCreating to configure database schema
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Item>(entity =>
    {
        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Quantity)
            .IsRequired()
            .HasDefaultValue(0);
    });
    modelBuilder.Entity<User>(entity =>
    {
        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        entity.HasIndex(e => e.Email)
            .IsUnique();
            
        entity.Property(e => e.PasswordHash)
            .IsRequired();

        entity.Property(e => e.Roles)
        .HasConversion(
            v => string.Join(",", v),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .IsRequired();
        
    });
}
}
