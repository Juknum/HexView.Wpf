namespace Juknum.HexView.Common.Converters;

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// A converter which tests for equality against an <see cref="ArrayList"/> of parameters.
/// </summary>
internal class MultiEqualityConverter : IMultiValueConverter
{
    /// <summary>
    /// Tests the values against the sequence of parameters for equality.
    /// </summary>
    ///
    /// <param name="values">
    /// The values produced by the binding source.
    /// </param>
    ///
    /// <param name="targetType">
    /// The type of the binding target property which must be <see cref="bool"/> or <see cref="Visibility"/>.
    /// </param>
    ///
    /// <param name="parameter">
    /// The converter parameters to use which must be of type <see cref="ArrayList"/>.
    /// </param>
    ///
    /// <param name="culture">
    /// The culture to use in the converter which is unused.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> if the one-to-one mapping of <paramref name="values"/> to <paramref name="parameter"/> all test
    /// for equality induvidually; <c>false</c> otherwise.
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not IList parameters || values == null || parameters.Count != values.Length)
        {
            return targetType == typeof(Visibility) ? Visibility.Collapsed : false;
        }

        bool equals = true;
        for (var i = 0; i < values.Length; ++i)
        {
            if (!Equals(values[i], parameters[i]))
            {
                equals = false;
                break;
            }
        }

        if (targetType == typeof(Visibility))
        {
            return equals ? Visibility.Visible : Visibility.Collapsed;
        }

        return equals;
    }

    /// <summary>
    /// Tests the values against the sequence of parameters for equality.
    /// </summary>
    ///
    /// <param name="value">
    /// The values produced by the binding source.
    /// </param>
    ///
    /// <param name="targetTypes">
    /// The type of the binding target property which must be <see cref="bool"/>.
    /// </param>
    ///
    /// <param name="parameter">
    /// The converter parameters to use which must be of type <see cref="ArrayList"/>.
    /// </param>
    ///
    /// <param name="culture">
    /// The culture to use in the converter which is unused.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> if the one-to-one mapping of <paramref name="value"/> to <paramref name="parameter"/> all test
    /// for equality induvidually; <c>false</c> otherwise.
    /// </returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        var parameters = (ArrayList)parameter;

        var result = new object[parameters.Count];
        parameters.CopyTo(result);

        return result;
    }
}
