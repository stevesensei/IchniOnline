using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;
using SqlSugar;

namespace IchniOnline.Server.Entities;

[SugarTable("beatmap")]
public class BeatmapDb
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    public Guid BeatmapId { get; set; }
    
    /// <summary>
    /// 同一首曲子的不同谱面属于同一合集
    /// </summary>
    [SugarColumn(ColumnName = "collection_id")]
    public Guid CollectionId { get; set; }
    
    /// <summary>
    /// 曲名
    /// </summary>
    [SugarColumn(ColumnName = "song_name")]
    public string SongName { get; set; } = null!;
    
    /// <summary>
    /// 曲绘地址
    /// </summary>
    [SugarColumn(ColumnName = "illustrate_url")]
    public string IllustrateUrl { get; set; } = null!;
    
    /// <summary>
    /// 曲绘作者
    /// </summary>
    [SugarColumn(ColumnName = "illustrator")]
    public string Illustrator { get; set; } = null!;
    
    /// <summary>
    /// 曲师
    /// </summary>
    [SugarColumn(ColumnName = "composer")]
    public string Composer { get; set; } = null!;
    
    /// <summary>
    /// 谱师
    /// </summary>
    [SugarColumn(ColumnName = "level_designer")]
    public string LevelDesigner { get; set; } = null!;
    
    /// <summary>
    /// 难度名称
    /// </summary>
    [SugarColumn(ColumnName = "difficulty")]
    public string Difficulty { get; set; } = null!;
    
    /// <summary>
    /// 着色，Hex
    /// </summary>
    [SugarColumn(ColumnName = "level_color")]
    public string LevelColor { get; set; } = null!;
    
    /// <summary>
    /// 物量
    /// </summary>
    [SugarColumn(ColumnName = "notes",IsJson = true)]
    public List<BeatmapNoteDto> Notes { get; set; } = null!;

    /// <summary>
    /// 标记一个谱面的版本，初始值为1，每次修改谱面时递增
    /// <remarks>这个设计不一定需要，但是涉及到排行榜的计算</remarks>
    /// </summary>
    [SugarColumn(ColumnName = "version")]
    public long Version { get; set; } = 1;
    
    [SugarColumn(ColumnName = "status")]
    public BeatmapStatus Status { get; set; } = BeatmapStatus.Private;
    
    /// <summary>
    /// UTC+0
    /// </summary>
    [SugarColumn(ColumnName = "release_time")]
    public long ScheduledReleaseTime { get; set; } = 0;
}