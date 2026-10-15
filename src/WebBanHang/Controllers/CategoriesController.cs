using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers;

public class CategoriesController(StoreDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var categories = db.Categories.Include(c => c.Products).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) categories = categories.Where(c => c.Name.Contains(q.Trim()));
        ViewBag.Query = q;
        return View(await categories.OrderBy(c => c.Name).ToListAsync());
    }
    public IActionResult Create() => View("Edit", new Category());
    public async Task<IActionResult> Edit(int id) => await db.Categories.FindAsync(id) is { } c ? View(c) : NotFound();
    [HttpPost]
    public async Task<IActionResult> Save([Bind("Id,Name,Description")] Category input)
    {
        if (!ModelState.IsValid) return View("Edit", input);
        input.Name = input.Name.Trim();
        input.Description = input.Description?.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == input.Name && c.Id != input.Id))
        { ModelState.AddModelError("Name", "Tên danh mục đã tồn tại."); return View("Edit", input); }
        if (input.Id == 0) db.Categories.Add(input);
        else
        {
            var current = await db.Categories.FindAsync(input.Id);
            if (current is null) return NotFound();
            current.Name = input.Name; current.Description = input.Description;
        }
        await db.SaveChangesAsync(); TempData["Success"] = "Đã lưu danh mục.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.Include(c => c.Products).SingleOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        if (category.Products.Count != 0) TempData["Error"] = "Hãy chuyển hoặc xóa sản phẩm trước khi xóa danh mục.";
        else { db.Categories.Remove(category); await db.SaveChangesAsync(); TempData["Success"] = "Đã xóa danh mục."; }
        return RedirectToAction(nameof(Index));
    }
}
