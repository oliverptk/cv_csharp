using System.Numerics;
using chartview_csharp.Chart;
using chartview_csharp.Chart.Helper;
using chartview_csharp.Parser;
using chartview_csharp.UI;
using chartview_csharp.Util;
using Raylib_cs;

namespace chartview_csharp;

public class ChartviewCore
{
    private readonly List<string> _plainChartFileTypes = [".osu", ".qua", ".sm", ".scc"];
    private readonly List<string> _packedChartFileTypes = [".osz", ".osl", ".qp"];
    
    public static readonly ChartManager ChartManager = new();
    public static readonly Vector2 WindowSize = new(1560, 800);
    
    private readonly FastIniParser _configuration = new();
    private readonly string _defaultConfiguration = """
                                                    [Objects]
                                                    ObjectDensity=1.0 -- float32, Density divider for objects, higher = more dense, lower = less dense
                                                    ObjectSize=32.0 -- float32, Radius of objects in pixels
                                                
                                                    [Lane]
                                                    Upscroll=false -- bool, Does lane scroll upwards instead of downwards
                                                    VisualOffset=32 -- int32, How many pixels apart from the edge of the screen
                                                    MaxSnapLines=4 -- int32, Grid snapping precision (4 = 1/4, 8 = 1/8, etc.)
                                                
                                                    [Render]
                                                    UseMSAAx4=true -- bool, Enables smoother drawing, set to false on low end machines for a better performance, no significant difference on mid to high end machines
                                                
                                                    [Audio]
                                                    PlaybackSpeed=1.0 -- float32, Speed multiplier of chart and music playback (0.75x = HT, 1.5x = DT)
                                                        
                                                    """;
    
    private readonly ChartSelectorDropdown _chartSelectorDropdown = new();
    
    public static Font FontRoboto { get; private set; }

    private void InitializeConfiguration()
    {
        if (!File.Exists("./chartview.ini"))
        {
            Console.WriteLine("[chartview] Configuration file missing, writing default config...");
            File.WriteAllText("./chartview.ini", _defaultConfiguration);
        }
        
        _configuration.Init(FileUtil.ReadFileBytes("./chartview.ini"));

        ChartviewGlobals.HitObjectDensity = _configuration.GetSection("Objects").GetDouble("ObjectDensity");
        ChartviewGlobals.HitObjectSize = _configuration.GetSection("Objects").GetDouble("ObjectSize");
        
        ChartviewGlobals.IsUpscroll = _configuration.GetSection("Lane").GetBool("Upscroll");
        ChartviewGlobals.VisualOffset = _configuration.GetSection("Lane").GetInt("VisualOffset");
        ChartviewGlobals.BeatSnapDivisor = _configuration.GetSection("Lane").GetInt("MaxSnapLines");
        
        ChartviewGlobals.ShouldUseMsaa = _configuration.GetSection("Render").GetBool("UseMSAAx4");
        
        ChartviewGlobals.AudioPlaybackSpeed = (float)_configuration.GetSection("Audio").GetDouble("PlaybackSpeed");
    }
    
    public void Run()
    {
        InitializeConfiguration();
        
        if (ChartviewGlobals.ShouldUseMsaa)
        {
            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        }
        
        Raylib.InitWindow((int)WindowSize.X, (int)WindowSize.Y, "chartview (go) | no chart loaded...");
        Raylib.InitAudioDevice();

        FontRoboto = DrawUtil.LoadFontScaled("./assets/fonts/Roboto-SemiBold.ttf", 256);

        Rlgl.Viewport(0, 0, (int)WindowSize.X, (int)WindowSize.Y);
        Rlgl.Ortho(0, WindowSize.X, WindowSize.Y, 0, -1000, 1000);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsFileDropped())
            {
                var dropped = Raylib.GetDroppedFiles(); // method already unloads files afterward, unlike raylib-go

                foreach (var file in dropped)
                {
                    var suffix = "." + file.Split(".")[^1]; // last index (prefixing with "." for backwards compatability with go version)
                    
                    if (_plainChartFileTypes.Contains(suffix))
                    {
                        ChartFileHandler.HandleDroppedPlainChartFile(file);
                    }
                    
                    if (_packedChartFileTypes.Contains(suffix))
                    {
                        ChartFileHandler.HandleDroppedPackedChartFile(file);
                    }

                    if (file == dropped[^1] && ChartManager.LoadedCharts.Count > 0)
                    {
                        ChartManager.UpdateCurrentChartAudioAndMinimap();
                    }
                }
            }
            
