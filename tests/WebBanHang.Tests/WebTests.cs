using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebBanHang.Data;
using Xunit;

namespace WebBanHang.Tests;

public sealed class StoreFactory : WebApplicationFactory<Program>
{
    private readonly string dataPath = Path.Combine(Path.GetTempPath(), "WebBanHang-tests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["DataDirectory"] = dataPath, ["PathBase"] = "/webbanhang" }))
        .ConfigureServices(services => {
            Directory.CreateDirectory(dataPath);
            services.RemoveAll<DbContextOptions<StoreDbContext>>();
            services.AddDbContext<StoreDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(dataPath, "test.db")}"));
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, "keys")));
        });
}

public class WebTests : IClassFixture<StoreFactory>
{
    private readonly HttpClient client;
    public WebTests(StoreFactory factory) => client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    [Theory]
    [InlineData("/")] [InlineData("/Products")] [InlineData("/Categories")] [InlineData("/Orders")]
    [InlineData("/Orders/Create")] [InlineData("/Products/Create")] [InlineData("/Categories/Create")]
    public async Task RendersUnderIisSubpath(string path)
    {
        var response = await client.GetAsync("/webbanhang" + path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/webbanhang/css/site.css", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task HealthChecksDatabase()
    {
        var response = await client.GetAsync("/webbanhang/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task CssWorksUnderIisSubpath() => Assert.Equal(HttpStatusCode.OK,
        (await client.GetAsync("/webbanhang/css/site.css")).StatusCode);
    [Fact] public async Task RejectsPostWithoutAntiforgeryToken() => Assert.Equal(HttpStatusCode.BadRequest,
        (await client.PostAsync("/webbanhang/Products/Delete/1", new FormUrlEncodedContent([]))).StatusCode);
    [Fact] public async Task MissingOrderReturns404() => Assert.Equal(HttpStatusCode.NotFound,
        (await client.GetAsync("/webbanhang/Orders/Details/999999")).StatusCode);
    [Fact] public async Task LowStockFilterExcludesWellStockedProducts()
    {
        var html = await client.GetStringAsync("/webbanhang/Products?lowStock=true");
        Assert.Contains("ASUS Vivobook", html);
        Assert.DoesNotContain("Logitech MX Master", html);
    }
    [Fact] public async Task CategorySearchFiltersResults()
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/webbanhang/Categories?q=" + Uri.EscapeDataString("Máy")));
        Assert.Contains("<strong>Máy tính</strong>", html);
        Assert.DoesNotContain("<strong>Âm thanh</strong>", html);
    }
}
