using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers;

public class HomeController(StoreDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var totals = await db.Orders.Where(o => o.Status != "Đã hủy").Select(o => o.Total).ToListAsync();
        return View(new DashboardViewModel(await db.Products.CountAsync(), await db.Categories.CountAsync(),
            await db.Orders.CountAsync(), totals.Sum(),
            await db.Orders.OrderByDescending(o => o.Id).Take(5).ToListAsync(),
            await db.Products.Include(p => p.Category).Where(p => p.Stock <= 8).OrderBy(p => p.Stock).ToListAsync()));
    }
    public IActionResult Error() { Response.StatusCode = 500; return View(); }
}
