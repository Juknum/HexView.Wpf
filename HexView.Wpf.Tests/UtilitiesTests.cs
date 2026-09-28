namespace HexView.Wpf.Tests;

using Juknum.HexView.Common;
using Xunit;

public class UtilitiesTests
{
    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(9, 3, 0)]
    [InlineData(-1, 3, 2)]
    [InlineData(-3, 3, 0)]
    [InlineData(-4, 3, 2)]
    [InlineData(0, 5, 0)]
    [InlineData(-10, 16, 6)]
    public void Mod_CalculatesArithmeticModulusCorrectly(int n, int m, int expected)
    {
        int result = n.Mod(m);
        Assert.Equal(expected, result);
    }
}
