namespace Juknum.HexView.Wpf;

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

/// <summary>
/// Input handling, mouse/keyboard navigation, and coordinate conversion for <see cref="HexViewer"/>.
/// </summary>
public partial class HexViewer
{
    private const int ScrollWheelScrollRows = 3;

    private SelectionArea highlightBegin = SelectionArea.None;
    private SelectionArea highlightState = SelectionArea.None;

    private double lastVerticalScrollValue = 0;

    /// <summary>
    /// Selects a range of bytes in the data source.
    /// </summary>
    /// <param name="offset">
    /// The starting byte offset of the selection.
    /// </param>
    /// <param name="length">
    /// The number of bytes to select.
    /// </param>
    public void Select(long offset, long length)
    {
        if (DataSource == null)
        {
            return;
        }

        long streamLength = DataSource.BaseStream.Length;
        SelectionStart = offset.Clamp(0, streamLength);
        SelectionEnd = (offset + length).Clamp(0, streamLength);
    }

    /// <summary>
    /// Selects all bytes in the data source.
    /// </summary>
    public void SelectAll()
    {
        if (DataSource == null)
        {
            return;
        }

        SelectionStart = 0;
        SelectionEnd = DataSource.BaseStream.Length;
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectionStart = 0;
        SelectionEnd = 0;
    }

    /// <summary>
    /// Scrolls the contents of the control to the specified offset.
    /// </summary>
    ///
    /// <param name="offset">
    /// The offset to scroll to.
    /// </param>
    public void ScrollToOffset(long offset)
    {
        long maxBytesDisplayed = BytesPerRow * MaxVisibleRows;

        if (Offset > offset)
        {
            // We need to scroll up
            Offset -= (((Offset - offset - 1) / BytesPerRow) + 1) * BytesPerRow;
        }

        if (Offset + maxBytesDisplayed <= offset)
        {
            // We need to scroll down
            Offset += (((offset - (Offset + maxBytesDisplayed)) / BytesPerRow) + 1) * BytesPerRow;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (Columns > 0 && MaxVisibleRows > 0)
        {
            switch (e.Key)
            {
                case Key.A:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            SelectAll();

                            e.Handled = true;
                        }

                        break;
                    }

                case Key.C:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            e.Handled = true;
                        }

                        break;
                    }

