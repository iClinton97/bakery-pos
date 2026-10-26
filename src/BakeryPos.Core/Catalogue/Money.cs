using System;
using System.Globalization;

namespace BakeryPos.Core.Catalogue;

public static class Money
{
    /// <summary>Formats whole cents as dollars, for example 1284 becomes "$12.84".</summary>
    public static string Format(int cents)
    {
        var absolute = Math.Abs((long)cents);
        var dollars = (absolute / 100).ToString("N0", CultureInfo.InvariantCulture);
        var sign = cents < 0 ? "-" : "";
        return $"{sign}${dollars}.{absolute % 100:00}";
    }
}
