using chartview_csharp.Chart.Data;
using Raylib_cs;

namespace chartview_csharp.Chart;

public class ChartManager
{
    public List<Chart> LoadedCharts { get; } = [];
    public Dictionary<uint, byte[]> LoadedAudios { get; } = new();

    public int CurrentChartIndex { get; set; }
    public int LastChartIndex { get; set; }
    public Texture2D CurrentChartMinimap { get; private set; }
    
    public Music CurrentChartAudio { get; private set; }

    public Chart? GetCurrentChart()
    {
        return LoadedCharts.Count == 0 ? null : LoadedCharts[CurrentChartIndex];
    }

    public bool IsCurrentChartEmpty()
    {
        var chart = GetCurrentChart();
        return chart == null || Metadata.IsDefaultMetadata(chart.Metadata);
    }
    
    public byte[]? GetCurrentChartAudioRaw()
    {
        var chart = GetCurrentChart();
        return chart == null || LoadedAudios.Count == 0 ? null : LoadedAudios[chart.Identifier.AudioCRC32];
    }

    public void UpdateCurrentChartAudioAndMinimap()
    {
        Raylib.UnloadMusicStream(CurrentChartAudio);
        
        var chart = GetCurrentChart();

        if (chart == null)
        {
            Console.WriteLine("[chartview] UpdateCurrentChartAudioAndMinimap called before first chart was imported!");
            return;
        }

        var filetype = chart.AudioLocation.Split(".");
        var rawAudio = GetCurrentChartAudioRaw();

        if (rawAudio == null)
        {
            Console.WriteLine("[chartview] UpdateCurrentChartAudioAndMinimap called without a correct loaded audio file!");
            return;
        }
        
        CurrentChartAudio = Raylib.LoadMusicStreamFromMemory($".{filetype[^1]}", rawAudio);
        
        Raylib.SetWindowTitle($"chartview (csharp) | {chart.Metadata.DisplayString}");

        ChartviewGlobals.MaxAudioLengthMs = (CurrentChartAudio.FrameCount / CurrentChartAudio.Stream.SampleRate) * 1000 + ChartviewGlobals.AudioEndPadding;
        ChartviewGlobals.PlayfieldScrollMs = 0;
        
        Raylib.SetMusicVolume(CurrentChartAudio, 0.5f);
        Raylib.SeekMusicStream(CurrentChartAudio, 0);

        CurrentChartMinimap = ChartRenderer.GenerateChartMinimap(chart);
    }
    
    public List<string> GetDropdownFriendlyChartList()
    {
        var charts = new List<string>();
        charts.AddRange(LoadedCharts.Select(chart => $"{chart.Metadata.Artist} - {chart.Metadata.Title} [{chart.Metadata.Difficulty}] ({chart.Lanes}K)"));
        return charts;
    }
    
    public void SyncLoadedMusic()
    {
        var average = ChartviewGlobals.MaxAudioLengthMs / 1000.0 * 1.05;
        Raylib.SeekMusicStream(CurrentChartAudio, (float)(ChartviewGlobals.PlayfieldScrollMs + average) / 1000.0f);
        Raylib.SetAudioStreamPitch(CurrentChartAudio.Stream, ChartviewGlobals.AudioPlaybackSpeed);
    }
}