using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models;

public class Category
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Nhập tên danh mục."), StringLength(80)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = "";
    [StringLength(300), Display(Name = "Mô tả")]
    public string? Description { get; set; }
    public List<Product> Products { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Nhập tên sản phẩm."), StringLength(120)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục."), Display(Name = "Danh mục")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    [Range(typeof(decimal), "1000", "1000000000", ErrorMessage = "Giá từ 1.000 đến 1.000.000.000 đ."), Display(Name = "Giá bán (VNĐ)")]
    public decimal Price { get; set; }
    [Range(0, 1000000), Display(Name = "Tồn kho")]
    public int Stock { get; set; }
}

public class SalesOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal DiscountPercent { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Mới";
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
