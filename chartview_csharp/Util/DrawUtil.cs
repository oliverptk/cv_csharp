using System.Numerics;
using Raylib_cs;

namespace chartview_csharp.Util;

public static class DrawUtil
{
    public static Font LoadFontScaled(string filename, int size)
    {
        var loaded = Raylib.LoadFontEx(filename, size, null, 256);
        Raylib.GenTextureMipmaps(ref loaded.Texture);
        Raylib.SetTextureFilter(loaded.Texture, TextureFilter.Trilinear);
        return loaded;
    }

    public static void DrawTextScaled(Font font, string text, float x, float y, float size, Color color)
    {
        var scaleFactor = size / 256f;
        RaylibMatrixPushPop(() =>
        {
            Rlgl.Scalef(scaleFactor, scaleFactor, 1f);
            Raylib.DrawTextEx(font, text, new Vector2(x / scaleFactor, y / scaleFactor), 256f, 1f, color);
        });
    }
    
    public static void DrawFrameCounter(float size) {
        // var textSize = rl.MeasureTextEx(roboto, fmt.Sprintf("%d FPS", rl.GetFPS()), size, 1)
        DrawTextScaled(ChartviewCore.FontRoboto, $"{Raylib.GetFPS()} FPS", 2, 2, size, Color.RayWhite);
    }

    public static void RaylibMatrixPushPop(Action func)
    {
        Rlgl.PushMatrix();
        func();
        Rlgl.PopMatrix();
    }
}