using System.Globalization;
using System.Text;
using chartview_csharp.Chart.Data;
using chartview_csharp.Chart.Enum;
using chartview_csharp.Chart.Helper;
using chartview_csharp.Util;

namespace chartview_csharp.Chart;

public class Chart
{
    public List<HitObject> HitObjects { get; } = [];
    public List<TimingPoint> TimingPoints { get; } = [];

    public Identifier Identifier { get; set; } = new()
    {
        CRC32 = 0,
        AudioCRC32 = 0
    };
    public Metadata Metadata { get; set; } = new()
    {
        UnicodeTitle = "",
        UnicodeArtist = "",
        Title = "",
        Artist = "",
        ChartCreator = "",
        Difficulty = "",
        DisplayString = "",
        Format = Interpreter.None
    };

    public string AudioLocation { get; private set; } = "";
    public int Lanes { get; private set; }
    public double SimfileOffset { get; set; } = 0;

    public HitObject GetLastHitObject()
    {
        return HitObjects[^1];
    }
    
    public TimingPoint GetCorrectTimingPoint(int offset)
    {
        var detected = TimingPoints[0];
        foreach (var timingPoint in TimingPoints.TakeWhile(timingPoint => !(Math.Floor(timingPoint.Offset) > offset))) detected = timingPoint;
        return detected;
    }

    public bool ChartAlreadyLoaded()
    {
        if (ChartviewCore.ChartManager.LoadedCharts.Count == 0) return false;
        return ChartviewCore.ChartManager.LoadedCharts.FindAll(chart => chart.Identifier.CRC32 == Identifier.CRC32).Count != 0;
    }
    
    public static bool ChartAlreadyLoadedExternal(uint crc32)
    {
        if (ChartviewCore.ChartManager.LoadedCharts.Count == 0) return false;
        return ChartviewCore.ChartManager.LoadedCharts.FindAll(chart => chart.Identifier.CRC32 == crc32).Count != 0;
    }

    public static Chart CreateChartFromFile(Interpreter mode, string file)
    {
        var chartContainer = FileUtil.ReadFileBytesCrc32(file);
        var lines = Encoding.UTF8.GetString(chartContainer.Bytes).Split("\n");

        var chart = CreateChartBase(mode, lines, file);
        chart.Identifier.CRC32 = chartContainer.Crc32;
        
        var audioContainer = FileUtil.ReadFileBytesCrc32(chart.AudioLocation);
        chart.Identifier.AudioCRC32 = audioContainer.Crc32;

        ChartviewCore.ChartManager.LoadedAudios[audioContainer.Crc32] = audioContainer.Bytes;
        Console.WriteLine($"[chartview] Loaded audio from {chart.AudioLocation} into memory");

        return chart;
    }
    
    public static Chart CreateChartFromStingArray(Interpreter mode, string[] file, uint chartCrc32)
    {
        return CreateChartBase(mode, file, "", chartCrc32);
    }

