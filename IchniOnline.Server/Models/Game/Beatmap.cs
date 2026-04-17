using System.Text.Json.Serialization;

namespace IchniOnline.Server.Models.Game;

[Serializable]
public sealed class Beatmap: SaveBaseData
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.BeatmapContainer;
    
    [JsonPropertyName("value")]
    public BeatmapWrapper Value { get; set; } = null!;
}

[Serializable]
public class BeatmapWrapper
{
    [JsonPropertyName("elementList")]
    public List<GameElement> Elements { get; set; } = null!;
}

[Serializable]
public class BeatmapRoot
{
    [JsonPropertyName("Beatmap")]
    public Beatmap Beatmap { get; set; } = null!;
}