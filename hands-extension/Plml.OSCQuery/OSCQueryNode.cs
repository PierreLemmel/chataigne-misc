using System.Text.Json.Serialization;
using Vizcon.OSC;

namespace Plml.OSCQuery;

public static class OSCQueryNodes
{
    public static OSCQueryNode CreateIntNode(string path, string name, int value, Action<int>? onValueChanged = null, int? min = null, int? max = null) => new OSCQueryNode
    {
        FullPath = path,
        Name = name,
        Type = OSCQueryTypes.Tags.Integer,
        Value = [
            MoreMaths.Clamp(value, min ?? int.MinValue, max ?? int.MaxValue)
        ],
        Range = (min is not null || max is not null) ? [new OSCRange { Min = min, Max = max }] : null,
        OnValueChanged = onValueChanged is not null ? (object[] values) =>
        {
            if (values.Length > 0 && values[0] is int i)
                onValueChanged.Invoke(i);
        } : null
    };

    public static OSCQueryNode CreateFloatNode(string path, string name, float value, Action<float>? onValueChanged = null, float? min = null, float? max = null) => new OSCQueryNode
    {
        FullPath = path,
        Name = name,
        Type = OSCQueryTypes.Tags.Float,
        Value = [
            MoreMaths.Clamp(value, min ?? float.MinValue, max ?? float.MaxValue)
        ],
        Range = (min is not null || max is not null) ? [new OSCRange { Min = min, Max = max }] : null,
        OnValueChanged = onValueChanged is not null ? (object[] values) =>
        {
            if (values.Length > 0 && values[0] is float flt)
                onValueChanged.Invoke(flt);
        } : null
    };

    public static OSCQueryNode CreateStringNode(string path, string name, string value, Action<string>? onValueChanged = null, string[]? enumValues = null) => new OSCQueryNode
    {
        FullPath = path,
        Name = name,
        Type = OSCQueryTypes.Tags.String,
        Value = [value],
        Range = enumValues != null ? [new OSCRange { Values = enumValues }] : null,
        OnValueChanged = onValueChanged is not null ? (object[] values) =>
        {
            if (values.Length > 0 && values[0] is string str)
                onValueChanged.Invoke(str);
        } : null
    };

    public static OSCQueryNode CreateBooleanNode(string path, string name, bool value, Action<bool>? onValueChanged = null) => new OSCQueryNode
    {
        FullPath = path,
        Name = name,
        Type = OSCQueryTypes.Tags.Boolean,
        Value = [value],
        OnValueChanged = onValueChanged is not null ? (object[] values) =>
        {
            if (values.Length > 0 && values[0] is bool b)
                onValueChanged.Invoke(b);
        } : null
    };

    public static OSCQueryNode CreateColorNode(string path, string name, RGBA value, Action<RGBA>? onValueChanged = null) => new OSCQueryNode
    {
        FullPath = path,
        Name = name,
        Type = OSCQueryTypes.Tags.Color,
        Value = [
            OSCQueryColor.RGBAToString(value)
        ],
        OnValueChanged = onValueChanged is not null ? (object[] values) =>
        {
            if (values.Length > 0 && values[0] is string str)
            {
                RGBA col = OSCQueryColor.StringToRGBA(str);
                onValueChanged.Invoke(col);
            }
        } : null
    };
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

    [JsonIgnore]
    public Action<object[]>? OnValueChanged { get; set; }
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