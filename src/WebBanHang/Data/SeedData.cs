using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data;

public static class SeedData
{
    public static async Task InitializeAsync(StoreDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Categories.AnyAsync()) return;
        var accessories = new Category { Name = "Phụ kiện", Description = "Phụ kiện cho góc làm việc" };
        var computers = new Category { Name = "Máy tính", Description = "Thiết bị phục vụ công việc" };
        var audio = new Category { Name = "Âm thanh", Description = "Âm thanh và giải trí" };
        db.Products.AddRange(
            new Product { Name = "Bàn phím cơ Keychron K2", Category = accessories, Price = 1890000, Stock = 24 },
            new Product { Name = "Chuột Logitech MX Master 3S", Category = accessories, Price = 2490000, Stock = 18 },
            new Product { Name = "Tai nghe Sony WH-CH720N", Category = audio, Price = 1990000, Stock = 8 },
            new Product { Name = "Laptop ASUS Vivobook 14", Category = computers, Price = 15990000, Stock = 5 },
            new Product { Name = "Hub USB-C 6 trong 1", Category = accessories, Price = 590000, Stock = 32 },
            new Product { Name = "Loa Bluetooth JBL Go 4", Category = audio, Price = 990000, Stock = 12 });
        await db.SaveChangesAsync();
    }
}
