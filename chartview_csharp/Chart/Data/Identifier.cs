namespace chartview_csharp.Chart.Data;

public record Identifier
{
    public required uint CRC32 { get; set; }
    public required uint AudioCRC32 { get; set; }
}