            if (ChartManager.IsCurrentChartEmpty() || ChartManager.GetCurrentChart() == null || ChartManager.GetCurrentChartAudioRaw() == null)
            {
                Raylib.BeginDrawing();
                
                Raylib.ClearBackground(new Color(0x20, 0x20, 0x20, 0xff));

                var textSize = Raylib.MeasureTextEx(FontRoboto, "Drop chart file...", 20, 1);
                DrawUtil.DrawTextScaled(FontRoboto, "Drop chart file...", (WindowSize.X - textSize.X) / 2, (WindowSize.Y - textSize.Y) / 2, 20, Color.White);
                
                DrawUtil.DrawFrameCounter(20);
                
                Raylib.EndDrawing();
                continue;
            }

            var laneBounds = LaneMathHelper.CalculateHorizontalLaneBounds(ChartviewGlobals.HitObjectSize, ChartviewGlobals.LaneGap, ChartManager.GetCurrentChart()!.Lanes);

            if (ChartviewGlobals.IsScrollPastAudio()) Raylib.StopMusicStream(ChartManager.CurrentChartAudio);

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                if (Raylib.IsMusicStreamPlaying(ChartManager.CurrentChartAudio)) Raylib.PauseMusicStream(ChartManager.CurrentChartAudio);
                else
                {
                    Raylib.PlayMusicStream(ChartManager.CurrentChartAudio);
                    if (ChartviewGlobals.IsScrollPastEnd()) ChartviewGlobals.PlayfieldScrollMs = 0;
                }

