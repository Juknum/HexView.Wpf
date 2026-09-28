namespace HexView.Wpf.Tests;

using System;
using System.IO;
using System.Threading;
using Juknum.HexView;
using Juknum.HexView.Enums;
using Xunit;

public class HexViewerCommandTests
{
    private static void RunInSta(Action action)
    {
        Exception ex = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ex = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (ex != null)
        {
            throw new AggregateException("Test failed in STA thread", ex);
        }
    }

    [Fact]
    public void ToggleText_TogglesShowTextAndUpdatesHeader()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            Assert.True(viewer.ShowText);
            Assert.Equal("Hide Text", viewer.ToggleTextHeader);

            HexViewer.ToggleTextCommand.Execute(null, viewer);
            Assert.False(viewer.ShowText);
            Assert.Equal("Show Text", viewer.ToggleTextHeader);

            HexViewer.ToggleTextCommand.Execute(null, viewer);
            Assert.True(viewer.ShowText);
            Assert.Equal("Hide Text", viewer.ToggleTextHeader);
        });
    }

    [Fact]
    public void ToggleEndianness_TogglesEndiannessAndUpdatesHeader()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            Assert.Equal(Endianness.BigEndian, viewer.Endianness);
            Assert.Equal("Little-endian", viewer.ToggleEndiannessHeader);

            HexViewer.ToggleEndiannessCommand.Execute(null, viewer);
            Assert.Equal(Endianness.LittleEndian, viewer.Endianness);
            Assert.Equal("Big-endian", viewer.ToggleEndiannessHeader);
        });
    }

    [Fact]
    public void ToggleSignedness_TogglesSignednessAndUpdatesHeader()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            // In Hexadecimal mode, signedness toggling is not executable
            Assert.False(HexViewer.ToggleSignednessCommand.CanExecute(null, viewer));

            // In Decimal integer mode, signedness toggling is executable
            viewer.DataFormat = DataFormat.Decimal;
            Assert.True(HexViewer.ToggleSignednessCommand.CanExecute(null, viewer));

            Assert.Equal(DataSignedness.Signed, viewer.DataSignedness);
            Assert.Equal("Unsigned", viewer.ToggleSignednessHeader);

            HexViewer.ToggleSignednessCommand.Execute(null, viewer);
            Assert.Equal(DataSignedness.Unsigned, viewer.DataSignedness);
            Assert.Equal("Signed", viewer.ToggleSignednessHeader);
        });
    }

    [Fact]
    public void SetIntegerFormat_UpdatesDataTypeAndWidth()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            HexViewer.SetIntegerFormatCommand.Execute("4", viewer);

            Assert.True(viewer.ShowData);
            Assert.Equal(DataType.Integer, viewer.DataType);
            Assert.Equal(4, viewer.DataWidth);
        });
    }

    [Fact]
    public void SetFloatingPointFormat_UpdatesDataTypeAndWidth()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            HexViewer.SetFloatingPointFormatCommand.Execute("8", viewer);

            Assert.True(viewer.ShowData);
            Assert.Equal(DataType.FloatingPoint, viewer.DataType);
            Assert.Equal(8, viewer.DataWidth);
        });
    }
}
