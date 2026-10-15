using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrder>().HasMany(o => o.Items).WithOne()
            .HasForeignKey(i => i.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
