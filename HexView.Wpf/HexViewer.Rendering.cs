namespace Juknum.HexView;

using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Juknum.HexView.Common;
using Juknum.HexView.Enums;
using DataFormat = Juknum.HexView.Enums.DataFormat;

/// <summary>
/// Visual rendering, layout calculations, and text formatting for <see cref="HexViewer"/>.
/// </summary>
public partial class HexViewer
{
    private const int MaxColumns = 128;
    private const int MaxRows = 128;

    private const int CharsBetweenSections = 2;
    private const int CharsBetweenDataColumns = 1;

    private double SelectionBoxDataXPadding => cachedFormattedChar.Width / 4;

    private double SelectionBoxDataYPadding => 0;

    private double SelectionBoxTextXPadding => 0;

    private double SelectionBoxTextYPadding => 0;

    private int BytesPerColumn => DataWidth;

    private int BytesPerRow => DataWidth * Columns;

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        UpdateState();

        canvas.Children.Clear();

        if (DataSource != null)
        {
            long savedDataSourcePosition = DataSource.BaseStream.Position;

            // Adjust the data source position based on the current offset
            DataSource.BaseStream.Position = Offset;

            DrawingVisual drawingVisual = new DrawingVisual();

            using (drawingContext = drawingVisual.RenderOpen())
            {
                int rowsToRender = MaxVisibleRows;
                if (DataSource != null && BytesPerRow > 0)
                {
                    long remainingBytes = Math.Max(0, DataSource.BaseStream.Length - Offset);
                    long remainingRows = (remainingBytes + BytesPerRow - 1) / BytesPerRow;
                    rowsToRender = (int)Math.Min(MaxVisibleRows, remainingRows);
                }

                // Add a small padding of 1 pixel to not clip the selection box on the last row
                var clipRect = new Rect(0, 0, canvas.ActualWidth, (rowsToRender * cachedFormattedChar.Height) + 1.0);

                // Clip the drawing to the bounds of the number of rows we can display to prevent the selection
                // box from being drawn where there is no text. This can happen if the control size is changed
                // while the selection remains active.
                drawingContext.PushClip(new RectangleGeometry(clipRect));

                var effectiveAddressBrush = GetEffectiveAddressBrush();
                var effectiveAlternatingBrush = GetEffectiveAlternatingBrush();
                var effectiveSelectionBrush = GetEffectiveSelectionBrush();
                var effectiveSelectionTextBrush = GetEffectiveSelectionTextBrush();

                var pen = new Pen(Foreground, 1.0);

                double halfPenThickness = pen.Thickness / 2;

                // Create guidelines to make sure our coordinate snap to device pixels
                GuidelineSet guidelines = new GuidelineSet();

                drawingContext.PushGuidelineSet(guidelines);

                if (ShowAddress)
                {
                    var addressVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();
                    var addressVerticalLinePoint1 = CalculateAddressVerticalLinePoint1();

                    guidelines.GuidelinesX.Add(addressVerticalLinePoint0.X + halfPenThickness);
                    guidelines.GuidelinesX.Add(addressVerticalLinePoint1.X + halfPenThickness);
                    guidelines.GuidelinesY.Add(addressVerticalLinePoint0.Y + halfPenThickness);
                    guidelines.GuidelinesY.Add(addressVerticalLinePoint1.Y + halfPenThickness);

                    drawingContext.DrawLine(pen, addressVerticalLinePoint0, addressVerticalLinePoint1);
                }

                if (ShowData)
                {
                    var dataVerticalLinePoint0 = CalculateDataVerticalLinePoint0();
                    var dataVerticalLinePoint1 = CalculateDataVerticalLinePoint1();

                    guidelines.GuidelinesX.Add(dataVerticalLinePoint0.X + halfPenThickness);
                    guidelines.GuidelinesX.Add(dataVerticalLinePoint1.X + halfPenThickness);
                    guidelines.GuidelinesY.Add(dataVerticalLinePoint0.Y + halfPenThickness);
                    guidelines.GuidelinesY.Add(dataVerticalLinePoint1.Y + halfPenThickness);

                    drawingContext.DrawLine(pen, dataVerticalLinePoint0, dataVerticalLinePoint1);

                    if (SelectionLength != 0 && MaxVisibleRows > 0 && Columns > 0)
                    {
                        Point selectionPoint0 = ConvertOffsetToPosition(SelectedOffset, SelectionArea.Data);
                        Point selectionPoint1 = ConvertOffsetToPosition(SelectedOffset + SelectionLength, SelectionArea.Data);

                        if (((SelectedOffset + SelectionLength - Offset) / BytesPerColumn) % Columns == 0)
                        {
                            // We're selecting the last column so the end point is the data vertical line (effectively)
                            selectionPoint1.X = dataVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width);
                            selectionPoint1.Y -= cachedFormattedChar.Height;
                        }
                        else
                        {
                            selectionPoint1.X -= CharsBetweenDataColumns * cachedFormattedChar.Width;
                        }

                        DrawSelectionGeometry(drawingContext, effectiveSelectionBrush, pen, selectionPoint0, selectionPoint1, SelectionArea.Data);
                    }
                }

                if (ShowText)
                {
                    var textVerticalLinePoint0 = CalculateTextVerticalLinePoint0();
                    var textVerticalLinePoint1 = CalculateTextVerticalLinePoint1();

                    guidelines.GuidelinesX.Add(textVerticalLinePoint0.X + halfPenThickness);
                    guidelines.GuidelinesX.Add(textVerticalLinePoint1.X + halfPenThickness);
                    guidelines.GuidelinesY.Add(textVerticalLinePoint0.Y + halfPenThickness);
                    guidelines.GuidelinesY.Add(textVerticalLinePoint1.Y + halfPenThickness);

                    drawingContext.DrawLine(pen, textVerticalLinePoint0, textVerticalLinePoint1);

                    if (SelectionLength != 0 && MaxVisibleRows > 0 && Columns > 0)
                    {
                        Point selectionPoint0 = ConvertOffsetToPosition(SelectedOffset, SelectionArea.Text);
                        Point selectionPoint1 = ConvertOffsetToPosition(SelectedOffset + SelectionLength, SelectionArea.Text);

                        if (((SelectedOffset + SelectionLength - Offset) / BytesPerColumn) % Columns == 0)
                        {
                            // We're selecting the last column so the end point is the text vertical line (effectively)
                            selectionPoint1.X = textVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width);
                            selectionPoint1.Y -= cachedFormattedChar.Height;
                        }

                        DrawSelectionGeometry(drawingContext, effectiveSelectionBrush, pen, selectionPoint0, selectionPoint1, SelectionArea.Text);
                    }
                }

