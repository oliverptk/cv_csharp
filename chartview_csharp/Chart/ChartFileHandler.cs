using System.IO.Compression;
using System.Text;
using chartview_csharp.Chart.Enum;
using chartview_csharp.Util;
using chartview_csharp.Util.Data;

namespace chartview_csharp.Chart;

public static class ChartFileHandler
{
    public static void HandleDroppedPlainChartFile(string file)
    {
        var split = file.Split(".");
        var fileType = split[^1];

        var interpreterType = Interpreter.None;

        switch (fileType) {
            case "osu":
                interpreterType = Interpreter.Osu;
                break;
            case "qua":
                interpreterType = Interpreter.Quaver;
                break;
            case "sm":
                interpreterType = Interpreter.Sm;
                break;
            case "scc":
                interpreterType = Interpreter.Scc;
                break;
            default:
                Console.WriteLine("[chartview] Unknown chart file type!");
                break;
        }

        var chart = Chart.CreateChartFromFile(interpreterType, file);
        
        if (chart.ChartAlreadyLoaded())
        {
            Console.WriteLine("[chartview] Chart with matching CRC32 already loaded, skipping!");
            return;
        }

        ChartviewCore.ChartManager.CurrentChartIndex = ChartviewCore.ChartManager.LoadedCharts.Count;
        ChartviewCore.ChartManager.LastChartIndex = ChartviewCore.ChartManager.LoadedCharts.Count;
        ChartviewCore.ChartManager.LoadedCharts.Add(chart);
    }

    public static void HandleDroppedPackedChartFile(string file)
    {
        var isQuaver = file.EndsWith(".qp");

        var compact = new ZipArchive(File.Open(file, FileMode.Open), ZipArchiveMode.Read);

        ChartviewCore.ChartManager.CurrentChartIndex = ChartviewCore.ChartManager.LoadedCharts.Count;
        ChartviewCore.ChartManager.LastChartIndex = ChartviewCore.ChartManager.LoadedCharts.Count;

        var importedCharts = new List<Chart>();
        
        foreach (var compacted in compact.Entries)
        {
            if (!compacted.Name.EndsWith(".osu") && !compacted.Name.EndsWith(".qua")) continue;
            
            var chartContainer = FileUtil.ReadCompressedFileBytesCrc32(compacted);

            if (Chart.ChartAlreadyLoadedExternal(chartContainer.Crc32))
            {
                Console.WriteLine("[chartview] Chart with matching CRC32 already loaded, skipping!");
                continue;
            }

            var mode = isQuaver ? Interpreter.Quaver : Interpreter.Osu;

            var chart = Chart.CreateChartFromStingArray(mode, Encoding.UTF8.GetString(chartContainer.Bytes).Split("\n"), chartContainer.Crc32);
            importedCharts.Add(chart);
        }
        
        var validAudioFiles = new List<string>();
        
        foreach (var chart in importedCharts.Where(chart => !validAudioFiles.Contains(chart.AudioLocation)))
        {
            validAudioFiles.Add(chart.AudioLocation);
        }
        
        foreach (var audio in compact.Entries)
        {
            if (!validAudioFiles.Contains(audio.Name)) continue;
            
            var audioContainer = FileUtil.ReadCompressedFileBytesCrc32(audio);

            foreach (var chart in importedCharts.Where(chart => chart.AudioLocation == audio.Name))
            {
                chart.Identifier.AudioCRC32 = audioContainer.Crc32;
            }

            ChartviewCore.ChartManager.LoadedAudios[audioContainer.Crc32] = audioContainer.Bytes;
        }
        
        ChartviewCore.ChartManager.LoadedCharts.AddRange(importedCharts);
    }
}