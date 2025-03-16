using System.Numerics;
using chartview_csharp.Util;
using Raylib_cs;

namespace chartview_csharp.UI;

public class ChartSelectorDropdown
{
    private bool IsOpen { get; set; }
    
    public void Handle(Font font, Rectangle rect, float size, Color foreground, Color background)
    {
        DrawUtil.RaylibMatrixPushPop(() =>
        {
            var current = ChartviewCore.ChartManager.GetCurrentChart()!;
            var currentText = $"{current.Metadata.Artist} - {current.Metadata.Title} [{current.Metadata.Difficulty}] ({current.Lanes}K)";
            var currentSize = Raylib.MeasureTextEx(font, currentText, size - 2f, 1);
            Raylib.DrawRectangleV(new Vector2(rect.X, rect.Y), new Vector2(rect.Width, rect.Height), background);
            DrawUtil.DrawTextScaled(font, currentText, rect.X + (rect.Width - currentSize.X) / 2f, rect.Y + (rect.Height - currentSize.Y) / 2f, size, foreground);
            
            if (IsOpen)
            {
                var yOffset = rect.Height;
                
                foreach (var chart in ChartviewCore.ChartManager.LoadedCharts)
                {
                    var openText = $"{chart.Metadata.Artist} - {chart.Metadata.Title} [{chart.Metadata.Difficulty}] ({chart.Lanes}K)";
                    var openSize = Raylib.MeasureTextEx(font, openText, size - 2f, 1);
                    Raylib.DrawRectangleV(new Vector2(rect.X, rect.Y + yOffset), new Vector2(rect.Width, rect.Height), chart == ChartviewCore.ChartManager.GetCurrentChart() ? ColorUtil.Darken(background) : background);
                    DrawUtil.DrawTextScaled(font, openText, rect.X + (rect.Width - openSize.X) / 2f, rect.Y + (rect.Height - openSize.Y) / 2f + yOffset, size, foreground);
                    yOffset += rect.Height;
                }
            }
        });

        var mousePosition = Raylib.GetMousePosition();
        
        if (ChartviewCore.ChartManager.LoadedCharts.Count > 1 && Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            if (mousePosition.X >= rect.X &&
                mousePosition.X <= rect.X + rect.Width &&
                mousePosition.Y >= rect.Y &&
                mousePosition.Y <= rect.Y + rect.Height)
            {
                IsOpen = !IsOpen;
                return;
            }

            if (!IsOpen) return;
            
            var yOffset = rect.Height;
            
            foreach (var chart in ChartviewCore.ChartManager.LoadedCharts)
            {
                if (mousePosition.X >= rect.X &&
                    mousePosition.X <= rect.X + rect.Width &&
                    mousePosition.Y >= rect.Y + yOffset &&
                    mousePosition.Y <= rect.Y + yOffset + rect.Height)
                {
                    ChartviewCore.ChartManager.LastChartIndex = ChartviewCore.ChartManager.CurrentChartIndex;
                    ChartviewCore.ChartManager.CurrentChartIndex = ChartviewCore.ChartManager.LoadedCharts.IndexOf(chart);
                    ChartviewCore.ChartManager.UpdateCurrentChartAudioAndMinimap();
                    IsOpen = false;
                    return;
                }
                    
                yOffset += rect.Height;
            }
                
            IsOpen = false;
        }
    }
}