    private static void OsuChartInterpreter(Chart chart, 
        ref string interpreter, 
        string line, 
        string file, 
        params uint[] crc32s)
    {
        if (line.StartsWith('['))
        {
            interpreter = line.Trim();
            return;
        }

        var data = line.Split(":", 2);

        switch (interpreter)
        {
            case "[General]":
                if (data[0] == "AudioFilename")
                {
                    if (crc32s.Length > 0 && file.Length == 0)
                    {
                        chart.AudioLocation = data[1].Trim();
                        return;
                    }

                    var path = Path.GetFullPath(file).Replace("/", "\\");
                    var spath = path.Split("\\");
                    chart.AudioLocation = $"{string.Join("\\", spath[..^1])}\\{data[1].Trim()}";

                    if (!File.Exists(chart.AudioLocation)) Console.WriteLine("[chartview] Audio file missing!");
                }
                
                break;
            case "[Metadata]":
                switch (data[0]) {
                    case "Title":
                        chart.Metadata.Title = data[1].Trim();
                        break;
                    case "TitleUnicode":
                        chart.Metadata.UnicodeTitle = data[1].Trim();
                        break;
                    case "Artist":
                        chart.Metadata.Artist = data[1].Trim();
                        break;
                    case "ArtistUnicode":
                        chart.Metadata.UnicodeArtist = data[1].Trim();
                        break;
                    case "Creator":
                        chart.Metadata.ChartCreator = data[1].Trim();
                        break;
                    case "Version":
                        chart.Metadata.Difficulty = data[1].Trim();
                        break;
                }
                
                break;
            case "[Difficulty]":
                if (data[0] == "CircleSize") chart.Lanes = int.Parse(data[1].Trim());
                break;
            case "[TimingPoints]":
                var timingData = line.Split(",");
                var millisPerBeat = NumberUtil.ParseDouble(timingData[1].Trim());

                if (millisPerBeat < 0.0) return;

                chart.TimingPoints.Add(new TimingPoint
                {
                    Offset = NumberUtil.ParseDouble(timingData[0]),
                    BPM = Math.Round(60.0 / millisPerBeat * 1000.0, 3),
                    MillisPerBeat = millisPerBeat,
                    SimfileBeatOffset = 0
                });
                break;
            case "[HitObjects]":
                var hitData = data[0].Split(",");
                var secondaryHitData = hitData[^1].Split(":");
                var isSingle = int.Parse(hitData[3]) == 1;

                var offset = new Offset
                {
                    Start = NumberUtil.ParseDouble(hitData[2]),
                    End = -1
                };
                
                if (!isSingle) offset.End = NumberUtil.ParseDouble(secondaryHitData[0]);

                var currentTimingPoint = chart.GetCorrectTimingPoint((int)offset.Start);

                chart.HitObjects.Add(new HitObject
                {
                    Offset = offset,
                    LastLocalOffset = offset.Start,
                    Lane = (int)(int.Parse(hitData[0]) / (128.0 / chart.Lanes * 4.0)),
                    Hold = !isSingle,
                    BeatRelation = BeatRelationHelper.ComputeSnapDivisor((offset.Start - currentTimingPoint.Offset) / currentTimingPoint.MillisPerBeat % 1)
                });
                
                break;
        }
    }
    