                Point origin = default;

                EnsureFontCache();

                for (var row = 0; row < rowsToRender; ++row)
                {
                    if (ShowAddress)
                    {
                        if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                        {
                            var textToFormat = GetFormattedAddressText(Address + (ulong)DataSource.BaseStream.Position);
                            var formattedText = new FormattedText(textToFormat, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, effectiveAddressBrush, 1.0);
                            drawingContext.DrawText(formattedText, origin);

                            origin.X += (CalculateAddressColumnCharWidth() + CharsBetweenSections) * cachedFormattedChar.Width;
                        }
                    }

                    long savedDataSourcePositionBeforeReadingData = DataSource.BaseStream.Position;

                    if (ShowData)
                    {
                        origin.X += CharsBetweenSections * cachedFormattedChar.Width;

                        var cachedDataColumnCharWidth = CalculateDataColumnCharWidth();

                        // Needed to track text in alternating columns so we can use a different brush when drawing
                        var evenColumnBuilder = new StringBuilder(Columns * DataWidth);
                        var oddColumnBuilder = new StringBuilder(Columns * DataWidth);

                        var column = 0;

                        // Draw text up until selection start point
                        while (column < Columns)
                        {
                            if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                            {
                                if (DataSource.BaseStream.Position >= SelectedOffset)
                                {
                                    break;
                                }

                                var textToFormat = ReadFormattedData();

                                if (column % 2 == 0)
                                {
                                    evenColumnBuilder.Append(textToFormat);
                                    evenColumnBuilder.Append(' ', CharsBetweenDataColumns);

                                    oddColumnBuilder.Append(' ', textToFormat.Length + CharsBetweenDataColumns);
                                }
                                else
                                {
                                    oddColumnBuilder.Append(textToFormat);
                                    oddColumnBuilder.Append(' ', CharsBetweenDataColumns);

                                    evenColumnBuilder.Append(' ', textToFormat.Length + CharsBetweenDataColumns);
                                }
                            }
                            else
                            {
                                evenColumnBuilder.Append(' ', cachedDataColumnCharWidth + CharsBetweenDataColumns);
                                oddColumnBuilder.Append(' ', cachedDataColumnCharWidth + CharsBetweenDataColumns);
                            }

                            ++column;
                        }

                        var evenFormattedText = new FormattedText(evenColumnBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, Foreground, 1.0);
                        drawingContext.DrawText(evenFormattedText, origin);

                        var oddFormattedText = new FormattedText(oddColumnBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, effectiveAlternatingBrush, 1.0);
                        drawingContext.DrawText(oddFormattedText, origin);

                        origin.X += evenFormattedText.WidthIncludingTrailingWhitespace;

                        if (column < Columns)
                        {
                            // We'll reuse this builder for drawing selection text
                            evenColumnBuilder.Clear();

                            // Draw text starting from selection start point
                            while (column < Columns)
                            {
                                if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                                {
                                    if (DataSource.BaseStream.Position >= SelectedOffset + SelectionLength)
                                    {
                                        break;
                                    }

                                    var textToFormat = ReadFormattedData();

                                    evenColumnBuilder.Append(textToFormat);
                                    evenColumnBuilder.Append(' ', CharsBetweenDataColumns);
                                }
                                else
                                {
                                    evenColumnBuilder.Append(' ', cachedDataColumnCharWidth + CharsBetweenDataColumns);
                                }

                                ++column;
                            }

                            evenFormattedText = new FormattedText(evenColumnBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, effectiveSelectionTextBrush, 1.0);
                            drawingContext.DrawText(evenFormattedText, origin);

                            origin.X += evenFormattedText.WidthIncludingTrailingWhitespace;

                            if (column < Columns)
                            {
                                evenColumnBuilder.Clear();
                                oddColumnBuilder.Clear();

                                // Draw text after end of selection
                                while (column < Columns)
                                {
                                    if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                                    {
                                        var textToFormat = ReadFormattedData();

                                        if (column % 2 == 0)
                                        {
                                            evenColumnBuilder.Append(textToFormat);
                                            evenColumnBuilder.Append(' ', CharsBetweenDataColumns);

                                            oddColumnBuilder.Append(' ', textToFormat.Length + CharsBetweenDataColumns);
                                        }
                                        else
                                        {
                                            oddColumnBuilder.Append(textToFormat);
                                            oddColumnBuilder.Append(' ', CharsBetweenDataColumns);

                                            evenColumnBuilder.Append(' ', textToFormat.Length + CharsBetweenDataColumns);
                                        }
                                    }
                                    else
                                    {
                                        evenColumnBuilder.Append(' ', cachedDataColumnCharWidth + CharsBetweenDataColumns);
                                        oddColumnBuilder.Append(' ', cachedDataColumnCharWidth + CharsBetweenDataColumns);
                                    }

                                    ++column;
                                }

                                evenFormattedText = new FormattedText(evenColumnBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, Foreground, 1.0);
                                drawingContext.DrawText(evenFormattedText, origin);

                                oddFormattedText = new FormattedText(oddColumnBuilder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, effectiveAlternatingBrush, 1.0);
                                drawingContext.DrawText(oddFormattedText, origin);

                                origin.X += evenFormattedText.WidthIncludingTrailingWhitespace;
                            }
                        }

                        // Compensate for the extra space added at the end of the builder
                        origin.X += (CharsBetweenSections - CharsBetweenDataColumns) * cachedFormattedChar.Width;
                    }

                    if (ShowText)
                    {
                        origin.X += CharsBetweenSections * cachedFormattedChar.Width;

                        if (ShowData)
                        {
                            // Reset the stream to read one byte at a time
                            DataSource.BaseStream.Position = savedDataSourcePositionBeforeReadingData;
                        }

                        var builder = new StringBuilder(Columns * DataWidth);

                        var column = 0;

                        // Draw text up until selection start point
                        while (column < Columns)
                        {
                            if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                            {
                                if (DataSource.BaseStream.Position >= SelectedOffset)
                                {
                                    break;
                                }

                                var textToFormat = ReadFormattedText();
                                builder.Append(textToFormat);
                            }

                            ++column;
                        }

                        var formattedText = new FormattedText(builder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, Foreground, 1.0);
                        drawingContext.DrawText(formattedText, origin);

                        if (column < Columns)
                        {
                            origin.X += formattedText.WidthIncludingTrailingWhitespace;

                            builder.Clear();

                            // Draw text starting from selection start point
                            while (column < Columns)
                            {
                                if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                                {
                                    if (DataSource.BaseStream.Position >= SelectedOffset + SelectionLength)
                                    {
                                        break;
                                    }

                                    var textToFormat = ReadFormattedText();
                                    builder.Append(textToFormat);
                                }

                                ++column;
                            }

                            formattedText = new FormattedText(builder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, effectiveSelectionTextBrush, 1.0);
                            drawingContext.DrawText(formattedText, origin);

                            if (column < Columns)
                            {
                                origin.X += formattedText.WidthIncludingTrailingWhitespace;

                                builder.Clear();

                                // Draw text after end of selection
                                while (column < Columns)
                                {
                                    if (DataSource.BaseStream.Position + BytesPerColumn <= DataSource.BaseStream.Length)
                                    {
                                        var textToFormat = ReadFormattedText();
                                        builder.Append(textToFormat);
                                    }

                                    ++column;
                                }

                                formattedText = new FormattedText(builder.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, cachedTypeface, FontSize, Foreground, 1.0);
                                drawingContext.DrawText(formattedText, origin);
                            }
                        }
                    }

                    origin.X = 0;
                    origin.Y += cachedFormattedChar.Height;
                }

