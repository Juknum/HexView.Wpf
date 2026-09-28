namespace Juknum.HexView;

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Juknum.HexView.Enums;
using DataFormat = Juknum.HexView.Enums.DataFormat;

/// <summary>
/// Represents a control designed to display a classical hexadecimal viewer.
/// </summary>
[TemplatePart(Name = CanvasName, Type = typeof(HexRenderSurface))]
[TemplatePart(Name = VerticalScrollBarName, Type = typeof(ScrollBar))]
public partial class HexViewer : Control
{
    private const string CanvasName = "PART_Canvas";
    private const string VerticalScrollBarName = "PART_VerticalScrollBar";

    private Typeface cachedTypeface;
    private GlyphTypeface cachedGlyphTypeface;
    private FormattedText cachedFormattedChar;
    private FontFamily cachedFontFamily;
    private FontStyle cachedFontStyle;
    private FontWeight cachedFontWeight;
    private FontStretch cachedFontStretch;
    private double cachedFontSize;
    private Brush cachedForeground;
    private double cachedCharWidth;
    private double cachedCharHeight;
    private double cachedBaseline;

    private HexRenderSurface canvas;
    private ScrollBar verticalScrollBar;

    static HexViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(HexViewer),
            new FrameworkPropertyMetadata(typeof(HexViewer)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HexViewer"/> class.
    /// </summary>
    public HexViewer()
    {
        CommandBindings.Add(new CommandBinding(CopyCommand, CopyExecuted, CopyCanExecute));
        CommandBindings.Add(new CommandBinding(ToggleTextCommand, ToggleTextExecuted));
        CommandBindings.Add(new CommandBinding(ToggleSignednessCommand, ToggleSignednessExecuted, ToggleSignednessCanExecute));
        CommandBindings.Add(new CommandBinding(ToggleEndiannessCommand, ToggleEndiannessExecuted));
        CommandBindings.Add(new CommandBinding(SetNoDataCommand, SetNoDataExecuted));
        CommandBindings.Add(new CommandBinding(SetIntegerFormatCommand, SetIntegerFormatExecuted));
        CommandBindings.Add(new CommandBinding(SetFloatingPointFormatCommand, SetFloatingPointFormatExecuted));
        CommandBindings.Add(new CommandBinding(SetDataFormatCommand, SetDataFormatExecuted));

        UpdateHeaders();
    }

    /// <summary>
    /// Gets the <see cref="ApplicationCommands.Copy"/> routed command.
    /// </summary>
    public static RoutedUICommand CopyCommand => ApplicationCommands.Copy;

    /// <summary>
    /// Gets the toggle text routed command.
    /// </summary>
    public static RoutedUICommand ToggleTextCommand { get; } = new(nameof(ToggleText), nameof(ToggleTextCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the toggle signedness routed command.
    /// </summary>
    public static RoutedUICommand ToggleSignednessCommand { get; } = new(nameof(ToggleSignedness), nameof(ToggleSignednessCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the toggle endianness routed command.
    /// </summary>
    public static RoutedUICommand ToggleEndiannessCommand { get; } = new(nameof(ToggleEndianness), nameof(ToggleEndiannessCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the set no data routed command.
    /// </summary>
    public static RoutedUICommand SetNoDataCommand { get; } = new(nameof(SetNoData), nameof(SetNoDataCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the set integer format routed command.
    /// </summary>
    public static RoutedUICommand SetIntegerFormatCommand { get; } = new(nameof(SetIntegerFormat), nameof(SetIntegerFormatCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the set floating point format routed command.
    /// </summary>
    public static RoutedUICommand SetFloatingPointFormatCommand { get; } = new(nameof(SetFloatingPointFormat), nameof(SetFloatingPointFormatCommand), typeof(HexViewer));

    /// <summary>
    /// Gets the set data format routed command.
    /// </summary>
    public static RoutedUICommand SetDataFormatCommand { get; } = new(nameof(SetDataFormat), nameof(SetDataFormatCommand), typeof(HexViewer));

    private enum SelectionArea
    {
        None,
        Address,
        Data,
        Text,
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (canvas != null)
        {
            canvas.Owner = null;
            canvas.SizeChanged -= OnCanvasSizeChanged;
        }

        if (verticalScrollBar != null)
        {
            verticalScrollBar.Scroll -= OnVerticalScrollBarScroll;
            verticalScrollBar.ValueChanged -= OnVerticalScrollBarValueChanged;
        }

        canvas = GetTemplateChild(CanvasName) as HexRenderSurface;
        verticalScrollBar = GetTemplateChild(VerticalScrollBarName) as ScrollBar;

        if (canvas != null)
        {
            canvas.Owner = this;
            canvas.SizeChanged += OnCanvasSizeChanged;
        }

        if (verticalScrollBar != null)
        {
            verticalScrollBar.Scroll += OnVerticalScrollBarScroll;
            verticalScrollBar.ValueChanged += OnVerticalScrollBarValueChanged;

            verticalScrollBar.Minimum = 0;
            verticalScrollBar.SmallChange = 1;
            verticalScrollBar.LargeChange = MaxVisibleRows;
        }

        UpdateState();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InvalidateVisual();
    }

    /// <summary>
    /// Re-renders the control content.
    /// </summary>
    public new void InvalidateVisual()
    {
        base.InvalidateVisual();
        canvas?.InvalidateVisual();
    }

    /// <summary>
    /// Copies the current selection of the control to the <see cref="Clipboard"/>.
    /// </summary>
    public void Copy()
    {
        if (IsSelectionActive && DataSource != null)
        {
            StringBuilder builder = new StringBuilder();

            long savedDataSourcePositionBeforeReadingData = DataSource.BaseStream.Position;

            DataSource.BaseStream.Position = Math.Min(SelectionStart, SelectionEnd);

            while (DataSource.BaseStream.Position < Math.Max(SelectionStart, SelectionEnd))
            {
                var formattedData = ReadFormattedData();

                builder.Append(formattedData);
            }

            DataSource.BaseStream.Position = savedDataSourcePositionBeforeReadingData;

            Clipboard.SetText(builder.ToString());
        }
    }

    private void ToggleText()
    {
        ShowText = !ShowText;
        if (ShowText && TextFormat != TextFormat.Ascii)
        {
            TextFormat = TextFormat.Ascii;
        }
    }

    private void ToggleSignedness()
    {
        DataSignedness = DataSignedness == DataSignedness.Signed ? DataSignedness.Unsigned : DataSignedness.Signed;
    }

    private void ToggleEndianness()
    {
        Endianness = Endianness == Endianness.BigEndian ? Endianness.LittleEndian : Endianness.BigEndian;
    }

    private void SetNoData()
    {
        ShowData = false;
    }

    private void SetIntegerFormat(object parameter)
    {
        if (parameter != null && int.TryParse(parameter.ToString(), out int width))
        {
            ShowData = true;
            DataType = DataType.Integer;
            DataWidth = width;
        }
    }

    private void SetFloatingPointFormat(object parameter)
    {
        if (parameter != null && int.TryParse(parameter.ToString(), out int width))
        {
            ShowData = true;
            DataType = DataType.FloatingPoint;
            DataWidth = width;
        }
    }

    private void SetDataFormat(object parameter)
    {
        if (parameter is DataFormat format)
        {
            DataFormat = format;
        }
        else if (parameter != null && Enum.TryParse<DataFormat>(parameter.ToString(), out var parsedFormat))
        {
            DataFormat = parsedFormat;
        }
    }

    private void EnsureFontCache()
    {
        if (cachedTypeface == null ||
            !Equals(cachedFontFamily, FontFamily) ||
            cachedFontStyle != FontStyle ||
            cachedFontWeight != FontWeight ||
            cachedFontStretch != FontStretch ||
            cachedFontSize != FontSize ||
            !Equals(cachedForeground, Foreground))
        {
            cachedFontFamily = FontFamily;
            cachedFontStyle = FontStyle;
            cachedFontWeight = FontWeight;
            cachedFontStretch = FontStretch;
            cachedFontSize = FontSize;
            cachedForeground = Foreground;

            cachedTypeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            if (cachedTypeface.TryGetGlyphTypeface(out cachedGlyphTypeface))
            {
                if (cachedGlyphTypeface.CharacterToGlyphMap.TryGetValue('X', out ushort glyphIndex))
                {
                    cachedCharWidth = cachedGlyphTypeface.AdvanceWidths[glyphIndex] * FontSize;
                }
                else
                {
                    cachedCharWidth = FontSize * 0.6;
                }

                cachedCharHeight = cachedGlyphTypeface.Height * FontSize;
                cachedBaseline = cachedGlyphTypeface.Baseline * FontSize;
            }
            else
            {
                cachedFormattedChar = new FormattedText(
                    "X",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    cachedTypeface,
                    FontSize,
                    Foreground ?? Brushes.Black,
                    1.0);
                cachedCharWidth = cachedFormattedChar.Width;
                cachedCharHeight = cachedFormattedChar.Height;
                cachedBaseline = cachedFormattedChar.Baseline;
            }
        }
    }

    private Brush GetEffectiveAddressBrush() => AddressBrush ?? (TryFindResource("AccentTextFillColorPrimaryBrush") as Brush) ?? (TryFindResource("TextFillColorSecondaryBrush") as Brush) ?? Foreground;

    private Brush GetEffectiveAlternatingBrush() => AlternatingDataColumnTextBrush ?? (TryFindResource("TextFillColorSecondaryBrush") as Brush) ?? Foreground;

    private Brush GetEffectiveSelectionBrush() => SelectionBrush ?? (TryFindResource("AccentFillColorDefaultBrush") as Brush) ?? SystemColors.HighlightBrush;

    private Brush GetEffectiveSelectionTextBrush() => SelectionTextBrush ?? (TryFindResource("TextOnAccentFillColorPrimaryBrush") as Brush) ?? SystemColors.HighlightTextBrush;

    private void CopyExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        Copy();
    }

    private void CopyCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsSelectionActive;
    }

    private void ToggleTextExecuted(object sender, ExecutedRoutedEventArgs e) => ToggleText();

    private void ToggleSignednessExecuted(object sender, ExecutedRoutedEventArgs e) => ToggleSignedness();

    private void ToggleSignednessCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = CanToggleSignedness;

    private void ToggleEndiannessExecuted(object sender, ExecutedRoutedEventArgs e) => ToggleEndianness();

    private void SetNoDataExecuted(object sender, ExecutedRoutedEventArgs e) => SetNoData();

    private void SetIntegerFormatExecuted(object sender, ExecutedRoutedEventArgs e) => SetIntegerFormat(e.Parameter);

    private void SetFloatingPointFormatExecuted(object sender, ExecutedRoutedEventArgs e) => SetFloatingPointFormat(e.Parameter);

    private void SetDataFormatExecuted(object sender, ExecutedRoutedEventArgs e) => SetDataFormat(e.Parameter);
}

/// <summary>
/// Internal render surface for the <see cref="HexViewer"/> control that executes direct OnRender painting.
/// </summary>
public sealed class HexRenderSurface : FrameworkElement
{
    internal HexViewer Owner { get; set; }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        Owner?.RenderContent(drawingContext);
    }
}