    private static void QuaverChartInterpreter(Chart chart, 
        ref string interpreter, 
        ref TimingPoint intermediaryTimingPoint, 
        ref HitObject intermediaryHitObject, 
        string line, 
        string file, 
        params uint[] crc32s)
    {
        var data = line.Split(":", 2);

        if (data[0] == "TimingPoints" || data[0] == "HitObjects" || data[0] == "SliderVelocities")
        {
            interpreter = data[0].Trim();
            return;
        }

        switch (interpreter)
        {
            case "GeneralRead":
                switch (data[0])
                {
                    case "AudioFile":
                        if (crc32s.Length > 0 && file.Length == 0)
                        {
                            chart.AudioLocation = data[1].Trim();
                            return;
                        }

                        var path = Path.GetFullPath(file).Replace("/", "\\");
                        var spath = path.Split("\\");
                        chart.AudioLocation = $"{string.Join("\\", spath[..^1])}\\{data[1].Trim()}";

                        if (!File.Exists(chart.AudioLocation)) Console.WriteLine("[chartview] Audio file missing!");
                        break;
                    case "Mode":
                        chart.Lanes = data[1].Trim() switch
                        {
                            "Keys4" => 4,
                            "Keys7" => 7,
                            _ => chart.Lanes
                        };
                        break;
                    case "Title":
                        chart.Metadata.Title = data[1].Trim();
                        chart.Metadata.UnicodeTitle = data[1].Trim();
                        break;
                    case "Artist":
                        chart.Metadata.Artist = data[1].Trim();
                        chart.Metadata.UnicodeArtist = data[1].Trim();
                        break;
                    case "Creator":
                        chart.Metadata.ChartCreator = data[1].Trim();
                        break;
                    case "DifficultyName":
                        chart.Metadata.Difficulty = data[1].Trim();
                        break;
                }
                
                break;
            case "TimingPoints":
                var tDataType = data[0].Replace("-", "").Trim();
                var tValue = data[1].Trim();

                if (data[0].StartsWith('-'))
                {
                    switch (tDataType) 
                    {
                        case "Bpm":
                            chart.TimingPoints.Add(new TimingPoint
                            {
                                Offset = 0.0,
                                BPM = NumberUtil.ParseDouble(tValue),
                                MillisPerBeat = (60.0 / NumberUtil.ParseDouble(tValue)) * 1000.0,
                                SimfileBeatOffset = 0
                            });
                            break;
                        case "StartTime":
                            intermediaryTimingPoint = new TimingPoint
                            {
                                Offset = NumberUtil.ParseDouble(tValue),
                                BPM = 0,
                                MillisPerBeat = 0,
                                SimfileBeatOffset = 0
                            };
                            break;
                    }
                    
                    return;
                }

                if (tDataType == "Bpm" && intermediaryTimingPoint.Offset != 0)
                {
                    intermediaryTimingPoint.BPM = NumberUtil.ParseDouble(tValue);
                    intermediaryTimingPoint.MillisPerBeat = (60.0 / NumberUtil.ParseDouble(tValue)) * 1000.0;
                    chart.TimingPoints.Add(intermediaryTimingPoint);
                    return;
                }

                Console.WriteLine($"[chartview] Invalid datatype '{tDataType}' for quaver import TimingPoint interpeter!");
                break;
            case "HitObjects":
                var hDataType = data[0].Replace("-", "").Trim();
                var hValue = data[1].Trim();
                
                if (data[0].StartsWith('-')) {
                    if (intermediaryHitObject.Lane != -1)
                    {
                        chart.HitObjects.Add(intermediaryHitObject);
                    }

                    if (hDataType == "StartTime")
                    {
                        var offsetParsed = NumberUtil.ParseDouble(hValue);
                        var currentTimingPoint = chart.GetCorrectTimingPoint((int)offsetParsed);
                        intermediaryHitObject = new HitObject
                        {
                            Offset = new Offset
                            {
                                Start = offsetParsed,
                                End = -1
                            },
                            LastLocalOffset = offsetParsed,
                            BeatRelation = BeatRelationHelper.ComputeSnapDivisor((offsetParsed - Math.Floor(currentTimingPoint.Offset)) / currentTimingPoint.MillisPerBeat % 1),
                            Lane = 0,
                            Hold = false
                        };
                    }

                    return;
                }

                switch (hDataType) {
                    case "Lane":
                        intermediaryHitObject.Lane = int.Parse(hValue) - 1;
                        break;
                    case "EndTime":
                        intermediaryHitObject.Offset.End = NumberUtil.ParseDouble(hValue);
                        intermediaryHitObject.Hold = true;
                        break;
                }
                
                break;
        }
    }

