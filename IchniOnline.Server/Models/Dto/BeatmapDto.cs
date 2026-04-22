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
    public List<BeatmapDivisionDto> Divisions { get; set; }= null!;
}

public class BeatmapListItemDto
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
    public List<BeatmapDivisionDto> Divisions { get; set; } = new();
}

public class BeatmapPagedDto
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
    [JsonPropertyName("items")]
    public List<BeatmapListItemDto> Items { get; set; } = new();
}

/// <summary>
/// 谱面差分
/// </summary>
[Serializable]
public class BeatmapDivisionDto
{
    [JsonPropertyName("beatmap_id")]
    public Guid BeatmapId { get; set; }
    [JsonPropertyName("designer")]
    public string LevelDesigner { get; set; } = null!;
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = null!;
    [JsonPropertyName("color")]
    public string LevelColor { get; set; } = null!;
}

[Serializable]
public class BeatmapNoteChartComponent
{
    [JsonPropertyName("from")]
    public double From { get; set; }
    
    [JsonPropertyName("to")]
    public double To { get; set; }
    
    [JsonPropertyName("count")]
    public long Count { get; set; }
}