                ChartviewGlobals.DisableScrolling = false;
                ChartManager.SyncLoadedMusic();
            }

            if (Raylib.GetMouseWheelMoveV().Y != 0)
            {
                double mouseY = Raylib.GetMouseWheelMoveV().Y;
                if (Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                {
                    if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
                    {
                        ChartviewGlobals.HitObjectSize += mouseY;
                        ChartviewGlobals.HitObjectSize = Math.Clamp(ChartviewGlobals.HitObjectSize, 4, 64);
                    }
                    
                    if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                    {
                        ChartviewGlobals.HitObjectDensity += ChartviewGlobals.HitObjectDensity <= 1 ? (ChartviewGlobals.HitObjectDensity == 1 && mouseY < 0 ? 0.1 : -(0.05 * mouseY)) : -(0.1 * mouseY);
                        ChartviewGlobals.HitObjectDensity = Math.Clamp(ChartviewGlobals.HitObjectDensity, 0.5, 16);
                    }
                }
                else
                {
                    var scrollTimingPoint = ChartManager.GetCurrentChart()!.GetCorrectTimingPoint((int)ChartviewGlobals.PlayfieldScrollMs);
                    ChartviewGlobals.PlayfieldScrollMs -= mouseY * (Raylib.IsMusicStreamPlaying(ChartManager.CurrentChartAudio) ? scrollTimingPoint.MillisPerBeat : scrollTimingPoint.MillisPerBeat / ChartviewGlobals.BeatSnapDivisor);
                    ChartManager.SyncLoadedMusic();
                }
            }
            
            Raylib.UpdateMusicStream(ChartManager.CurrentChartAudio);

            if (Raylib.IsMusicStreamPlaying(ChartManager.CurrentChartAudio) || !ChartviewGlobals.IsScrollPastStart() || (!ChartviewGlobals.IsScrollPastEnd() && ChartviewGlobals.IsScrollPastAudio() && !ChartviewGlobals.DisableScrolling))
            {
                ChartviewGlobals.PlayfieldScrollMs += 1000.0 * ChartviewGlobals.AudioPlaybackSpeed * Raylib.GetFrameTime();
                if (ChartviewGlobals.IsScrollPastEnd()) ChartviewGlobals.DisableScrolling = true;
                if (!ChartviewGlobals.IsScrollPastStart()) Raylib.StopMusicStream(ChartManager.CurrentChartAudio);
            }

            ChartviewGlobals.PlayfieldScrollMs = Math.Clamp(ChartviewGlobals.PlayfieldScrollMs, 0, ChartviewGlobals.MaxAudioLengthMs);
            
            Raylib.BeginDrawing();
            
            Raylib.ClearBackground(new Color(0x20, 0x20, 0x20, 0xff));
            
            DrawUtil.RaylibMatrixPushPop(() =>
            {
                if (ChartviewGlobals.IsUpscroll)
                {
                    Rlgl.Rotatef(180, 0, 0, 1);
                    Rlgl.Translatef(laneBounds.X - (WindowSize.X + ChartManager.GetCurrentChart()!.Lanes * 64), -WindowSize.Y, 0);
                }
                
                Raylib.DrawTexture(ChartManager.CurrentChartMinimap, (int)(laneBounds.X - ChartManager.GetCurrentChart()!.Lanes * 2.0f), 0, Color.White);
            });
            
            DrawUtil.RaylibMatrixPushPop(() =>
            {
                if (ChartviewGlobals.IsUpscroll)
                {
                    Rlgl.Rotatef(180, 0, 0, 1);
                    Rlgl.Translatef(-WindowSize.X, -WindowSize.Y, 0);
                }

                Rlgl.Translatef(0, -ChartviewGlobals.VisualOffset, 0);
                
                ChartRenderer.RenderLane(ChartManager.GetCurrentChart()!);
                ChartRenderer.RenderChart(ChartManager.GetCurrentChart()!);
            });

            _chartSelectorDropdown.Handle(FontRoboto, new Rectangle(WindowSize.X - 402, 2, 400, 20), 12, Color.White, new Color(0x20, 0x20, 0x20, 0xff));
            
            /*
            RayGUI.SetDefaultFontSize(16);

            var export = new Button((int)WindowSize.X - 202, (int)WindowSize.Y - 26, 200, 24, "Export chart... (NOP)")
            {
                Type = ButtonType.Custom,
                Event = delegate 
                {
                    Console.WriteLine("[chartview] Opening export dialog... (not implemented yet!)");
                }
            };
            
            container.Add("export_button", export);
            
            RayGUI.ActivateGui(container);
            */
            
            var infoTimingPoint = ChartManager.GetCurrentChart()!.GetCorrectTimingPoint((int)ChartviewGlobals.PlayfieldScrollMs);
            DrawUtil.DrawTextScaled(FontRoboto, $"Density: {ChartviewGlobals.HitObjectDensity:0.##} [LAlt], Size: {ChartviewGlobals.HitObjectSize:0} [LCtrl]", 2, WindowSize.Y-42, 20, Color.RayWhite);
            DrawUtil.DrawTextScaled(FontRoboto, $"BPM: {infoTimingPoint.BPM:0.##}, Progress: {ChartviewGlobals.PlayfieldScrollMs/ChartviewGlobals.MaxAudioLengthMs*100.0:0.##}%, Playback Speed: {ChartviewGlobals.AudioPlaybackSpeed:0.##}x", 2, WindowSize.Y-22, 20, Color.RayWhite);
            
            DrawUtil.DrawFrameCounter(20);

            Raylib.EndDrawing();
        }
    }
}