    private static void SimfileChartInterpreter(Chart chart,
        ref string interpreter,
        string line,
        string file,
        params uint[] crc32s)
    {
        var data = line.Split(":", 2);

        if (data[0] == "#BPMS" || data[0] == "#NOTES")
        {
            interpreter = data[0].Trim();
            if (data[0].Trim() == "#NOTES") return;
        }

        if (interpreter == "#NOTES" && !line.StartsWith("0000"))
        {
            return;
        }

        var cleanVal = data[^1].Trim();

        if (cleanVal.EndsWith(';'))
        {
            cleanVal = cleanVal[..^1];
        }
        
        switch (interpreter) 
        {
            case "GeneralRead":
                switch (data[0].Trim())
                {
                    case "#MUSIC":
                        if (crc32s.Length > 0 && file.Length == 0)
                        {
                            chart.AudioLocation = cleanVal;
                            return;
                        }

                        var path = Path.GetFullPath(file).Replace("/", "\\");
                        var spath = path.Split("\\");
                        chart.AudioLocation = $"{string.Join("\\", spath[..^1])}\\{cleanVal}";

                        if (!File.Exists(chart.AudioLocation)) Console.WriteLine("[chartview] Audio file missing!");
                        break;
                    case "#TITLE":
                        chart.Metadata.Title = cleanVal;
                        chart.Metadata.UnicodeTitle = cleanVal;
                        break;
                    case "#ARTIST":
                        chart.Metadata.Artist = cleanVal;
                        chart.Metadata.UnicodeArtist = cleanVal;
                        break;
                    case "#OFFSET":
                        chart.SimfileOffset = NumberUtil.ParseDouble(cleanVal) * 1000.0;
                        break;
                }
                
                break;
            case "#BPMS":
                if (cleanVal.Length == 0 || cleanVal == ";") { // last correct TimingPoint was already read
                    interpreter = "GeneralRead";
                    return;
                }

                var bpms = cleanVal.Split(",");
                
                foreach (var bpm in bpms)
                {
                    if (bpm.Length == 0) continue;

                    var offset = bpm.Split("=");
                    var cleanBeatOffset = offset[0].Replace(",", "");
                    var millisPerBeat = (60.0 / NumberUtil.ParseDouble(offset[1])) * 1000.0;

                    if (chart.TimingPoints.Count == 0)
                    {
                        chart.TimingPoints.Add(new TimingPoint
                        {
                            Offset = millisPerBeat * 4 - chart.SimfileOffset,
                            BPM = NumberUtil.ParseDouble(offset[1]),
                            MillisPerBeat = millisPerBeat,
                            SimfileBeatOffset = NumberUtil.ParseDouble(cleanBeatOffset)
                        });
                    }
                    else
                    {
                        var currentTimingPoint = chart.TimingPoints[^1];
                        var realBeatOffset = NumberUtil.ParseDouble(cleanBeatOffset) - 4;
                        var realOffset = currentTimingPoint.Offset + currentTimingPoint.MillisPerBeat * (realBeatOffset - currentTimingPoint.SimfileBeatOffset);
                        
                        chart.TimingPoints.Add(new TimingPoint
                        {
                            Offset = realOffset,
                            BPM = NumberUtil.ParseDouble(offset[1]),
                            MillisPerBeat = millisPerBeat,
                            SimfileBeatOffset = realBeatOffset
                        });
                    }
                }
                
                if (cleanVal.EndsWith(';'))interpreter = "GeneralRead";
                break;
        }
    }

    private static void SimfileHitObjectProcessor(Chart chart, string[] lines)
    {
        // PREPROCESS
        var data = new SimfileNoteData
        {
            Creator = "",
            Difficulty = -1,
            DifficultyName = "",
            Notes = []
        };

        var shouldRead = false;
        var count = 0;

        var current = new List<string>();
        
        foreach (var line in lines)
        {
            if (!shouldRead)
            {
                if (line.StartsWith("#NOTES")) shouldRead = true;
                continue;
            }

            if (count <= 4)
            {
                var lineData = line.Trim().Split(":")[0];
                
                switch (count) 
                {
                    case 1:
                        data.Creator = lineData;
                        break;
                    case 2:
                        data.DifficultyName = lineData;
                        break;
                    case 3:
                        data.Difficulty = int.Parse(lineData);
                        break;
                }

                count++;
                continue;
            }
            
            if (line.StartsWith(','))
            {
                data.Notes.Add(current);
                current = [];
                continue;
            }
            
            current.Add(line.Trim());
        }
        
        // POSTPROCESS
        chart.Metadata.ChartCreator = data.Creator;
        chart.Metadata.Difficulty = data.DifficultyName;

        var currentOffset = -chart.SimfileOffset;
        
        foreach (var section in data.Notes)
        {
            var divisor = section.Count;

            foreach (var lane in section)
            {
                var notes = lane.ToCharArray();
                var currentTimingPoint = chart.GetCorrectTimingPoint((int)currentOffset);

                for (var laneIndex = 0; laneIndex < notes.Length; laneIndex++)
                {
                    var note = notes[laneIndex].ToString();
                    if (note == ";") continue; // I don't know why this is bugged here but not on go
                    var val = int.Parse(note);
                    switch (val)
                    {
                        case 1:
                        case 2:
                        case 4: 
                            var hold = val is 2 or 4;
                            chart.HitObjects.Add(new HitObject
                            {
                                Offset = new Offset
                                {
                                    Start = currentOffset,
                                    End = -1
                                },
                                LastLocalOffset = currentOffset,
                                Lane = laneIndex,
                                Hold = hold,
                                BeatRelation = BeatRelationHelper.ComputeSnapDivisor((currentOffset - Math.Floor(currentTimingPoint.Offset)) / currentTimingPoint.MillisPerBeat % 1)
                            });
                            break;
                        case 3:
                            var startIndex = chart.HitObjects.Count - 1;
                            while (startIndex > 0) {
                                var hitObject = chart.HitObjects[startIndex];
                                if (hitObject.Lane == laneIndex && hitObject.Hold)
                                {
                                    var detected = chart.HitObjects[startIndex];
                                    detected.Offset.End = currentOffset;
                                    break;
                                }
                                startIndex--;
                            }
                            break;
                    }
                }

                currentOffset += (currentTimingPoint.MillisPerBeat * 4) / divisor;
            }
        }
        
        Console.WriteLine($"[chartview] Simfile processed with {chart.HitObjects.Count} hit objects");
    }
    
