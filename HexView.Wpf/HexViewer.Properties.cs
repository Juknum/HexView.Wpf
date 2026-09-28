namespace Juknum.HexView.Wpf;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

/// <summary>
/// Dependency properties and CLR property wrappers for <see cref="HexViewer"/>.
/// </summary>
public partial class HexViewer
{
    /// <summary>
    /// Defines the address at which the data in the <see cref="DataSourceProperty"/> begins.
    /// </summary>
    public static readonly DependencyProperty AddressProperty =
        DependencyProperty.Register(nameof(Address), typeof(ulong), typeof(HexViewer),
            new FrameworkPropertyMetadata(0UL, OnAddressChanged));

    /// <summary>
    /// Defines the brush used to display the addresses in the address section of the control.
    /// </summary>
    public static readonly DependencyProperty AddressBrushProperty =
        DependencyProperty.Register(nameof(AddressBrush), typeof(Brush), typeof(HexViewer),
            new FrameworkPropertyMetadata(null, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the width of the addresses displayed in the address section of the control.
    /// </summary>
    public static readonly DependencyProperty AddressFormatProperty =
        DependencyProperty.Register(nameof(AddressFormat), typeof(AddressFormat), typeof(HexViewer),
            new FrameworkPropertyMetadata(AddressFormat.Address32, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the brush used for alternating for text in alternating (odd numbered) columns in the data section of the control.
    /// </summary>
    public static readonly DependencyProperty AlternatingDataColumnTextBrushProperty =
        DependencyProperty.Register(nameof(AlternatingDataColumnTextBrush), typeof(Brush), typeof(HexViewer),
            new FrameworkPropertyMetadata(null, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the number of columns to display.
    /// </summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(int), typeof(HexViewer),
            new FrameworkPropertyMetadata(16, OnPropertyChangedInvalidateVisual, CoerceColumns));

    /// <summary>
    /// Defines the endianness used to interpret the data.
    /// </summary>
    public static readonly DependencyProperty EndiannessProperty =
        DependencyProperty.Register(nameof(Endianness), typeof(Endianness), typeof(HexViewer),
            new FrameworkPropertyMetadata(Endianness.BigEndian, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the format of the data to display.
    /// </summary>
    public static readonly DependencyProperty DataFormatProperty =
        DependencyProperty.Register(nameof(DataFormat), typeof(DataFormat), typeof(HexViewer),
            new FrameworkPropertyMetadata(DataFormat.Hexadecimal, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the signedness of the data to display.
    /// </summary>
    public static readonly DependencyProperty DataSignednessProperty =
        DependencyProperty.Register(nameof(DataSignedness), typeof(DataSignedness), typeof(HexViewer),
            new FrameworkPropertyMetadata(DataSignedness.Signed, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the data source which is used to read the data to display within this control.
    /// </summary>
    public static readonly DependencyProperty DataSourceProperty =
        DependencyProperty.Register(nameof(DataSource), typeof(BinaryReader), typeof(HexViewer),
            new FrameworkPropertyMetadata(OnDataSourceChanged));

    /// <summary>
    /// Defines the type of the data to display.
    /// </summary>
    public static readonly DependencyProperty DataTypeProperty =
        DependencyProperty.Register(nameof(DataType), typeof(DataType), typeof(HexViewer),
            new FrameworkPropertyMetadata(DataType.Integer, OnDataTypeChanged));

    /// <summary>
    /// Defines the width of the data to display.
    /// </summary>
    public static readonly DependencyProperty DataWidthProperty =
        DependencyProperty.Register(nameof(DataWidth), typeof(int), typeof(HexViewer),
            new FrameworkPropertyMetadata(1, OnDataWidthChanged, CoerceDataWidth), ValidateDataWidth);

    /// <summary>
    /// Defines the offset from the <see cref="DataSourceProperty"/> of the first visible data element being displayed.
    /// </summary>
    public static readonly DependencyProperty OffsetProperty =
        DependencyProperty.Register(nameof(Offset), typeof(long), typeof(HexViewer),
            new FrameworkPropertyMetadata(0L, OnPropertyChangedInvalidateVisual, CoerceOffset));

    /// <summary>
    /// Defines the maximum number of columns, based on the size of the control, which can be displayed.
    /// </summary>
    public static readonly DependencyPropertyKey MaxVisibleColumnsPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(MaxVisibleColumns), typeof(int), typeof(HexViewer),
            new FrameworkPropertyMetadata(OnPropertyChangedInvalidateVisual, CoerceMaxVisibleColumns));

    /// <summary>
    /// Defines the maximum number of columns, based on the size of the control, which can be displayed.
    /// </summary>
    public static readonly DependencyProperty MaxVisibleColumnsProperty = MaxVisibleColumnsPropertyKey.DependencyProperty;

    /// <summary>
    /// Defines the maximum number of rows, based on the size of the control, which can be displayed.
    /// </summary>
    public static readonly DependencyPropertyKey MaxVisibleRowsPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(MaxVisibleRows), typeof(int), typeof(HexViewer),
            new FrameworkPropertyMetadata(OnPropertyChangedInvalidateVisual, CoerceMaxVisibleRows));

    /// <summary>
    /// Defines the maximum number of rows, based on the size of the control, which can be displayed.
    /// </summary>
    public static readonly DependencyProperty MaxVisibleRowsProperty = MaxVisibleRowsPropertyKey.DependencyProperty;

    /// <summary>
    /// Defines the brush used for selection fill.
    /// </summary>
    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush), typeof(HexViewer),
            new FrameworkPropertyMetadata(null, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the brush used for selected text.
    /// </summary>
    public static readonly DependencyProperty SelectionTextBrushProperty =
        DependencyProperty.Register(nameof(SelectionTextBrush), typeof(Brush), typeof(HexViewer),
            new FrameworkPropertyMetadata(null, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the offset from <see cref="DataSourceProperty"/> of where the user selection has ended.
    /// </summary>
    public static readonly DependencyPropertyKey SelectionEndPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(SelectionEnd), typeof(long), typeof(HexViewer),
            new FrameworkPropertyMetadata(OnSelectionEndChanged, CoerceSelectionEnd));

    /// <summary>
    /// Defines the offset from <see cref="DataSourceProperty"/> of where the user selection has ended.
    /// </summary>
    public static readonly DependencyProperty SelectionEndProperty = SelectionEndPropertyKey.DependencyProperty;

    /// <summary>
    /// Defines the offset from <see cref="DataSourceProperty"/> of where the user selection has started.
    /// </summary>
    public static readonly DependencyPropertyKey SelectionStartPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(SelectionStart), typeof(long), typeof(HexViewer),
            new FrameworkPropertyMetadata(OnSelectionStartChanged, CoerceSelectionStart));

    /// <summary>
    /// Defines the offset from <see cref="DataSourceProperty"/> of where the user selection has started.
    /// </summary>
    public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;

    /// <summary>
    /// Determines whether to show the address section of the control.
    /// </summary>
    public static readonly DependencyProperty ShowAddressProperty =
        DependencyProperty.Register(nameof(ShowAddress), typeof(bool), typeof(HexViewer),
            new FrameworkPropertyMetadata(true, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Determines whether to show the data section of the control.
    /// </summary>
    public static readonly DependencyProperty ShowDataProperty =
        DependencyProperty.Register(nameof(ShowData), typeof(bool), typeof(HexViewer),
            new FrameworkPropertyMetadata(true, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Determines whether to show the text section of the control.
    /// </summary>
    public static readonly DependencyProperty ShowTextProperty =
        DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(HexViewer),
            new FrameworkPropertyMetadata(true, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Defines the format of the text to display in the text section.
    /// </summary>
    public static readonly DependencyProperty TextFormatProperty =
        DependencyProperty.Register(nameof(TextFormat), typeof(TextFormat), typeof(HexViewer),
            new FrameworkPropertyMetadata(TextFormat.Ascii, OnPropertyChangedInvalidateVisual));

    /// <summary>
    /// Gets or sets the address at which the data in the <see cref="DataSource"/> begins.
    /// </summary>
    public ulong Address
    {
        get => (ulong)GetValue(AddressProperty);
        set => SetValue(AddressProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to display the addresses in the address section of the control.
    /// </summary>
    public Brush AddressBrush
    {
        get => (Brush)GetValue(AddressBrushProperty);
        set => SetValue(AddressBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used for alternating for text in alternating (odd numbered) columns in the data section of the control.
    /// </summary>
    public Brush AlternatingDataColumnTextBrush
    {
        get => (Brush)GetValue(AlternatingDataColumnTextBrushProperty);
        set => SetValue(AlternatingDataColumnTextBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of columns to display.
    /// </summary>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the endianness used to interpret the data.
    /// </summary>
    public Endianness Endianness
    {
        get => (Endianness)GetValue(EndiannessProperty);
        set => SetValue(EndiannessProperty, value);
    }

    /// <summary>
    /// Gets or sets the format of the data to display.
    /// </summary>
    public DataFormat DataFormat
    {
        get => (DataFormat)GetValue(DataFormatProperty);
        set => SetValue(DataFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the signedness of the data to display.
    /// </summary>
    public DataSignedness DataSignedness
    {
        get => (DataSignedness)GetValue(DataSignednessProperty);
        set => SetValue(DataSignednessProperty, value);
    }

    /// <summary>
    /// Gets or sets the data source which is used to read the data to display within this control.
    /// </summary>
    public BinaryReader DataSource
    {
        get => (BinaryReader)GetValue(DataSourceProperty);
        set => SetValue(DataSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the type of the data to display.
    /// </summary>
    public DataType DataType
    {
        get => (DataType)GetValue(DataTypeProperty);
        set => SetValue(DataTypeProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the data to display.
    /// </summary>
    public int DataWidth
    {
        get => (int)GetValue(DataWidthProperty);
        set => SetValue(DataWidthProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the user has made any selection within the control.
    /// </summary>
    public bool IsSelectionActive => SelectionLength != 0;

    /// <summary>
    /// Gets the maximum number of columns, based on the size of the control, which can be displayed.
    /// </summary>
    public int MaxVisibleColumns
    {
        get => (int)GetValue(MaxVisibleColumnsProperty);
        private set => SetValue(MaxVisibleColumnsPropertyKey, value);
    }

    /// <summary>
    /// Gets the maximum number of rows, based on the size of the control, which can be displayed.
    /// </summary>
    public int MaxVisibleRows
    {
        get => (int)GetValue(MaxVisibleRowsProperty);
        private set => SetValue(MaxVisibleRowsPropertyKey, value);
    }

    /// <summary>
    /// Gets or sets the offset from the <see cref="DataSource"/> of the first visible data element being displayed.
    /// </summary>
    public long Offset
    {
        get => (long)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    /// <summary>
    /// Gets lowest order address currently being selected.
    /// </summary>
    public ulong SelectedAddress => Address + (ulong)SelectedOffset;

    /// <summary>
    /// Gets the offset from <see cref="DataSource"/> of the <see cref="SelectedAddress"/>.
    /// </summary>
    public long SelectedOffset => Math.Min(SelectionStart, SelectionEnd);

    /// <summary>
    /// Gets or sets the brush used for selection fill.
    /// </summary>
    public Brush SelectionBrush
    {
        get => (Brush)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <summary>
    /// Gets the offset from <see cref="DataSource"/> of where the user selection has ended.
    /// </summary>
    public long SelectionEnd
    {
        get => (long)GetValue(SelectionEndProperty);
        private set => SetValue(SelectionEndPropertyKey, value);
    }

    /// <summary>
    /// Gets the number of bytes selected.
    /// </summary>
    public long SelectionLength
    {
        get
        {
            if (SelectionStart <= SelectionEnd)
            {
                return SelectionEnd - SelectionStart;
            }
            else
            {
                return SelectionStart - SelectionEnd + BytesPerColumn;
            }
        }
    }

    /// <summary>
    /// Gets the offset from <see cref="DataSource"/> of where the user selection has started.
    /// </summary>
    public long SelectionStart
    {
        get => (long)GetValue(SelectionStartProperty);
        private set => SetValue(SelectionStartPropertyKey, value);
    }

    /// <summary>
    /// Gets or sets the brush used for selected text.
    /// </summary>
    public Brush SelectionTextBrush
    {
        get => (Brush)GetValue(SelectionTextBrushProperty);
        set => SetValue(SelectionTextBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the address section of the control.
    /// </summary>
    public bool ShowAddress
    {
        get => (bool)GetValue(ShowAddressProperty);
        set => SetValue(ShowAddressProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the data section of the control.
    /// </summary>
    public bool ShowData
    {
        get => (bool)GetValue(ShowDataProperty);
        set => SetValue(ShowDataProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the text section of the control.
    /// </summary>
    public bool ShowText
    {
        get => (bool)GetValue(ShowTextProperty);
        set => SetValue(ShowTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the addresses displayed in the address section of the control.
    /// </summary>
    public AddressFormat AddressFormat
    {
        get => (AddressFormat)GetValue(AddressFormatProperty);
        set => SetValue(AddressFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the format of the text to display in the text section.
    /// </summary>
    public TextFormat TextFormat
    {
        get => (TextFormat)GetValue(TextFormatProperty);
        set => SetValue(TextFormatProperty, value);
    }

    /// <summary>
    /// Gets the header text for the toggle text command.
    /// </summary>
    public string ToggleTextHeader => ShowText ? "Hide Text" : "Show Text";

    /// <summary>
    /// Gets the header text for the toggle signedness command.
    /// </summary>
    public string ToggleSignednessHeader => DataSignedness == DataSignedness.Signed ? "Unsigned" : "Signed";

    /// <summary>
    /// Gets the header text for the toggle endianness command.
    /// </summary>
    public string ToggleEndiannessHeader => Endianness == Endianness.BigEndian ? "Little-endian" : "Big-endian";

    /// <summary>
    /// Gets a value indicating whether the signedness toggle command can execute.
    /// </summary>
    public bool CanToggleSignedness => ShowData && DataType == DataType.Integer && DataFormat == DataFormat.Decimal;

    private static void OnPropertyChangedInvalidateVisual(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(e.Property.Name);
        hexViewer.OnPropertyChanged(nameof(ToggleTextHeader));
        hexViewer.OnPropertyChanged(nameof(ToggleSignednessHeader));
        hexViewer.OnPropertyChanged(nameof(ToggleEndiannessHeader));
        hexViewer.OnPropertyChanged(nameof(CanToggleSignedness));
        hexViewer.ToggleSignednessCommand.NotifyCanExecuteChanged();
    }

    private static void OnSelectionEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(nameof(SelectionEnd));
        hexViewer.OnPropertyChanged(nameof(SelectionLength));
        hexViewer.OnPropertyChanged(nameof(SelectedOffset));
        hexViewer.OnPropertyChanged(nameof(SelectedAddress));
        hexViewer.OnPropertyChanged(nameof(IsSelectionActive));
    }

    private static void OnSelectionStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(nameof(SelectionStart));
        hexViewer.OnPropertyChanged(nameof(SelectionLength));
        hexViewer.OnPropertyChanged(nameof(SelectedOffset));
        hexViewer.OnPropertyChanged(nameof(SelectedAddress));
        hexViewer.OnPropertyChanged(nameof(IsSelectionActive));
    }

    private static object CoerceColumns(DependencyObject d, object value)
    {
        var hexViewer = (HexViewer)d;

        if (hexViewer.MaxVisibleColumns == 0)
        {
            return (int)value;
        }
        else
        {
            return Math.Min((int)value, hexViewer.MaxVisibleColumns);
        }
    }

    private static object CoerceMaxVisibleColumns(DependencyObject d, object value)
    {
        return Math.Min((int)value, MaxColumns);
    }

    private static object CoerceMaxVisibleRows(DependencyObject d, object value)
    {
        return Math.Min((int)value, MaxRows);
    }

    private static object CoerceSelectionStart(DependencyObject d, object value)
    {
        var hexViewer = (HexViewer)d;

        if (hexViewer.DataSource != null)
        {
            long selectionStart = (long)value;

            // Selection offset cannot start in the middle of the data width
            selectionStart -= selectionStart % hexViewer.BytesPerColumn;

            // Selection start cannot be at the end of the stream so adjust by data width number of bytes
            value = Math.Clamp(selectionStart, 0, (hexViewer.DataSource.BaseStream.Length / hexViewer.BytesPerColumn * hexViewer.BytesPerColumn) - hexViewer.BytesPerColumn);
        }
        else
        {
            value = 0L;
        }

        return value;
    }

    private static object CoerceSelectionEnd(DependencyObject d, object value)
    {
        var hexViewer = (HexViewer)d;

        if (hexViewer.DataSource != null)
        {
            long selectionEnd = (long)value;

            // Selection offset cannot start in the middle of the data width
            selectionEnd -= selectionEnd % hexViewer.BytesPerColumn;

            // Unlike selection start the selection end can be at the end of the stream
            value = Math.Clamp(selectionEnd, 0, hexViewer.DataSource.BaseStream.Length / hexViewer.BytesPerColumn * hexViewer.BytesPerColumn);
        }
        else
        {
            value = 0L;
        }

        return value;
    }

    private static object CoerceDataWidth(DependencyObject d, object value)
    {
        var hexViewer = (HexViewer)d;

        if (hexViewer.DataType == DataType.FloatingPoint && (int)value < 4)
        {
            value = 4;
        }

        return value;
    }

    private static object CoerceOffset(DependencyObject d, object value)
    {
        var hexViewer = (HexViewer)d;

        if (hexViewer.DataSource != null)
        {
            long offset = (long)value;

            value = Math.Clamp(offset, 0, hexViewer.DataSource.BaseStream.Length);
        }
        else
        {
            value = 0L;
        }

        return value;
    }

    private static bool ValidateDataWidth(object value) => (int)value is 1 or 2 or 4 or 8;

    private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.SelectionStart = 0;
        hexViewer.SelectionEnd = 0;

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(nameof(Address));
        hexViewer.OnPropertyChanged(nameof(SelectedAddress));
    }

    private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.CoerceValue(DataWidthProperty);

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(nameof(DataType));
        hexViewer.OnPropertyChanged(nameof(CanToggleSignedness));
        hexViewer.ToggleSignednessCommand.NotifyCanExecuteChanged();
    }

    private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.Offset = 0;
        hexViewer.SelectionStart = 0;
        hexViewer.SelectionEnd = 0;

        hexViewer.InvalidateVisual();
    }

    private static void OnDataWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var hexViewer = (HexViewer)d;

        hexViewer.SelectionStart = 0;
        hexViewer.SelectionEnd = 0;

        hexViewer.InvalidateVisual();
        hexViewer.OnPropertyChanged(nameof(DataWidth));
        hexViewer.OnPropertyChanged(nameof(CanToggleSignedness));
        hexViewer.ToggleSignednessCommand.NotifyCanExecuteChanged();
    }
}
