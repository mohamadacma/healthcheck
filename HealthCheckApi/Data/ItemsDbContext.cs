namespace HealthCheckApi.Data;
using HealthCheckApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class ItemsDbContext : DbContext {
public DbSet<Item> Items { get; set; } 
public DbSet<User> Users { get; set; }
public DbSet<UsageRecord> UsageRecords { get; set; }

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
            .IsRequired()
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));
    });

    //configure UsageRecord
    modelBuilder.Entity<UsageRecord>(entity =>
    {
        entity.Property(e => e.Amount)
            .IsRequired();
        entity.Property(e => e.Date)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.Reason)
            .HasMaxLength(100);
        entity.Property(e => e.User)
            .HasMaxLength(50);

    // Relationship: One Item to Many UsageRecords
        entity.HasOne(ur => ur.Item)
            .WithMany(i => i.UsageHistory)
            .HasForeignKey(ur => ur.ItemId)
            .OnDelete(DeleteBehavior.Cascade); // Delete records if item is deleted
    });     
    }
}
