namespace IchniOnline.Server.Models.Dto;

public record BeatmapNoteDto(
    Guid NoteGuid, 
    SaveDataType NoteType,
    double JudgeTime);

public class BeatmapDto
{
    public Guid CollectionId { get; set; }
    
    public string SongName { get; set; } = null!;
    
    public string IllustrateUrl { get; set; } = null!;

    public string Illustrator { get; set; } = null!;
    
    public string Composer { get; set; } = null!;

    public List<BeatmapDivisionDto> Divisions = new();
}

/// <summary>
/// 谱面差分
/// </summary>
public class BeatmapDivisionDto
{
    public Guid BeatmapId { get; set; }
    
    public string LevelDesigner { get; set; } = null!;
    
    public string Difficulty { get; set; } = null!;
    
    public string LevelColor { get; set; } = null!;
}