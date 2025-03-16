namespace chartview_csharp;

public static class ChartviewGlobals
{
    public static double HitObjectDensity { get; set; } = 1.0;
    public static double HitObjectSize { get; set; } = 32.0;
    public static double MinimapHitObjectSize { get; set; } = 1.0;
    public static double LaneGap { get; set; } = 2.0;
    
    public static int VisualOffset { get; set; } = 32;
    public static bool IsUpscroll { get; set; } = false;
    
    public static double PlayfieldScrollMs { get; set; } = 0;
    
    public static int BeatSnapDivisor { get; set; } = 8;
    
    public static bool DisableScrolling { get; set; } = false;
    
    public static uint MaxAudioLengthMs { get; set; } = 0;
    public static uint AudioEndPadding { get; set; } = 1000;
    public static float AudioPlaybackSpeed { get; set; } = 1.0f;
    
    public static bool ShouldUseMsaa { get; set; } = true;
    
    public static bool IsScrollPastEnd()
    {
        return PlayfieldScrollMs >= MaxAudioLengthMs - 1.0;
    }

    public static bool IsScrollPastAudio()
    {
        return PlayfieldScrollMs >= MaxAudioLengthMs - AudioEndPadding;
    }

    public static bool IsScrollPastStart()
    {
        return PlayfieldScrollMs >= 0;
    }
}