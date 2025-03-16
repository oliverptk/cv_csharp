using System.Text;
using System.Text.RegularExpressions;
using chartview_csharp.Util;

namespace chartview_csharp.Parser;

public partial class FastIniParser
{
    public record Section(Dictionary<string, string> Data)
    {
        public string GetString(string key)
        {
            return Data[key];
        }
        
        public int GetInt(string key)
        {
            return int.Parse(Data[key]);
        }
        
        public double GetDouble(string key)
        {
            return NumberUtil.ParseDouble(Data[key]);
        }
        
        public bool GetBool(string key)
        {
            return bool.Parse(Data[key]);
        }
    }
    
    [GeneratedRegex(@"\[.*\]")]
    private static partial Regex SectionHead();
    
    private readonly Dictionary<string, Dictionary<string, string>> data = new();

    public void Init(byte[] bytes)
    {
        var lines = Encoding.UTF8.GetString(bytes).Split("\n");

        var section = "UNKNOWN";
        
        foreach (var line in lines)
        {
            var cleanLine = line.Trim();

            if (cleanLine.Length == 0 || cleanLine.StartsWith("--")) continue;

            if (SectionHead().Match(cleanLine).Success)
            {
                section = cleanLine.Replace("[", "").Replace("]", "").Trim();
                data[section] = new Dictionary<string, string>();
                continue;
            }

            var value = cleanLine.Split("--")[0].Split("=");
            data[section][value[0]] = value[1].Trim();
        }
    }

    public Section GetSection(string section)
    {
        return new Section(data[section]);
    }
}