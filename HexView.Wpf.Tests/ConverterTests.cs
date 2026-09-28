namespace HexView.Wpf.Tests;

using System.Collections;
using System.Globalization;
using System.Windows;
using Juknum.HexView.Common.Converters;
using Juknum.HexView.Enums;
using Xunit;

public class ConverterTests
{
    [Fact]
    public void EqualityConverter_Convert_ReturnsTrueWhenEqual()
    {
        var converter = new EqualityConverter();
        var result = converter.Convert(DataType.Integer, typeof(bool), DataType.Integer, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    [Fact]
    public void EqualityConverter_Convert_ReturnsFalseWhenNotEqual()
    {
        var converter = new EqualityConverter();
        var result = converter.Convert(DataType.Integer, typeof(bool), DataType.FloatingPoint, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    [Fact]
    public void EqualityConverter_Convert_Visibility()
    {
        var converter = new EqualityConverter();
        var visible = converter.Convert(4, typeof(Visibility), 4, CultureInfo.InvariantCulture);
        var collapsed = converter.Convert(4, typeof(Visibility), 8, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, visible);
        Assert.Equal(Visibility.Collapsed, collapsed);
    }

    [Fact]
    public void MultiEqualityConverter_Convert_MatchesSequence()
    {
        var converter = new MultiEqualityConverter();
        var values = new object[] { true, DataType.Integer, 4 };
        var parameters = new ArrayList { true, DataType.Integer, 4 };

        var result = converter.Convert(values, typeof(bool), parameters, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    [Fact]
    public void MultiEqualityConverter_Convert_MismatchesSequence()
    {
        var converter = new MultiEqualityConverter();
        var values = new object[] { true, DataType.Integer, 2 };
        var parameters = new ArrayList { true, DataType.Integer, 4 };

        var result = converter.Convert(values, typeof(bool), parameters, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    [Fact]
    public void MultiEqualityConverter_Convert_Visibility()
    {
        var converter = new MultiEqualityConverter();
        var values = new object[] { true, DataType.FloatingPoint, 8 };
        var parameters = new ArrayList { true, DataType.FloatingPoint, 8 };

        var visible = converter.Convert(values, typeof(Visibility), parameters, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, visible);
    }

    [Fact]
    public void MultiEqualityConverter_Convert_HandlesNullAndLengthMismatchSafely()
    {
        var converter = new MultiEqualityConverter();
        var values = new object[] { true, DataType.Integer };
        var parameters = new ArrayList { true, DataType.Integer, 4 };

        var result = converter.Convert(values, typeof(bool), parameters, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);

        var nullResult = converter.Convert(null, typeof(bool), parameters, CultureInfo.InvariantCulture);
        Assert.Equal(false, nullResult);
    }
}
