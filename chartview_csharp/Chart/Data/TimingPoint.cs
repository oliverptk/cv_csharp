namespace chartview_csharp.Chart.Data;

public record TimingPoint
{
    public required double Offset { get; set; }
    public required double BPM { get; set; }
    public required double MillisPerBeat { get; set; }
    public required double SimfileBeatOffset { get; set; }
    
    public static readonly TimingPoint Default = new()
    {
        Offset = 0,
        BPM = 0,
        MillisPerBeat = 0,
        SimfileBeatOffset = 0
    };
}