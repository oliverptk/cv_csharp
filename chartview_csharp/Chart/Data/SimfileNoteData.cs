namespace chartview_csharp.Chart.Data;

public record SimfileNoteData
{
    public required string Creator { get; set; }
    public required int Difficulty { get; set; }
    public required string DifficultyName { get; set; }
    public required List<List<string>> Notes { get; set; }
}