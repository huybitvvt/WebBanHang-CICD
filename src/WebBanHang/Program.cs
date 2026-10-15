using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Services;

var builder = WebApplication.CreateBuilder(args);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
var dataDirectory = Path.GetFullPath(builder.Configuration["DataDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(dataDirectory);
builder.Services.AddDataProtection().SetApplicationName("WebBanHang")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "store.db")}"));
builder.Services.AddScoped<OrderService>();
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
var pathBase = builder.Configuration["PathBase"];
// IIS supplies PathBase itself; UsePathBase also supports the local Kestrel rehearsal.
if (!string.IsNullOrWhiteSpace(pathBase)) app.UsePathBase(pathBase);
app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", async (StoreDbContext db) =>
{
    await db.Categories.CountAsync();
    return Results.Ok(new {
        status = "Healthy", application = "WebBanHang",
        version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    });
});
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
using (var scope = app.Services.CreateScope())
    await SeedData.InitializeAsync(scope.ServiceProvider.GetRequiredService<StoreDbContext>());
app.Run();

public partial class Program { }