    private static Chart CreateChartBase(Interpreter mode, string[] lines, string file, params uint[] crc32s)
    {
        var chart = new Chart();

        if (crc32s.Length > 0)
        {
            chart.Identifier.CRC32 = crc32s[0];
            if (crc32s.Length > 1) chart.Identifier.AudioCRC32 = crc32s[1];
        }
        
        var intermediaryHitObject = new HitObject
        {
            Offset = new Offset
            {
                Start = 0,
                End = 0
            },
            LastLocalOffset = 0,
            Hold = false,
            Lane = -1,
            BeatRelation = SnapDivisor.UNKNOWN
        };
        
        var intermediaryTimingPoint = new TimingPoint
        {
            Offset = 0,
            BPM = 0,
            MillisPerBeat = 0,
            SimfileBeatOffset = 0
        };

        var interpreterStatus = "";
        if (mode != Interpreter.Osu) interpreterStatus = "GeneralRead";
        
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0) continue;

            switch (mode)
            {
                case Interpreter.Osu:
                    OsuChartInterpreter(chart, ref interpreterStatus, line, file, crc32s);
                    break;
                case Interpreter.Quaver:
                    QuaverChartInterpreter(chart, ref interpreterStatus, ref intermediaryTimingPoint, ref intermediaryHitObject, line, file, crc32s);
                    break;
                case Interpreter.Sm:
                    SimfileChartInterpreter(chart, ref interpreterStatus, line, file, crc32s);
                    continue;
                case Interpreter.None:
                case Interpreter.Scc:
                default:
                    Console.WriteLine("[chartview] file interpreter for this chart type has not yet been implemented! (line ignored)");
                    break;
            }
        }
        
        // Simfile Note Processing
        if (mode is Interpreter.Sm or Interpreter.Scc)
        {
            SimfileHitObjectProcessor(chart, lines);
            chart.Lanes = 4;
        }

        chart.Metadata.DisplayString = $"{chart.Metadata.UnicodeArtist} - {chart.Metadata.UnicodeTitle} [{chart.Metadata.Difficulty}] ({chart.Lanes}K) by {chart.Metadata.ChartCreator}";
        chart.Metadata.Format = mode;

        Console.WriteLine($"[chartview] Loaded chart: {chart.Metadata.DisplayString}");
        Console.WriteLine($"[chartview] BPM: {chart.TimingPoints[0].BPM:0.##} ({chart.TimingPoints[0].MillisPerBeat:0.################} ms per beat)\n");

        var printAudioLocation = crc32s.Length > 0 ? "[using crc32 as audio key]" : chart.AudioLocation;
        Console.WriteLine($"[chartview] Audio location: {printAudioLocation}");

        Console.WriteLine($"[chartview] Timing points: {chart.TimingPoints.Count}");
        Console.WriteLine($"[chartview] Hit objects: {chart.HitObjects.Count}");

        return chart;
    }
}