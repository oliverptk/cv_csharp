using chartview_csharp.Chart.Enum;

namespace chartview_csharp.Chart.Data;

public record HitObject
{
    public required Offset Offset { get; set; }
    public required double LastLocalOffset { get; set; }
    public required int Lane { get; set; }
    public required bool Hold { get; set; }
    public required SnapDivisor BeatRelation { get; set; }
    
    public double GetLocalOffsetStart()
    {
        return (Offset.Start - ChartviewGlobals.PlayfieldScrollMs) / ChartviewGlobals.HitObjectDensity;
    }

    public double GetLocalOffsetEnd() 
    {
        return (Offset.End - ChartviewGlobals.PlayfieldScrollMs) / ChartviewGlobals.HitObjectDensity;
    }
}