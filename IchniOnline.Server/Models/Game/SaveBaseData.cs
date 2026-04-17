using System.Text.Json.Serialization;

namespace IchniOnline.Server.Models.Game;

public abstract class SaveBaseData
{
    [JsonPropertyName("__type")]
    public abstract SaveDataType SaveDataType { get; set; } 
}