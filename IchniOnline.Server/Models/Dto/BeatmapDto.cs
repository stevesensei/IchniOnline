using System.Text.Json.Serialization;

namespace IchniOnline.Server.Models.Dto;

public record BeatmapNoteDto(
    Guid NoteGuid, 
    SaveDataType NoteType,
    double JudgeTime);

public class BeatmapDto
{
    [JsonPropertyName("id")]
    public Guid CollectionId { get; set; }
    [JsonPropertyName("name")]
    public string SongName { get; set; } = null!;
    [JsonPropertyName("url")]
    public string IllustrateUrl { get; set; } = null!;
    [JsonPropertyName("illustrator")]
    public string Illustrator { get; set; } = null!;
    [JsonPropertyName("composer")]
    public string Composer { get; set; } = null!;
    [JsonPropertyName("difficulties")]
    public List<BeatmapDivisionDto> Divisions = new();
}

/// <summary>
/// 谱面差分
/// </summary>
public class BeatmapDivisionDto
{
    [JsonPropertyName("beatmap_id")]
    public Guid Id { get; set; }
    public Guid BeatmapId { get; set; }
    [JsonPropertyName("designer")]
    public string LevelDesigner { get; set; } = null!;
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = null!;
    [JsonPropertyName("color")]
    public string LevelColor { get; set; } = null!;
}

public class BeatmapNoteChartComponent
{
    [JsonPropertyName("from")]
    public double From { get; set; }
    
    [JsonPropertyName("to")]
    public double To { get; set; }
    
    [JsonPropertyName("count")]
    public long Count { get; set; }
}