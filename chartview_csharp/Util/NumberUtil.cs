using System.Globalization;

namespace chartview_csharp.Util;

public static class NumberUtil
{
    public static double ParseDouble(string input)
    {
        return double.Parse(input, CultureInfo.InvariantCulture.NumberFormat);
    }
}