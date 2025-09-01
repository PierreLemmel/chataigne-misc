using System.Text.Json.Serialization;
using Vizcon.OSC;

namespace Plml.OSCQuery;

public static class OSCQueryNodes
{
    public static OSCQueryNode CreateIntNode(string path, string name, int value, int? min = null, int? max = null)
    {
        return new OSCQueryNode
        {
            FullPath = path,
            Name = name,
            Type = OSCQueryTypes.Tags.Integer,
            Value = [value],
            Range = (min is not null || max is not null) ? [new OSCRange { Min = min, Max = max }] : null
        };
    }

    public static OSCQueryNode CreateFloatNode(string path, string name, float value, float? min = null, float? max = null)
    {
        float clampedValue = MoreMaths.Clamp(value, min ?? float.MinValue, max ?? float.MaxValue);

        return new OSCQueryNode
        {
            FullPath = path,
            Name = name,
            Type = OSCQueryTypes.Tags.Float,
            Value = [value],
            Range = (min is not null || max is not null) ? [new OSCRange { Min = min, Max = max }] : null
        };
    }

    public static OSCQueryNode CreateStringNode(string path, string name, string value, string[]? enumValues = null)
    {
        return new OSCQueryNode
        {
            FullPath = path,
            Name = name,
            Type = OSCQueryTypes.Tags.String,
            Value = [value],
            Range = enumValues != null ? [new OSCRange { Values = enumValues }] : null
        };
    }

    public static OSCQueryNode CreateBooleanNode(string path, string name, bool value)
    {
        return new OSCQueryNode
        {
            FullPath = path,
            Name = name,
            Type = OSCQueryTypes.Tags.Boolean,
            Value = [value]
        };
    }

    public static OSCQueryNode CreateColorNode(string path, string name, RGBA value)
    {
        return new OSCQueryNode
        {
            FullPath = path,
            Name = name,
            Type = OSCQueryTypes.Tags.Color,
            Value = [
                OSCQueryColor.RGBAToString(value)
            ]
        };
    }
}

public class OSCQueryNode
{
    [JsonPropertyName("DESCRIPTION")]
    public required string Name { get; set; }

    [JsonPropertyName("FULL_PATH")]
    public required string FullPath { get; set; }

    [JsonPropertyName("TYPE")]
    public required string Type { get; set; }

    [JsonPropertyName("ACCESS")]
    public OSCQueryAccess? Access { get; set; }

    [JsonPropertyName("CONTENTS")]
    public Dictionary<string, OSCQueryNode>? Contents { get; set; }

    [JsonPropertyName("VALUE")]
    public object[]? Value { get; set; }

    [JsonPropertyName("RANGE")]
    public OSCRange[]? Range { get; set; }

    [JsonIgnore]
    public bool Listen { get; set; } = false;
}

public class OSCQueryHostInfo
{
    [JsonPropertyName("NAME")]
    public required string Name { get; set; }

    [JsonPropertyName("EXTENSIONS")]
    public required Dictionary<string, bool> Extensions { get; set; }

    [JsonPropertyName("OSC_PORT")]
    public required int OSCPort { get; set; }

    [JsonPropertyName("OSC_TRANSPORT")]
    public string OSCTransport { get; set; } = "UDP";

    [JsonPropertyName("METADATA")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public class OSCRange
{
    [JsonPropertyName("MIN")]
    public float? Min { get; set; }
    
    [JsonPropertyName("MAX")]
    public float? Max { get; set; }

    [JsonPropertyName("VALS")]
    public string[]? Values { get; set; }
}

public enum OSCQueryAccess
{
    NoValue = 0,
    Read = 1,
    Write = 2,
    ReadWrite = 3,
}

public static class OSCQueryTypes
{
    public static string Container = "Container";

    public static class Tags
    {
        public const string Integer = "i";
        public const string Float = "f";
        public const string String = "s";
        public const string Color = "r";
        public const string Boolean = "T";
    }
}