                DataSource.BaseStream.Position = savedDataSourcePosition;

                drawingContext.Pop();
                drawingContext.Pop();
            }

            var visualHost = new CanvasVisualHost
            {
                Visual = drawingVisual,
                IsHitTestVisible = false,
            };

            canvas.Children.Add(visualHost);
        }
    }

    private void DrawSelectionGeometry(DrawingContext drawingContext, Brush brush, Pen pen, Point point0, Point point1, SelectionArea relativeTo)
    {
        if ((long)point0.Y > (long)point1.Y)
        {
            throw new ArgumentException($"{nameof(point0)}.Y > {nameof(point1)}.Y", nameof(point0));
        }

        Point lhsVerticalLinePoint0;
        Point rhsVerticalLinePoint0;

        double selectionBoxXPadding;
        double selectionBoxYPadding;

        switch (relativeTo)
        {
            case SelectionArea.Data:
                {
                    lhsVerticalLinePoint0 = CalculateAddressVerticalLinePoint0();
                    rhsVerticalLinePoint0 = CalculateDataVerticalLinePoint0();

                    selectionBoxXPadding = SelectionBoxDataXPadding;
                    selectionBoxYPadding = SelectionBoxDataYPadding;
                }

                break;

            case SelectionArea.Text:
                {
                    lhsVerticalLinePoint0 = CalculateDataVerticalLinePoint0();
                    rhsVerticalLinePoint0 = CalculateTextVerticalLinePoint0();

                    selectionBoxXPadding = SelectionBoxTextXPadding;
                    selectionBoxYPadding = SelectionBoxTextYPadding;
                }

                break;

            default:
                {
                    throw new ArgumentException($"Invalid relative area {relativeTo}", nameof(relativeTo));
                }
        }

        // Create guidelines to make sure our coordinate snap to device pixels
        GuidelineSet guidelines = new GuidelineSet();

        drawingContext.PushGuidelineSet(guidelines);

        double halfPenThickness = pen.Thickness / 2;

        PathGeometry geometry = new PathGeometry();

        point0.X -= selectionBoxXPadding;
        point1.X += selectionBoxXPadding;
        point0.Y -= selectionBoxYPadding;
        point1.Y += selectionBoxYPadding;

        PathFigure figure = new PathFigure
        {
            StartPoint = point0,
            IsClosed = true,
        };

        if ((long)point0.X < (long)point1.X)
        {
            if ((long)point0.Y < (long)point1.Y)
            {
                Point point2 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point0.Y);
                Point point3 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point1.Y);
                Point point4 = new Point(point1.X, point1.Y + cachedFormattedChar.Height);
                Point point5 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point1.Y + cachedFormattedChar.Height);
                Point point6 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point0.Y + cachedFormattedChar.Height);
                Point point7 = new Point(point0.X, point0.Y + cachedFormattedChar.Height);

                figure.Segments.Add(new LineSegment(point0, true));
                figure.Segments.Add(new LineSegment(point2, true));
                figure.Segments.Add(new LineSegment(point3, true));
                figure.Segments.Add(new LineSegment(point1, true));
                figure.Segments.Add(new LineSegment(point4, true));
                figure.Segments.Add(new LineSegment(point5, true));
                figure.Segments.Add(new LineSegment(point6, true));
                figure.Segments.Add(new LineSegment(point7, true));

                guidelines.GuidelinesX.Add(point6.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point0.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point1.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point2.X + halfPenThickness);
                guidelines.GuidelinesY.Add(point0.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point6.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point1.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point5.Y + halfPenThickness);
            }
            else
            {
                Point point2 = new Point(point1.X, point1.Y + cachedFormattedChar.Height);
                Point point3 = new Point(point0.X, point0.Y + cachedFormattedChar.Height);

                figure.Segments.Add(new LineSegment(point1, true));
                figure.Segments.Add(new LineSegment(point2, true));
                figure.Segments.Add(new LineSegment(point3, true));

                guidelines.GuidelinesX.Add(point0.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point1.X + halfPenThickness);
                guidelines.GuidelinesY.Add(point0.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point3.Y + halfPenThickness);
            }
        }
        else
        {
            if ((long)(point0.Y + cachedFormattedChar.Height) == (long)point1.Y)
            {
                Point point2 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point0.Y);
                Point point3 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point1.Y);
                Point point4 = new Point(point0.X, point1.Y);

                figure.Segments.Add(new LineSegment(point2, true));
                figure.Segments.Add(new LineSegment(point3, true));
                figure.Segments.Add(new LineSegment(point4, true));

                guidelines.GuidelinesX.Add(point0.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point2.X + halfPenThickness);
                guidelines.GuidelinesY.Add(point0.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point4.Y + halfPenThickness);

                PathFigure lhsFigure = new PathFigure
                {
                    StartPoint = point1,
                    IsClosed = true,
                };

                Point point5 = new Point(point1.X, point1.Y + cachedFormattedChar.Height);
                Point point6 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point1.Y + cachedFormattedChar.Height);
                Point point7 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point1.Y);

                lhsFigure.Segments.Add(new LineSegment(point5, true));
                lhsFigure.Segments.Add(new LineSegment(point6, true));
                lhsFigure.Segments.Add(new LineSegment(point7, true));

                guidelines.GuidelinesX.Add(point7.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point1.X + halfPenThickness);
                guidelines.GuidelinesY.Add(point7.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point6.Y + halfPenThickness);

                geometry.Figures.Add(lhsFigure);
            }
            else
            {
                Point point2 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point0.Y);
                Point point3 = new Point(rhsVerticalLinePoint0.X - (CharsBetweenSections * cachedFormattedChar.Width) + selectionBoxXPadding, point1.Y);
                Point point4 = new Point(point1.X, point1.Y + cachedFormattedChar.Height);
                Point point5 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point1.Y + cachedFormattedChar.Height);
                Point point6 = new Point(lhsVerticalLinePoint0.X + (CharsBetweenSections * cachedFormattedChar.Width) - selectionBoxXPadding, point0.Y + cachedFormattedChar.Height);
                Point point7 = new Point(point0.X, point0.Y + cachedFormattedChar.Height);

                figure.Segments.Add(new LineSegment(point0, true));
                figure.Segments.Add(new LineSegment(point2, true));
                figure.Segments.Add(new LineSegment(point3, true));
                figure.Segments.Add(new LineSegment(point1, true));
                figure.Segments.Add(new LineSegment(point4, true));
                figure.Segments.Add(new LineSegment(point5, true));
                figure.Segments.Add(new LineSegment(point6, true));
                figure.Segments.Add(new LineSegment(point7, true));

                guidelines.GuidelinesX.Add(point6.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point1.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point0.X + halfPenThickness);
                guidelines.GuidelinesX.Add(point2.X + halfPenThickness);
                guidelines.GuidelinesY.Add(point0.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point6.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point1.Y + halfPenThickness);
                guidelines.GuidelinesY.Add(point5.Y + halfPenThickness);
            }
        }

        geometry.Figures.Add(figure);

        drawingContext.DrawGeometry(brush, pen, geometry);
        drawingContext.Pop();
    }

    private void UpdateState()
    {
        UpdateMaxVisibleRowsAndColumns();
        UpdateScrollBar();
    }

    private void UpdateMaxVisibleRowsAndColumns()
    {
        int maxVisibleRows = 0;
        int maxVisibleColumns = 0;

        if ((ShowAddress || ShowData || ShowText) && canvas != null)
        {
            EnsureFontCache();

            maxVisibleRows = Math.Max(0, (int)(canvas.ActualHeight / cachedFormattedChar.Height));

            if (ShowData || ShowText)
            {
                int charsPerRow = (int)(canvas.ActualWidth / cachedFormattedChar.Width);

                if (ShowAddress)
                {
                    charsPerRow -= CalculateAddressColumnCharWidth() + (2 * CharsBetweenSections);
                }

                if (ShowData && ShowText)
                {
                    charsPerRow -= 3 * CharsBetweenSections;
                }

                int charsPerColumn = 0;

                if (ShowData)
                {
                    charsPerColumn += CalculateDataColumnCharWidth() + CharsBetweenDataColumns;
                }

                if (ShowText)
                {
                    charsPerColumn += CalculateTextColumnCharWidth();
                }

                if (charsPerColumn != 0)
                {
                    maxVisibleColumns = Math.Max(0, charsPerRow / charsPerColumn);
                }
            }
            else
            {
                maxVisibleColumns = 0;
            }
        }

        MaxVisibleRows = maxVisibleRows;
        MaxVisibleColumns = maxVisibleColumns;

        // Maximum visible rows has now changed and so we must update the maximum amount we should scroll by
        if (verticalScrollBar != null)
        {
            verticalScrollBar.LargeChange = maxVisibleRows;
        }
    }

    private void UpdateScrollBar()
    {
        if (verticalScrollBar != null && (ShowAddress || ShowData || ShowText) && DataSource != null && Columns > 0 && MaxVisibleRows > 0)
        {
            long q = DataSource.BaseStream.Length / BytesPerRow;
            long r = DataSource.BaseStream.Length % BytesPerRow;
            long totalRows = q + (r > 0 ? 1 : 0);

            verticalScrollBar.ViewportSize = MaxVisibleRows;
            verticalScrollBar.Maximum = Math.Max(0, totalRows - MaxVisibleRows);

            // Adjust the scroll value based on the current offset
            verticalScrollBar.Value = Offset / BytesPerRow;

            // Adjust again to compensate for residual bytes if the number of bytes between the start of the stream
            // and the current offset is less than the number of bytes we can display per row
            if (verticalScrollBar.Value == 0 && Offset > 0)
            {
                ++verticalScrollBar.Value;
            }
        }
        else if (verticalScrollBar != null)
        {
            verticalScrollBar.ViewportSize = 0;
            verticalScrollBar.Maximum = 0;
            verticalScrollBar.Value = 0;
        }
    }

    private string ReadFormattedText()
    {
        if (TextFormat != TextFormat.Ascii)
        {
            throw new InvalidOperationException($"Invalid {nameof(TextFormat)} value.");
        }

        return string.Create(DataWidth, DataSource, static (span, reader) =>
        {
            for (int k = 0; k < span.Length; ++k)
            {
                byte value = reader.ReadByte();
                span[k] = value is > 31 and < 127 ? (char)value : '.';
            }
        });
    }

    private string ReadFormattedData() => (DataType, DataFormat, DataSignedness, DataWidth) switch
    {
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 1) => $"{DataSource.ReadSByte():+#;-#;0}".PadLeft(4),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 2) => $"{EndianBitConverter.Convert(DataSource.ReadInt16(), Endianness):+#;-#;0}".PadLeft(6),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 4) => $"{EndianBitConverter.Convert(DataSource.ReadInt32(), Endianness):+#;-#;0}".PadLeft(11),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 8) => $"{EndianBitConverter.Convert(DataSource.ReadInt64(), Endianness):+#;-#;0}".PadLeft(21),

        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 1) => $"{DataSource.ReadByte()}".PadLeft(3),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 2) => $"{EndianBitConverter.Convert(DataSource.ReadUInt16(), Endianness)}".PadLeft(5),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 4) => $"{EndianBitConverter.Convert(DataSource.ReadUInt32(), Endianness)}".PadLeft(10),
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 8) => $"{EndianBitConverter.Convert(DataSource.ReadUInt64(), Endianness)}".PadLeft(20),

        (DataType.Integer, DataFormat.Hexadecimal, _, 1) => $"{DataSource.ReadByte(),0:X2}",
        (DataType.Integer, DataFormat.Hexadecimal, _, 2) => $"{EndianBitConverter.Convert(DataSource.ReadUInt16(), Endianness),0:X4}",
        (DataType.Integer, DataFormat.Hexadecimal, _, 4) => $"{EndianBitConverter.Convert(DataSource.ReadUInt32(), Endianness),0:X8}",
        (DataType.Integer, DataFormat.Hexadecimal, _, 8) => $"{EndianBitConverter.Convert(DataSource.ReadUInt64(), Endianness),0:X16}",

        (DataType.FloatingPoint, _, _, 4) => $"{BitConverter.UInt32BitsToSingle(EndianBitConverter.Convert(DataSource.ReadUInt32(), Endianness)):E08}".PadLeft(16),
        (DataType.FloatingPoint, _, _, 8) => $"{BitConverter.UInt64BitsToDouble(EndianBitConverter.Convert(DataSource.ReadUInt64(), Endianness)):E16}".PadLeft(24),

        _ => throw new InvalidOperationException($"Invalid data configuration: DataType={DataType}, DataFormat={DataFormat}, DataSignedness={DataSignedness}, DataWidth={DataWidth}")
    };

    private string GetFormattedAddressText(ulong address) => AddressFormat switch
    {
        AddressFormat.Address16 => $"{address & 0xFFFF,0:X4}",
        AddressFormat.Address24 => $"{(address >> 16) & 0xFF,0:X2}:{address & 0xFFFF,0:X4}",
        AddressFormat.Address32 => $"{(address >> 16) & 0xFFFF,0:X4}:{address & 0xFFFF,0:X4}",
        AddressFormat.Address48 => $"{(address >> 32) & 0xFF,0:X4}:{address & 0xFFFFFFFF,0:X8}",
        AddressFormat.Address64 => $"{address >> 32,0:X8}:{address & 0xFFFFFFFF,0:X8}",
        _ => throw new InvalidOperationException($"Invalid {nameof(AddressFormat)} value.")
    };

    private int CalculateAddressColumnCharWidth() => AddressFormat switch
    {
        AddressFormat.Address16 => 4,
        AddressFormat.Address24 => 7,
        AddressFormat.Address32 => 9,
        AddressFormat.Address48 => 13,
        AddressFormat.Address64 => 17,
        _ => throw new InvalidOperationException($"Invalid {nameof(AddressFormat)} value.")
    };

    private int CalculateDataColumnCharWidth() => (DataType, DataFormat, DataSignedness, DataWidth) switch
    {
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 1) => 4,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 2) => 6,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 4) => 11,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Signed, 8) => 21,

        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 1) => 3,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 2) => 5,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 4) => 10,
        (DataType.Integer, DataFormat.Decimal, DataSignedness.Unsigned, 8) => 20,

        (DataType.Integer, DataFormat.Hexadecimal, _, 1 or 2 or 4 or 8) => 2 * DataWidth,

        (DataType.FloatingPoint, _, _, 4) => 16,
        (DataType.FloatingPoint, _, _, 8) => 24,

        _ => throw new InvalidOperationException($"Invalid configuration for {nameof(CalculateDataColumnCharWidth)}")
    };

    private Point CalculateAddressVerticalLinePoint0()
    {
        Point point1 = default;

        if (ShowAddress)
        {
            point1.X = (CalculateAddressColumnCharWidth() + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        return point1;
    }

    private Point CalculateAddressVerticalLinePoint1()
    {
        Point point2 = default;

        if (ShowAddress)
        {
            point2.X = (CalculateAddressColumnCharWidth() + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        int visibleRows = MaxVisibleRows;
        if (DataSource != null && BytesPerRow > 0)
        {
            long remainingBytes = Math.Max(0, DataSource.BaseStream.Length - Offset);
            long remainingRows = (remainingBytes + BytesPerRow - 1) / BytesPerRow;
            visibleRows = (int)Math.Min(MaxVisibleRows, remainingRows);
        }

        point2.Y = Math.Min(cachedFormattedChar.Height * visibleRows, canvas.ActualHeight);

        return point2;
    }

    private Point CalculateDataVerticalLinePoint0()
    {
        Point point1 = CalculateAddressVerticalLinePoint0();

        if (ShowData)
        {
            point1.X += (CharsBetweenSections + ((CalculateDataColumnCharWidth() + CharsBetweenDataColumns) * Columns) - CharsBetweenDataColumns + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        return point1;
    }

    private Point CalculateDataVerticalLinePoint1()
    {
        Point point2 = CalculateAddressVerticalLinePoint1();

        if (ShowData)
        {
            point2.X += (CharsBetweenSections + ((CalculateDataColumnCharWidth() + CharsBetweenDataColumns) * Columns) - CharsBetweenDataColumns + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        return point2;
    }

    private int CalculateTextColumnCharWidth()
    {
        return BytesPerColumn;
    }

    private Point CalculateTextVerticalLinePoint0()
    {
        Point point1 = CalculateDataVerticalLinePoint0();

        if (ShowText)
        {
            point1.X += (CharsBetweenSections + (CalculateTextColumnCharWidth() * Columns) + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        return point1;
    }

    private Point CalculateTextVerticalLinePoint1()
    {
        Point point2 = CalculateDataVerticalLinePoint1();

        if (ShowText)
        {
            point2.X += (CharsBetweenSections + (CalculateTextColumnCharWidth() * Columns) + CharsBetweenSections) * cachedFormattedChar.Width;
        }

        return point2;
    }
}
