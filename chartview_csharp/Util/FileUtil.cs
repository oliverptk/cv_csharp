using System.IO.Compression;
using System.IO.Hashing;
using chartview_csharp.Util.Data;

namespace chartview_csharp.Util;

public static class FileUtil
{
    private static uint HashCrc32(byte[] bytes)
    {
        return Crc32.HashToUInt32(bytes);
    }

    public static byte[] ReadFileBytes(string file)
    {
        return File.ReadAllBytes(file);
    }

    public static Crc32Container ReadFileBytesCrc32(string file)
    {
        var bytes = ReadFileBytes(file);
        return new Crc32Container(bytes, HashCrc32(bytes));
    }
    
    public static Crc32Container ReadCompressedFileBytesCrc32(ZipArchiveEntry file)
    {
        var open = file.Open();
        using var memoryStream = new MemoryStream();
        open.CopyTo(memoryStream);
        
        var bytes = memoryStream.ToArray();
        return new Crc32Container(bytes, HashCrc32(bytes));
    }
}