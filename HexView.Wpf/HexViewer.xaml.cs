namespace Juknum.HexView.Wpf;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Represents a control designed to display a classical hexadecimal viewer.
/// </summary>
public partial class HexViewer : UserControl, INotifyPropertyChanged
{
    private const string CanvasName = "PART_Canvas";
    private const string VerticalScrollBarName = "PART_VerticalScrollBar";

    private Typeface cachedTypeface;
    private FormattedText cachedFormattedChar;
    private FontFamily cachedFontFamily;
    private FontStyle cachedFontStyle;
    private FontWeight cachedFontWeight;
    private FontStretch cachedFontStretch;
    private double cachedFontSize;
    private Brush cachedForeground;

    private Canvas canvas;
    private ScrollBar verticalScrollBar;

    /// <summary>
    /// Initializes a new instance of the <see cref="HexViewer"/> class.
    /// </summary>
    public HexViewer()
    {
        InitializeComponent();

        canvas = PART_Canvas;
        verticalScrollBar = PART_VerticalScrollBar;

        if (canvas != null)
        {
            canvas.SizeChanged += (s, e) => InvalidateVisual();

            CommandBindings.Add(new CommandBinding(
                CopyCommand,
                CopyExecuted,
                CopyCanExecute));
        }

        if (verticalScrollBar != null)
        {
            verticalScrollBar.Scroll += OnVerticalScrollBarScroll;
            verticalScrollBar.ValueChanged += OnVerticalScrollBarValueChanged;

            verticalScrollBar.Minimum = 0;
            verticalScrollBar.SmallChange = 1;
            verticalScrollBar.LargeChange = MaxVisibleRows;
        }

        Loaded += (s, e) =>
        {
            global::Wpf.Ui.Appearance.ApplicationThemeManager.Changed += OnApplicationThemeChanged;
            InvalidateVisual();
        };

        Unloaded += (s, e) =>
        {
            global::Wpf.Ui.Appearance.ApplicationThemeManager.Changed -= OnApplicationThemeChanged;
        };
    }

    /// <summary>
    /// Gets the <see cref="ApplicationCommands.Copy"/> routed command.
    /// </summary>
    public static RoutedUICommand CopyCommand => ApplicationCommands.Copy;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    private enum SelectionArea
    {
        None,
        Address,
        Data,
        Text,
    }

    /// <summary>
    /// Copies the current selection of the control to the <see cref="Clipboard"/>.
    /// </summary>
    public void Copy()
    {
        if (IsSelectionActive)
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

    [RelayCommand]
    private void ToggleText()
    {
        ShowText = !ShowText;
        if (ShowText && TextFormat != TextFormat.Ascii)
        {
            TextFormat = TextFormat.Ascii;
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleSignedness))]
    private void ToggleSignedness()
    {
        DataSignedness = DataSignedness == DataSignedness.Signed ? DataSignedness.Unsigned : DataSignedness.Signed;
    }

    [RelayCommand]
    private void ToggleEndianness()
    {
        Endianness = Endianness == Endianness.BigEndian ? Endianness.LittleEndian : Endianness.BigEndian;
    }

    [RelayCommand]
    private void SetNoData()
    {
        ShowData = false;
    }

    [RelayCommand]
    private void SetIntegerFormat(object parameter)
    {
        if (parameter != null && int.TryParse(parameter.ToString(), out int width))
        {
            ShowData = true;
            DataType = DataType.Integer;
            DataWidth = width;
        }
    }

    [RelayCommand]
    private void SetFloatingPointFormat(object parameter)
    {
        if (parameter != null && int.TryParse(parameter.ToString(), out int width))
        {
            ShowData = true;
            DataType = DataType.FloatingPoint;
            DataWidth = width;
        }
    }

    [RelayCommand]
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
            cachedFormattedChar == null ||
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
            cachedFormattedChar = new FormattedText(
                "X",
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                cachedTypeface,
                FontSize,
                Foreground ?? Brushes.Black,
                1.0);
        }
    }

    private void OnApplicationThemeChanged(global::Wpf.Ui.Appearance.ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        Dispatcher.InvokeAsync(InvalidateVisual);
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

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class CanvasVisualHost : UIElement
    {
        /// <summary>
        /// Gets or sets the Visual.
        /// </summary>
        public Visual Visual { get; set; }

        /// <inheritdoc/>
        protected override int VisualChildrenCount => Visual == null ? 0 : 1;

        /// <inheritdoc/>
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }
    }
}
