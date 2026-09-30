using Microsoft.EntityFrameworkCore;
using KitchenFlow.Models;

namespace KitchenFlow.Data;

public sealed class DbContext(DbContextOptions<DbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();

        order.ToTable("orders");
        order.HasKey(entity => entity.Id);

        order.Property(entity => entity.Id)
            .HasColumnName("id");

        order.Property(entity => entity.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(120)
            .IsRequired();

        order.Property(entity => entity.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20)
            .IsRequired();

        order.Property(entity => entity.Items)
            .HasColumnName("items")
            .HasColumnType("jsonb")
            .IsRequired();

        order.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        order.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
