namespace WebBanHang.Services;

public static class OrderCalculator
{
    public static decimal CalculateTotal(IEnumerable<(decimal UnitPrice, int Quantity)> lines, decimal discountPercent)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (discountPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(discountPercent));
        decimal subtotal = 0;
        foreach (var (price, quantity) in lines)
        {
            if (price < 0 || quantity <= 0) throw new ArgumentOutOfRangeException(nameof(lines));
            subtotal = checked(subtotal + price * quantity);
        }
        return decimal.Round(subtotal * (1 - discountPercent / 100m), 0, MidpointRounding.AwayFromZero);
    }
}

THIS_IS_AN_INTENTIONAL_CSHARP_SYNTAX_ERROR;
