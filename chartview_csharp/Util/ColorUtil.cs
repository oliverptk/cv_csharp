using Raylib_cs;

namespace chartview_csharp.Util;

public static class ColorUtil
{
    public static Color Darken(Color c)
    {
        return new Color((int)(c.R * 0.5f), (int)(c.G * 0.5f), (int)(c.B * 0.5f), c.A);
    }
}