using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plml.OSCQuery;

public class OSCQueryNode
{
    [JsonPropertyName("DESCRIPTION")]
    public string? Description { get; set; }

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

    [JsonPropertyName("WS_PORT")]
    public required int WSPort { get; set; }

    [JsonPropertyName("METADATA")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public class OSCRange
{
    [JsonPropertyName("MIN")]
    public float? Min { get; set; }
    
    [JsonPropertyName("MAX")]
    public float? Max { get; set; }

    [JsonPropertyName("VALUES")]
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
        public static string Integer = "i";
        public static string Float = "f";
        public static string String = "s";
        public static string Color = "r";
        public static string Boolean = "T";
    }
}