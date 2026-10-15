using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using Xunit;

namespace WebBanHang.Tests;

public sealed class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly StoreDbContext db;
    private readonly OrderService service;
    public OrderServiceTests()
    {
        connection.Open();
        db = new StoreDbContext(new DbContextOptionsBuilder<StoreDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Products.Add(new Product { Name = "Test product", Category = new Category { Name = "Test" }, Price = 100000, Stock = 10 });
        db.SaveChanges(); db.ChangeTracker.Clear();
        service = new OrderService(db);
    }
    private static CreateOrderInput Order(int quantity = 2) => new() {
        CustomerName = "Nguyễn An", DiscountPercent = 10,
        Lines = [new() { ProductId = 1, Quantity = quantity }] };
    [Fact] public async Task SavesTotalAndReducesStock()
    {
        var order = await service.CreateAsync(Order());
        Assert.Equal(180000, order.Total);
        Assert.Equal(8, (await db.Products.AsNoTracking().SingleAsync()).Stock);
        Assert.Equal(1, await db.Orders.CountAsync());
    }
    [Fact] public async Task InsufficientStockLeavesNoOrder()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(Order(11)));
        Assert.Equal(10, (await db.Products.AsNoTracking().SingleAsync()).Stock);
        Assert.Empty(await db.Orders.ToListAsync());
    }
    [Fact] public async Task DuplicateLinesCannotOversell()
    {
        var input = Order(6); input.Lines.Add(new() { ProductId = 1, Quantity = 6 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(input));
        Assert.Equal(10, (await db.Products.AsNoTracking().SingleAsync()).Stock);
    }
    [Fact] public async Task FailureOnLaterLineRollsBackEarlierStockChange()
    {
        var input = Order(); input.Lines.Add(new() { ProductId = 9999, Quantity = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(input));
        Assert.Equal(10, (await db.Products.AsNoTracking().SingleAsync()).Stock);
        Assert.Empty(await db.Orders.ToListAsync());
    }
    [Fact] public async Task CancelRestoresInventoryOnlyOnce()
    {
        var order = await service.CreateAsync(Order());
        await service.SetStatusAsync(order.Id, "Đã hủy");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetStatusAsync(order.Id, "Đã hủy"));
        Assert.Equal(10, (await db.Products.AsNoTracking().SingleAsync()).Stock);
    }
    [Fact] public async Task CompleteDoesNotRestoreInventory()
    {
        var order = await service.CreateAsync(Order()); await service.SetStatusAsync(order.Id, "Hoàn tất");
        Assert.Equal(8, (await db.Products.AsNoTracking().SingleAsync()).Stock);
    }
    [Fact] public async Task EmptyOrderIsRejected()
    {
        var input = Order(); input.Lines.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(input));
    }
    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
