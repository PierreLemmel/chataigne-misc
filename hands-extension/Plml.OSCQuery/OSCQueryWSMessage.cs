using System.Text.Json.Serialization;

namespace Plml.OSCQuery;

internal class OSCQueryWSMessage
{
    [JsonPropertyName("COMMAND")]
    public required string Command { get; set; }

    [JsonPropertyName("DATA")]
    public required string Data { get; set; }
}
