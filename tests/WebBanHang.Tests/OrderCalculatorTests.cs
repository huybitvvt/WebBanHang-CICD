using WebBanHang.Services;
using Xunit;

namespace WebBanHang.Tests;

public class OrderCalculatorTests
{
    [Fact] public void SumsAllLines() => Assert.Equal(250000m,
        OrderCalculator.CalculateTotal([(100000m, 2), (50000m, 1)], 0));
    [Fact] public void AppliesTenPercentDiscount() => Assert.Equal(180000m,
        OrderCalculator.CalculateTotal([(100000m, 2)], 10));
    [Fact] public void FullDiscountIsZero() => Assert.Equal(0,
        OrderCalculator.CalculateTotal([(150000m, 3)], 100));
    [Fact] public void RoundsDongAwayFromZero() => Assert.Equal(91m,
        OrderCalculator.CalculateTotal([(101m, 1)], 10));
    [Fact] public void EmptyOrderTotalIsZero() => Assert.Equal(0,
        OrderCalculator.CalculateTotal([], 0));
    [Theory] [InlineData(-1)] [InlineData(101)]
    public void RejectsInvalidDiscount(int discount) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        OrderCalculator.CalculateTotal([(100000m, 1)], discount));
    [Theory] [InlineData(-100, 1)] [InlineData(100, 0)] [InlineData(100, -1)]
    public void RejectsInvalidLines(int price, int quantity) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        OrderCalculator.CalculateTotal([(price, quantity)], 0));
}
