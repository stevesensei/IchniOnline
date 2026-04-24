using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;

namespace IchniOnline.Server.Entities;

public class BeatmapDb
{
    public Guid BeatmapId { get; set; }
    
    /// <summary>
    /// 同一首曲子的不同谱面属于同一合集
    /// </summary>
    public Guid CollectionId { get; set; }
    
    /// <summary>
    /// 曲名
    /// </summary>
    public string SongName { get; set; } = null!;
    
    /// <summary>
    /// 曲绘地址
    /// </summary>
    public string IllustrateUrl { get; set; } = null!;
    
    /// <summary>
    /// 曲绘作者
    /// </summary>
    public string Illustrator { get; set; } = null!;
    
    /// <summary>
    /// 曲师
    /// </summary>
    public string Composer { get; set; } = null!;
    
    /// <summary>
    /// 谱师
    /// </summary>
    public string LevelDesigner { get; set; } = null!;
    
    /// <summary>
    /// 难度名称
    /// </summary>
    public string Difficulty { get; set; } = null!;
    
    /// <summary>
    /// 着色，Hex
    /// </summary>
    public string LevelColor { get; set; } = null!;
    
    /// <summary>
    /// 物量
    /// </summary>
    public List<BeatmapNoteDto> Notes { get; set; } = null!;

    /// <summary>
    /// 难度定数
    /// </summary>
    public double DifficultyRating { get; set; } = 10;

    /// <summary>
    /// 标记一个谱面的版本，初始值为1，每次修改谱面时递增
    /// <remarks>这个设计不一定需要，但是涉及到排行榜的计算</remarks>
    /// </summary>
    public long Version { get; set; } = 1;
    
    public BeatmapStatus Status { get; set; } = BeatmapStatus.Private;
    
    /// <summary>
    /// UTC+0
    /// </summary>
    public long ScheduledReleaseTime { get; set; } = 0;
}