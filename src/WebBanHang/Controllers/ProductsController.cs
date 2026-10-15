using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers;

public class ProductsController(StoreDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int? categoryId)
    {
        var products = db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) products = products.Where(p => p.Name.Contains(q));
        if (categoryId.HasValue) products = products.Where(p => p.CategoryId == categoryId);
        ViewBag.Query = q; ViewBag.CategoryId = categoryId;
        await CategoriesAsync(categoryId);
        return View(await products.OrderBy(p => p.Name).ToListAsync());
    }
    private async Task CategoriesAsync(int? selected = null) =>
        ViewBag.Categories = new SelectList(await db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);
    public async Task<IActionResult> Create() { await CategoriesAsync(); return View("Edit", new Product()); }
    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        await CategoriesAsync(product.CategoryId); return View(product);
    }
    [HttpPost]
    public async Task<IActionResult> Save([Bind("Id,Name,CategoryId,Price,Stock")] Product input)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == input.CategoryId)) ModelState.AddModelError("CategoryId", "Danh mục không tồn tại.");
        if (!ModelState.IsValid) { await CategoriesAsync(input.CategoryId); return View("Edit", input); }
        if (input.Id == 0) db.Products.Add(input);
        else
        {
            var current = await db.Products.FindAsync(input.Id);
            if (current is null) return NotFound();
            current.Name = input.Name; current.CategoryId = input.CategoryId; current.Price = input.Price; current.Stock = input.Stock;
        }
        await db.SaveChangesAsync(); TempData["Success"] = "Đã lưu sản phẩm.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        if (await db.OrderItems.AnyAsync(i => i.ProductId == id)) TempData["Error"] = "Sản phẩm đã có trong đơn hàng, không thể xóa.";
        else { db.Products.Remove(product); await db.SaveChangesAsync(); TempData["Success"] = "Đã xóa sản phẩm."; }
        return RedirectToAction(nameof(Index));
    }
}
