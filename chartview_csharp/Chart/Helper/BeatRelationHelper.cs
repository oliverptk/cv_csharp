using System.Numerics;
using chartview_csharp.Chart.Enum;
using Raylib_cs;

namespace chartview_csharp.Chart.Helper;

public static class BeatRelationHelper
{
    private static float VarianceThreshold => 0.005f; // maximum variance allowed for estimating beat snapping relation
    private static Vector2 RangeMinimum => new(-VarianceThreshold, VarianceThreshold);
    private static Vector2 RangeMaximum => new(1.0f - VarianceThreshold, 1.0f);

    private static readonly Dictionary<Vector2, SnapDivisor> VarianceRanges = new()
    {
        { RangeMinimum,            SnapDivisor.OneOne },
        { RangeMaximum,            SnapDivisor.OneOne },
        { CreateRange(0.5f),    SnapDivisor.OneTwo },
        { CreateRange(0.333f),  SnapDivisor.OneThree },
        { CreateRange(0.666f),  SnapDivisor.OneThree },
        { CreateRange(0.25f),   SnapDivisor.OneFour },
        { CreateRange(0.75f),   SnapDivisor.OneFour },
        { CreateRange(0.1666f), SnapDivisor.OneSix },
        { CreateRange(0.8333f), SnapDivisor.OneSix },
        { CreateRange(0.125f),  SnapDivisor.OneEight },
        { CreateRange(0.375f),  SnapDivisor.OneEight },
        { CreateRange(0.625f),  SnapDivisor.OneEight },
        { CreateRange(0.875f),  SnapDivisor.OneEight },
        { CreateRange(0.0833f), SnapDivisor.OneTwelve },
        { CreateRange(0.4166f), SnapDivisor.OneTwelve },
        { CreateRange(0.5833f), SnapDivisor.OneTwelve },
        { CreateRange(0.9166f), SnapDivisor.OneTwelve },
        { CreateRange(0.0625f), SnapDivisor.OneSixteen },
        { CreateRange(0.1875f), SnapDivisor.OneSixteen },
        { CreateRange(0.3125f), SnapDivisor.OneSixteen },
        { CreateRange(0.4375f), SnapDivisor.OneSixteen },
        { CreateRange(0.5625f), SnapDivisor.OneSixteen },
        { CreateRange(0.6875f), SnapDivisor.OneSixteen },
        { CreateRange(0.8125f), SnapDivisor.OneSixteen },
        { CreateRange(0.9375f), SnapDivisor.OneSixteen }
    };

    private static readonly Dictionary<SnapDivisor, Color> ColorMap = new()
    {
        { SnapDivisor.OneOne, Color.RayWhite },
        { SnapDivisor.OneTwo, new Color(0xff, 0x20, 0x20, 0xff) },
        { SnapDivisor.OneThree, new Color(0xff, 0x66, 0x00, 0xff) },
        { SnapDivisor.OneFour, new Color(0x20, 0x20, 0xff, 0xff) },
        { SnapDivisor.OneSix, new Color(0xff, 0x66, 0x00, 0xff) },
        { SnapDivisor.OneEight, new Color(0xff, 0xff, 0x20, 0xff) },
        { SnapDivisor.OneTwelve, new Color(0xff, 0x66, 0x00, 0xff) },
        { SnapDivisor.OneSixteen, new Color(0xe2, 0x32, 0xf7, 0xff) },
        { SnapDivisor.UNKNOWN, new Color(0x80, 0x80, 0x80, 0xff) }
    };
    
    private static Vector2 CreateRange(float v)
    {
        return new Vector2(v - VarianceThreshold, v + VarianceThreshold);
    }
    
    public static Color GetObjectColor(SnapDivisor relation)
    {
        return ColorMap[relation];
    }
    
    public static Color GetLaneColor(int count)
    {
        var laneColor = ColorMap[SnapDivisor.UNKNOWN];
        var checkedDivisor = 16;

        while (checkedDivisor >= 1) 
        {
            if (ChartviewGlobals.BeatSnapDivisor >= checkedDivisor && count%(ChartviewGlobals.BeatSnapDivisor/checkedDivisor) == 0)
            {
                laneColor = checkedDivisor switch
                {
                    16 => ColorMap[SnapDivisor.OneSixteen],
                    8 => ColorMap[SnapDivisor.OneEight],
                    4 => ColorMap[SnapDivisor.OneFour],
                    2 => ColorMap[SnapDivisor.OneTwo],
                    1 => ColorMap[SnapDivisor.OneOne],
                    _ => laneColor
                };
            }

            checkedDivisor /= 2;
        }

        return laneColor;
    }
    
    public static SnapDivisor ComputeSnapDivisor(double relation) 
    {
        foreach (var (k, v) in VarianceRanges)
        {
            if (relation >= k.X && relation <= k.Y) return v;
        }

        return SnapDivisor.UNKNOWN;
    }
}