namespace HexView.Wpf.Tests;

using System;
using Juknum.HexView.Common;
using Juknum.HexView.Enums;
using Xunit;

public class EndianBitConverterTests
{
    [Fact]
    public void Convert_Int16_LittleEndian()
    {
        short val = 0x1234;
        short converted = EndianBitConverter.Convert(val, Endianness.LittleEndian);
        short expected = BitConverter.IsLittleEndian ? (short)0x1234 : (short)0x3412;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_Int16_BigEndian()
    {
        short val = 0x1234;
        short converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        short expected = BitConverter.IsLittleEndian ? (short)0x3412 : (short)0x1234;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_UInt16_BigEndian()
    {
        ushort val = 0xABCD;
        ushort converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        ushort expected = BitConverter.IsLittleEndian ? (ushort)0xCDAB : (ushort)0xABCD;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_Int32_BigEndian()
    {
        int val = 0x12345678;
        int converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        int expected = BitConverter.IsLittleEndian ? 0x78563412 : 0x12345678;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_UInt32_BigEndian()
    {
        uint val = 0x12345678;
        uint converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        uint expected = BitConverter.IsLittleEndian ? 0x78563412u : 0x12345678u;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_Int64_BigEndian()
    {
        long val = 0x0123456789ABCDEF;
        long converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        long expected = BitConverter.IsLittleEndian ? unchecked((long)0xEFCDAB8967452301UL) : val;
        Assert.Equal(expected, converted);
    }

    [Fact]
    public void Convert_UInt64_BigEndian()
    {
        ulong val = 0x0123456789ABCDEF;
        ulong converted = EndianBitConverter.Convert(val, Endianness.BigEndian);
        ulong expected = BitConverter.IsLittleEndian ? 0xEFCDAB8967452301 : val;
        Assert.Equal(expected, converted);
    }
}
