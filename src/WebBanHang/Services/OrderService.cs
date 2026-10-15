using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services;

public class OrderService(StoreDbContext db)
{
    public async Task<SalesOrder> CreateAsync(CreateOrderInput input)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName)) throw new InvalidOperationException("Nhập tên khách hàng.");
        if (input.DiscountPercent is < 0 or > 100) throw new InvalidOperationException("Giảm giá phải từ 0 đến 100%.");
        if (input.Lines.Count is < 1 or > 50 || input.Lines.Any(l => l.ProductId <= 0 || l.Quantity is < 1 or > 10000))
            throw new InvalidOperationException("Chọn sản phẩm và số lượng hợp lệ cho từng dòng.");
        var lines = input.Lines.GroupBy(l => l.ProductId)
            .Select(g => new OrderLineInput { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) }).ToList();
        // Price is always loaded from the database; inventory changes and order creation share a transaction.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new SalesOrder { CustomerName = input.CustomerName.Trim(), Phone = input.Phone,
            DiscountPercent = input.DiscountPercent };
        foreach (var line in lines)
        {
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == line.ProductId)
                ?? throw new InvalidOperationException("Sản phẩm không còn tồn tại.");
            var updated = await db.Products.Where(p => p.Id == line.ProductId && p.Stock >= line.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - line.Quantity));
            if (updated != 1) throw new InvalidOperationException($"{product.Name}: không đủ tồn kho.");
            order.Items.Add(new OrderItem { ProductId = product.Id, ProductName = product.Name,
                UnitPrice = product.Price, Quantity = line.Quantity });
        }
        order.Total = OrderCalculator.CalculateTotal(order.Items.Select(i => (i.UnitPrice, i.Quantity)), input.DiscountPercent);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order;
    }

    public async Task SetStatusAsync(int id, string status)
    {
        if (status is not ("Hoàn tất" or "Đã hủy")) throw new InvalidOperationException("Trạng thái không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == id);
        if (order.Status != "Mới") throw new InvalidOperationException("Đơn đã xử lý không thể thay đổi lần nữa.");
        if (status == "Đã hủy")
            foreach (var item in order.Items)
                await db.Products.Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
        order.Status = status;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
