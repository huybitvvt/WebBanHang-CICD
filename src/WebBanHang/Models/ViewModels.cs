using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models;

public class CreateOrderInput
{
    [Required(ErrorMessage = "Nhập tên khách hàng."), StringLength(100), Display(Name = "Khách hàng")]
    public string CustomerName { get; set; } = "";
    [StringLength(20), RegularExpression(@"[0-9+()\s-]{8,20}", ErrorMessage = "Số điện thoại không hợp lệ."), Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }
    [Range(typeof(decimal), "0", "100"), Display(Name = "Giảm giá (%)")]
    public decimal DiscountPercent { get; set; }
    [MinLength(1), MaxLength(50)]
    public List<OrderLineInput> Lines { get; set; } = [new()];
}
public class OrderLineInput
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 10000)] public int Quantity { get; set; } = 1;
}
public record DashboardViewModel(int ProductCount, int CategoryCount, int OrderCount,
    decimal Revenue, List<SalesOrder> RecentOrders, List<Product> LowStockProducts);
