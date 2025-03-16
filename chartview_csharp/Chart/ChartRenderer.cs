using System.Numerics;
using chartview_csharp.Chart.Data;
using chartview_csharp.Chart.Helper;
using chartview_csharp.Util;
using Raylib_cs;

namespace chartview_csharp.Chart;

public static class ChartRenderer
{
    private static readonly Dictionary<int, float> Lighting = new();
    
    public static Texture2D GenerateChartMinimap(Chart chart)
    {
        var laneWidth = chart.Lanes * 2;
        var hitObjectMinimap = Raylib.GenImageColor(laneWidth, (int)ChartviewCore.WindowSize.Y, new Color(0, 0, 0, 0));

        var mmDensity = ChartviewGlobals.MaxAudioLengthMs / (ChartviewCore.WindowSize.Y - ChartviewGlobals.MinimapHitObjectSize);
        
        foreach (var obj in chart.HitObjects)
        {
            var localOffset = obj.Offset.Start / mmDensity;

            var objectColor = BeatRelationHelper.GetObjectColor(obj.BeatRelation);

            if (obj.Hold)
            {
                var localEndOffset = obj.Offset.End / mmDensity;
                var offsetDiff = localEndOffset - localOffset;
                var holdOffset = localOffset + offsetDiff;

                while (offsetDiff > 0)
                {
                    Raylib.ImageDrawRectangle(
                        ref hitObjectMinimap, 
                        LaneMathHelper.CalculateHorizontalPositionMinimap(laneWidth, obj.Lane, chart.Lanes), 
                        (int)(ChartviewCore.WindowSize.Y - holdOffset - ChartviewGlobals.MinimapHitObjectSize), 
                        (int)ChartviewGlobals.MinimapHitObjectSize, 
                        (int)ChartviewGlobals.MinimapHitObjectSize, 
                        objectColor);
                    offsetDiff -= ChartviewGlobals.MinimapHitObjectSize / 8.0;
                    holdOffset = localOffset + offsetDiff;
                }
            }
            
            Raylib.ImageDrawRectangle(ref hitObjectMinimap, 
                LaneMathHelper.CalculateHorizontalPositionMinimap(laneWidth, obj.Lane, chart.Lanes), 
                (int)(ChartviewCore.WindowSize.Y - localOffset - ChartviewGlobals.MinimapHitObjectSize), 
                (int)ChartviewGlobals.MinimapHitObjectSize, 
                (int)ChartviewGlobals.MinimapHitObjectSize, 
                objectColor);
        }

        return Raylib.LoadTextureFromImage(hitObjectMinimap);
    }

    public static void RenderLane(Chart chart)
    {
        var laneBounds = LaneMathHelper.CalculateHorizontalLaneBounds(ChartviewGlobals.HitObjectSize, ChartviewGlobals.LaneGap, chart.Lanes);
        
        Raylib.DrawRectangle((int)laneBounds.X, 
            ChartviewGlobals.VisualOffset, 
            (int)(laneBounds.Y-laneBounds.X), 
            (int)ChartviewCore.WindowSize.Y+ChartviewGlobals.VisualOffset, 
            new Color(0x00, 0x00, 0x00, 0x70));

        foreach (var point in chart.TimingPoints)
        {
            var count = 0;

            double maxDist;
            if (point == chart.TimingPoints[^1]) maxDist = ChartviewGlobals.MaxAudioLengthMs;
            else maxDist = chart.TimingPoints[chart.TimingPoints.IndexOf(point) + 1].Offset;

            var beatSnapScrollMs = point.Offset;
            
            while (beatSnapScrollMs < maxDist)
            {
                var localOffset = (beatSnapScrollMs - ChartviewGlobals.PlayfieldScrollMs) / ChartviewGlobals.HitObjectDensity;

                if (localOffset + ChartviewGlobals.VisualOffset >= 0 && 
                    localOffset - ChartviewGlobals.VisualOffset <= ChartviewCore.WindowSize.Y)
                {
                    var drawHeight = ChartviewCore.WindowSize.Y - localOffset;
                    var drawColor = BeatRelationHelper.GetLaneColor(count);

                    if (point.BPM < 1000 || count % ChartviewGlobals.BeatSnapDivisor == 0)
                    {
                        Raylib.DrawLine((int)laneBounds.X, (int)drawHeight, (int)laneBounds.Y, (int)drawHeight, drawColor);
                    }
                }
                
                beatSnapScrollMs += point.MillisPerBeat / ChartviewGlobals.BeatSnapDivisor;
                count++;
            }

            var bpmOffset = (point.Offset - ChartviewGlobals.PlayfieldScrollMs) / ChartviewGlobals.HitObjectDensity;

            if (bpmOffset + ChartviewGlobals.VisualOffset >= 0 &&
                bpmOffset - ChartviewGlobals.VisualOffset <= ChartviewCore.WindowSize.Y)
            {
                if (ChartviewGlobals.IsUpscroll)
                {
                    DrawUtil.RaylibMatrixPushPop(() =>
                    {
                        Rlgl.Rotatef(180, 0, 0, 1);
                        Rlgl.Translatef(-ChartviewCore.WindowSize.X, -ChartviewCore.WindowSize.Y, 0);
                        DrawUtil.DrawTextScaled(ChartviewCore.FontRoboto, $"{point.BPM:0.##}BPM", laneBounds.Y + 2, (float)bpmOffset, 16, Color.White);
                    });
                }
                else DrawUtil.DrawTextScaled(ChartviewCore.FontRoboto, $"{point.BPM:0.##}BPM", laneBounds.Y + 2, (float)(ChartviewCore.WindowSize.Y - bpmOffset), 16, Color.White);
            }
        }
    }

