namespace HexView.Wpf.Tests;

using System;
using System.IO;
using System.Threading;
using Juknum.HexView;
using Juknum.HexView.Enums;
using Xunit;

public class SelectionTests
{
    private static void RunInSta(System.Action action)
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
    public void Selection_Forward_CalculatesLengthAndOffsetCorrectly()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            var bytes = new byte[100];
            viewer.DataSource = new BinaryReader(new MemoryStream(bytes));

            viewer.Select(10, 20);

            Assert.Equal(10, viewer.SelectionStart);
            Assert.Equal(30, viewer.SelectionEnd);
            Assert.Equal(10, viewer.SelectedOffset);
            Assert.Equal(20, viewer.SelectionLength);
            Assert.True(viewer.IsSelectionActive);
        });
    }

    [Fact]
    public void Selection_Backward_CalculatesLengthAndOffsetSymmetrically()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            var bytes = new byte[100];
            viewer.DataSource = new BinaryReader(new MemoryStream(bytes));

            viewer.Select(30, -20);

            Assert.Equal(30, viewer.SelectionStart);
            Assert.Equal(10, viewer.SelectionEnd);
            Assert.Equal(10, viewer.SelectedOffset);
            Assert.Equal(20, viewer.SelectionLength);
            Assert.True(viewer.IsSelectionActive);
        });
    }

    [Fact]
    public void Selection_Clear_ResetsSelection()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            var bytes = new byte[100];
            viewer.DataSource = new BinaryReader(new MemoryStream(bytes));

            viewer.Select(10, 20);
            viewer.ClearSelection();

            Assert.Equal(0, viewer.SelectionStart);
            Assert.Equal(0, viewer.SelectionEnd);
            Assert.Equal(0, viewer.SelectedOffset);
            Assert.Equal(0, viewer.SelectionLength);
            Assert.False(viewer.IsSelectionActive);
        });
    }

    [Fact]
    public void Selection_SelectAll_SelectsEntireStream()
    {
        RunInSta(() =>
        {
            var viewer = new HexViewer();
            var bytes = new byte[256];
            viewer.DataSource = new BinaryReader(new MemoryStream(bytes));

            viewer.SelectAll();

            Assert.Equal(0, viewer.SelectionStart);
            Assert.Equal(256, viewer.SelectionEnd);
            Assert.Equal(0, viewer.SelectedOffset);
            Assert.Equal(256, viewer.SelectionLength);
        });
    }
}
