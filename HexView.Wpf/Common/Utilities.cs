namespace Juknum.HexView.Common;

using System;
using System.Numerics;

/// <summary>
/// A utility class with miscellaneous methods.
/// </summary>
internal static class Utilities
{


    /// <summary>
    /// Calculates the arithmetic modulus of <paramref name="n"/> modulo <paramref name="m"/>.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of the values.
    /// </typeparam>
    ///
    /// <param name="n">
    /// The value to compute the modulus of.
    /// </param>
    ///
    /// <param name="m">
    /// The modulus.
    /// </param>
    ///
    /// <returns>
    /// The non-negative value <c>r</c> such that for some integral value <c>q</c>:
    /// <c><paramref name="n"/> = q*m + r</c>.
    /// </returns>
    public static T Mod<T>(this T n, T m)
        where T : IBinaryInteger<T>
    {
        T r = n % m;
        return r < T.Zero ? r + m : r;
    }
}
