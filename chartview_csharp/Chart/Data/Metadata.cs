using chartview_csharp.Chart.Enum;

namespace chartview_csharp.Chart.Data;

public record Metadata
{
    public required string UnicodeTitle { get; set; }
    public required string UnicodeArtist { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string ChartCreator { get; set; }
    public required string Difficulty { get; set; }
    public required string DisplayString { get; set; }
    public required Interpreter Format { get; set; }

    public static bool IsDefaultMetadata(Metadata metadata)
    {
        return metadata.UnicodeTitle.Length == 0 &&
               metadata.UnicodeArtist.Length == 0 &&
               metadata.Title.Length == 0 &&
               metadata.Artist.Length == 0 &&
               metadata.ChartCreator.Length == 0 &&
               metadata.Difficulty.Length == 0 &&
               metadata.DisplayString.Length == 0 &&
               metadata.Format == Interpreter.None;
    }
}