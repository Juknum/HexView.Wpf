namespace Juknum.HexView.Wpf;

using System;
using System.Buffers.Binary;

/// <summary>
/// A utility class for converting values between endian formats.
/// </summary>
internal static class EndianBitConverter
{
    public static short Convert(short value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);

    public static ushort Convert(ushort value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);

    public static int Convert(int value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);

    public static uint Convert(uint value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);

    public static long Convert(long value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);

    public static ulong Convert(ulong value, Endianness endianness) =>
        BitConverter.IsLittleEndian == (endianness == Endianness.LittleEndian)
            ? value
            : BinaryPrimitives.ReverseEndianness(value);
}