    private static void RenderHoldObject(HitObject obj, int lanes, int laneIndex)
    {
        var objectColor = BeatRelationHelper.GetObjectColor(obj.BeatRelation);
        var passColor = ColorUtil.Darken(objectColor);
        var endColor = objectColor;

        var localOffset = obj.GetLocalOffsetStart();
        var localEndOffset = obj.GetLocalOffsetEnd();
        var offsetDiff = localEndOffset - localOffset;

        if (localEndOffset <= 0) endColor = passColor;

        var pHorizontal = LaneMathHelper.CalculateHorizontalPosition(ChartviewGlobals.HitObjectSize, ChartviewGlobals.LaneGap, laneIndex, lanes);
        
        Raylib.DrawCircle(pHorizontal, 
            (int)(ChartviewCore.WindowSize.Y-localEndOffset-ChartviewGlobals.HitObjectSize), 
            (float)ChartviewGlobals.HitObjectSize, 
            endColor);
        
        Raylib.DrawRectangle((int)(pHorizontal-ChartviewGlobals.HitObjectSize), 
            (int)(ChartviewCore.WindowSize.Y-localEndOffset-ChartviewGlobals.HitObjectSize),
            (int)ChartviewGlobals.HitObjectSize*2,
            (int)(localEndOffset-localOffset),
            objectColor);

        var yPos = Math.Clamp((localEndOffset - localOffset) - offsetDiff, localOffset, localEndOffset);
        
        Raylib.DrawRectangle((int)(pHorizontal-ChartviewGlobals.HitObjectSize), 
            (int)(ChartviewCore.WindowSize.Y-yPos-ChartviewGlobals.HitObjectSize),
            (int)ChartviewGlobals.HitObjectSize*2,
            (int)(yPos-localOffset),
            passColor);

        if (localOffset <= 0)
        {
            Raylib.DrawCircle(pHorizontal,
                (int)(ChartviewCore.WindowSize.Y-yPos-ChartviewGlobals.HitObjectSize),
                (float)ChartviewGlobals.HitObjectSize,
                passColor);
        }

        if ((int)localEndOffset >= 0 && (int)localOffset <= 0) Lighting[laneIndex] = 1.0f;
    }

    public static void RenderChart(Chart chart)
    {
        for (var i = 0; i < chart.Lanes; i++)
        {
            var ringPos = new Vector2(
                LaneMathHelper.CalculateHorizontalPosition(ChartviewGlobals.HitObjectSize, ChartviewGlobals.LaneGap, i, chart.Lanes),
                (float)(ChartviewCore.WindowSize.Y - ChartviewGlobals.HitObjectSize));
            Raylib.DrawRing(ringPos, 
                (float)ChartviewGlobals.HitObjectSize, 
                (float)ChartviewGlobals.HitObjectSize + 1.5f, 
                0, 
                360, 
                36, 
                Color.White);
        }

        foreach (var obj in chart.HitObjects)
        {
            var localOffset = obj.GetLocalOffsetStart();

            var laneIndex = obj.Lane;

            if (ChartviewGlobals.IsUpscroll) {
                laneIndex = (chart.Lanes - 1) - laneIndex; // invert used lane index if using upscroll
            }

            var objectColor = BeatRelationHelper.GetObjectColor(obj.BeatRelation);
            var passColor = ColorUtil.Darken(objectColor);

            var visibility = localOffset + ChartviewGlobals.VisualOffset + ChartviewGlobals.HitObjectSize * 2 >= 0 &&
                             localOffset - ChartviewGlobals.VisualOffset <= ChartviewCore.WindowSize.Y;

            if (obj.Hold)
            {
                var localEndOffset = obj.GetLocalOffsetEnd();
                visibility = localEndOffset + ChartviewGlobals.HitObjectSize >= 0 &&
                             localOffset <= ChartviewCore.WindowSize.Y;
            }

            if (visibility)
            {
                var usedColor = objectColor;

                if (obj.Hold) RenderHoldObject(obj, chart.Lanes, laneIndex);

                if (localOffset <= 0) usedColor = passColor;

                var hPos = LaneMathHelper.CalculateHorizontalPosition(ChartviewGlobals.HitObjectSize, 
                    ChartviewGlobals.LaneGap, 
                    laneIndex,
                    chart.Lanes);
                
                Raylib.DrawCircle(hPos, 
                    (int)(ChartviewCore.WindowSize.Y-localOffset-ChartviewGlobals.HitObjectSize),
                    (float)ChartviewGlobals.HitObjectSize,
                    usedColor);

                if (localOffset <= 0 && obj.LastLocalOffset >= 0)
                {
                    Lighting[laneIndex] = 1.0f;
                }

                obj.LastLocalOffset = localOffset;
            }
        }
        
        foreach (var (key, light) in Lighting)
        {
            if (light <= 0.0 || !Raylib.IsMusicStreamPlaying(ChartviewCore.ChartManager.CurrentChartAudio)) continue;
            Raylib.DrawCircle(
                LaneMathHelper.CalculateHorizontalPosition(ChartviewGlobals.HitObjectSize, 
                    ChartviewGlobals.LaneGap, 
                    key, 
                    chart.Lanes),
                (int)(ChartviewCore.WindowSize.Y - ChartviewGlobals.HitObjectSize),
                (float)ChartviewGlobals.HitObjectSize,
                new Color(0xaa, 0xaa, 0xaa, (int)(0xff * Lighting[key])));
            Lighting[key] -= 12.5f * Raylib.GetFrameTime();
            Lighting[key] = Math.Clamp(Lighting[key], 0.0f, 1.0f);
        }
    }
}