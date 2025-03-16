using System.Numerics;

namespace chartview_csharp.Chart.Helper;

public static class LaneMathHelper
{
    public static int CalculateHorizontalPosition(double size, double gap, int laneIndex, int lanes)
    {
        var center = ChartviewCore.WindowSize.X / 2;
        var playfieldSize = (size + gap) * 2 * (lanes - 1);
        return (int)(center - playfieldSize / 2 + (size + gap) * 2 * laneIndex);
    }
    
    public static int CalculateHorizontalPositionMinimap(int minimapWidth, int laneIndex, int lanes)
    {
        var playfieldSize = 2 * (lanes - 1);
        return minimapWidth - playfieldSize + 2 * laneIndex;
    }
    
    public static Vector2 CalculateHorizontalLaneBounds(double size, double gap, int lanes)
    {
        var center = ChartviewCore.WindowSize.X / 2;
        var playfieldSize = (size + gap) * 2 * (lanes - 1);
        var left = center - playfieldSize / 2 - (size + 4);
        var right = center + playfieldSize / 2 + size + 4;
        return new Vector2((float)left, (float)right);
    }
}