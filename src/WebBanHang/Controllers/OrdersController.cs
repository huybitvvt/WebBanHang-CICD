using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers;

public class OrdersController(StoreDbContext db, OrderService orders) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Orders.OrderByDescending(o => o.Id).ToListAsync());
    public async Task<IActionResult> Details(int id) =>
        await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id) is { } order ? View(order) : NotFound();
    public async Task<IActionResult> Create() { await ProductsAsync(); return View(new CreateOrderInput()); }
    private async Task ProductsAsync() => ViewBag.Products = await db.Products.AsNoTracking().Where(p => p.Stock > 0).OrderBy(p => p.Name).ToListAsync();
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var order = await orders.CreateAsync(input);
                TempData["Success"] = "Đã tạo đơn hàng và cập nhật tồn kho.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await ProductsAsync(); return View(input);
    }
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        if (!await db.Orders.AnyAsync(o => o.Id == id)) return NotFound();
        try { await orders.SetStatusAsync(id, status); TempData["Success"] = "Đã cập nhật trạng thái đơn hàng."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