                case Key.Down:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionEnd += BytesPerRow;
                        }
                        else
                        {
                            SelectionStart += BytesPerRow;
                            SelectionEnd = SelectionStart + BytesPerColumn;
                        }

                        ScrollToOffset(SelectionEnd - BytesPerColumn);

                        e.Handled = true;

                        break;
                    }

                case Key.End:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            SelectionEnd = DataSource.BaseStream.Length;

                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                SelectionStart = SelectionEnd - BytesPerColumn;
                            }

                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }
                        else
                        {
                            SelectionEnd += (Offset - SelectionEnd).Mod(BytesPerRow);

                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                SelectionStart = SelectionEnd - BytesPerColumn;
                            }

                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }

                        e.Handled = true;

                        break;
                    }

                case Key.Home:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            SelectionEnd = 0;

                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                SelectionStart = SelectionEnd;
                                SelectionEnd = SelectionStart + BytesPerColumn;
                            }

                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }
                        else
                        {
                            long targetRowStart;
                            if (SelectionEnd < SelectionStart)
                            {
                                targetRowStart = (SelectionEnd / BytesPerRow) * BytesPerRow;
                            }
                            else
                            {
                                targetRowStart = (Math.Max(0, SelectionEnd - 1) / BytesPerRow) * BytesPerRow;
                            }

                            SelectionEnd = Math.Max(0, targetRowStart);

                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                SelectionStart = SelectionEnd;
                                SelectionEnd = SelectionStart + BytesPerColumn;
                            }

                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }

                        e.Handled = true;

                        break;
                    }

                case Key.Left:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionEnd -= BytesPerColumn;
                        }
                        else
                        {
                            SelectionStart -= BytesPerColumn;
                            SelectionEnd = SelectionStart + BytesPerColumn;
                        }

                        ScrollToOffset(SelectionEnd - BytesPerColumn);

                        e.Handled = true;

                        break;
                    }

                case Key.PageDown:
                    {
                        bool isOffsetVisibleBeforeSelectionChange = IsOffsetVisible(SelectionEnd);

                        SelectionEnd += BytesPerRow * MaxVisibleRows;

                        if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionStart = SelectionEnd - BytesPerColumn;
                        }

                        if (isOffsetVisibleBeforeSelectionChange)
                        {
                            ScrollToOffset(Offset + (BytesPerRow * MaxVisibleRows * 2) - BytesPerColumn);
                        }
                        else
                        {
                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }

                        e.Handled = true;
                        break;
                    }

                case Key.PageUp:
                    {
                        bool isOffsetVisibleBeforeSelectionChange = IsOffsetVisible(SelectionEnd);

                        SelectionEnd -= BytesPerRow * MaxVisibleRows;

                        if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionStart = SelectionEnd - BytesPerColumn;
                            SelectionEnd = SelectionStart + BytesPerColumn;
                        }

                        if (isOffsetVisibleBeforeSelectionChange)
                        {
                            ScrollToOffset(Offset - (BytesPerRow * MaxVisibleRows));
                        }
                        else
                        {
                            ScrollToOffset(SelectionEnd - BytesPerColumn);
                        }

                        e.Handled = true;
                        break;
                    }

                case Key.Right:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionEnd += BytesPerColumn;
                        }
                        else
                        {
                            SelectionStart += BytesPerColumn;
                            SelectionEnd = SelectionStart + BytesPerColumn;
                        }

                        ScrollToOffset(SelectionEnd - BytesPerColumn);

                        e.Handled = true;
                        break;
                    }

                case Key.Up:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            SelectionEnd -= BytesPerRow;
                        }
                        else
                        {
                            SelectionStart -= BytesPerRow;
                            SelectionEnd = SelectionStart + BytesPerColumn;
                        }

                        ScrollToOffset(SelectionEnd - BytesPerColumn);

                        e.Handled = true;
                        break;
                    }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    /// <inheritdoc/>
    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            Point position = e.GetPosition(canvas);

            Point addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();

            if (position.X < addressVerticalLinePoint0.X)
            {
                highlightBegin = SelectionArea.Address;
                highlightState = SelectionArea.Address;

                SelectionStart = ConvertPositionToOffset(position);
                SelectionEnd = SelectionStart + BytesPerRow;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (highlightState == SelectionArea.None && CaptureMouse())
        {
            Point position = e.GetPosition(canvas);

            Point addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();
            Point dataVerticalLinePoint0 = CalculateDataVerticalLinePoint0();
            Point textVerticalLinePoint0 = CalculateTextVerticalLinePoint0();

            if (position.X < addressVerticalLinePoint0.X)
            {
                highlightBegin = SelectionArea.Address;
                highlightState = SelectionArea.Address;
            }
            else if (position.X < dataVerticalLinePoint0.X)
            {
                highlightBegin = SelectionArea.Data;
                highlightState = SelectionArea.Data;
            }
            else if (position.X < textVerticalLinePoint0.X)
            {
                highlightBegin = SelectionArea.Text;
                highlightState = SelectionArea.Text;
            }

            if (highlightState != SelectionArea.None)
            {
                SelectionStart = ConvertPositionToOffset(position);

                SelectionEnd = SelectionStart + BytesPerColumn;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        highlightState = SelectionArea.None;

        ReleaseMouseCapture();
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        switch (highlightState)
        {
            case SelectionArea.Data:
            case SelectionArea.Text:
                {
                    Point position = e.GetPosition(canvas);

                    var currentMouseOverOffset = ConvertPositionToOffset(position);

                    if (currentMouseOverOffset >= SelectionStart)
                    {
                        SelectionEnd = currentMouseOverOffset + BytesPerColumn;
                    }
                    else
                    {
                        SelectionEnd = currentMouseOverOffset;
                    }

                    break;
                }
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Delta < 0)
        {
            verticalScrollBar.Value += ScrollWheelScrollRows;

            OnVerticalScrollBarScroll(verticalScrollBar, new ScrollEventArgs(ScrollEventType.SmallIncrement, verticalScrollBar.Value));
        }
        else
        {
            verticalScrollBar.Value -= ScrollWheelScrollRows;

            OnVerticalScrollBarScroll(verticalScrollBar, new ScrollEventArgs(ScrollEventType.SmallDecrement, verticalScrollBar.Value));
        }
    }

    private void OnVerticalScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        lastVerticalScrollValue = e.OldValue;
    }

    private void OnVerticalScrollBarScroll(object sender, ScrollEventArgs e)
    {
        long targetRow = (long)Math.Round(e.NewValue);
        long newOffset = Math.Max(0, targetRow * BytesPerRow);

        if (DataSource != null)
        {
            newOffset = Math.Min(newOffset, DataSource.BaseStream.Length);
        }

        Offset = newOffset;

        InvalidateVisual();
    }

    private long ConvertPositionToOffset(Point position)
    {
        long offset = Offset;

        switch (highlightBegin)
        {
            case SelectionArea.Address:
                {
                    Point addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();
                    Point addressVerticalLinePoint1 = CalculateAddressVerticalLinePoint1();

                    // Clamp the Y coordinate to within the address region
                    position.Y = position.Y.Clamp(addressVerticalLinePoint0.Y, addressVerticalLinePoint1.Y);

                    // Convert the Y coordinate to the row number
                    position.Y /= cachedFormattedChar.Height;

                    if (position.Y >= MaxVisibleRows)
                    {
                        // Due to floating point rounding we may end up with exactly the maximum number of rows, so adjust to compensate
                        --position.Y;
                    }

                    offset += BytesPerRow * (long)position.Y;
                }

                break;

            case SelectionArea.Data:
                {
                    Point addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();

                    Point dataVerticalLinePoint0 = CalculateDataVerticalLinePoint0();
                    Point dataVerticalLinePoint1 = CalculateDataVerticalLinePoint1();

                    // Clamp the X coordinate to within the data region
                    position.X = position.X.Clamp(addressVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width), dataVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width));

                    // Normalize with respect to the data region
                    position.X -= addressVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width);

                    // Convert the X coordinate to the column number
                    position.X /= (CalculateDataColumnCharWidth() + CharsBetweenDataColumns) * cachedFormattedChar.Width;

                    if (position.X >= Columns)
                    {
                        // Due to floating point rounding we may end up with exactly the maximum number of columns, so adjust to compensate
                        --position.X;
                    }

                    // Clamp the Y coordinate to within the data region
                    position.Y = position.Y.Clamp(dataVerticalLinePoint0.Y, dataVerticalLinePoint1.Y);

                    // Convert the Y coordinate to the row number
                    position.Y /= cachedFormattedChar.Height;

                    if (position.Y >= MaxVisibleRows)
                    {
                        // Due to floating point rounding we may end up with exactly the maximum number of rows, so adjust to compensate
                        --position.Y;
                    }

                    offset += (((long)position.Y * Columns) + (long)position.X) * BytesPerColumn;
                }

                break;

            case SelectionArea.Text:
                {
                    Point dataVerticalLinePoint0 = CalculateDataVerticalLinePoint0();

                    Point textVerticalLinePoint0 = CalculateTextVerticalLinePoint0();
                    Point textVerticalLinePoint1 = CalculateTextVerticalLinePoint1();

                    // Clamp the X coordinate to within the text region
                    position.X = position.X.Clamp(dataVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width), textVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width));

                    // Normalize with respect to the text region
                    position.X -= dataVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width);

                    // Convert the X coordinate to the column number
                    position.X /= CalculateTextColumnCharWidth() * cachedFormattedChar.Width;

                    if (position.X >= Columns)
                    {
                        // Due to floating point rounding we may end up with exactly the maximum number of columns, so
                        // adjust to compensate
                        --position.X;
                    }

                    // Clamp the Y coordinate to within the text region
                    position.Y = position.Y.Clamp(textVerticalLinePoint0.Y, textVerticalLinePoint1.Y);

                    // Convert the Y coordinate to the row number
                    position.Y /= cachedFormattedChar.Height;

                    if (position.Y >= MaxVisibleRows)
                    {
                        // Due to floating point rounding we may end up with exactly the maximum number of rows, so adjust to compensate
                        --position.Y;
                    }

                    offset += (((long)position.Y * Columns) + (long)position.X) * BytesPerColumn;
                }

                break;

            default:
                {
                    throw new InvalidOperationException($"Invalid highlight state ${highlightState}");
                }
        }

        return offset;
    }

    private Point ConvertOffsetToPosition(long offset, SelectionArea relativeTo)
    {
        Point position = default;

        switch (relativeTo)
        {
            case SelectionArea.Data:
                {
                    Point addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();

                    position.X = addressVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width);
                    position.Y = addressVerticalLinePoint0.Y;

                    // Normalize requested offset to a zero based column
                    long normalizedColumn = (offset - Offset) / BytesPerColumn;

                    position.X += (((normalizedColumn % Columns) + Columns) % Columns) * (CalculateDataColumnCharWidth() + CharsBetweenDataColumns) * cachedFormattedChar.Width;

                    if (normalizedColumn < 0)
                    {
                        // Negative normalized offset means the Y position is above the current offset. Because division
                        // rounds toward zero we need to compensate here.
                        position.Y += (((normalizedColumn + 1) / Columns) - 1) * cachedFormattedChar.Height;
                    }
                    else
                    {
                        position.Y += normalizedColumn / Columns * cachedFormattedChar.Height;
                    }
                }

                break;

            case SelectionArea.Text:
                {
                    Point dataVerticalLinePoint0 = CalculateDataVerticalLinePoint0();

                    position.X = dataVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width);
                    position.Y = dataVerticalLinePoint0.Y;

                    // Normalize requested offset to a zero based column
                    long normalizedColumn = (offset - Offset) / BytesPerColumn;

                    position.X += (((normalizedColumn % Columns) + Columns) % Columns) * CalculateTextColumnCharWidth() * cachedFormattedChar.Width;

                    if (normalizedColumn < 0)
                    {
                        // Negative normalized offset means the Y position is above the current offset. Because division
                        // rounds toward zero we need to compensate here.
                        position.Y += (((normalizedColumn + 1) / Columns) - 1) * cachedFormattedChar.Height;
                    }
                    else
                    {
                        position.Y += normalizedColumn / Columns * cachedFormattedChar.Height;
                    }
                }

                break;

            default:
                {
                    throw new ArgumentException($"Invalid relative area {relativeTo}", nameof(relativeTo));
                }
        }

        return position;
    }

    private bool IsOffsetVisible(long offset)
    {
        long maxBytesDisplayed = BytesPerRow * MaxVisibleRows;

        return Offset <= offset && Offset + maxBytesDisplayed >= offset;
    }
}
