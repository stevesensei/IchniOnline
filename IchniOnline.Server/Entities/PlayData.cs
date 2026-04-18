using SqlSugar;

namespace IchniOnline.Server.Entities;

[SugarTable("play_data")]
public class PlayData
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    public Guid PlayDataId { get; set; }
    
    [SugarColumn(ColumnName = "beatmap_id")]
    public Guid BeatmapId { get; set; }
    
    [SugarColumn(ColumnName = "user_id")]
    public Guid UserId { get; set; }
    
    [SugarColumn(ColumnName = "perfect_count")]
    public long PerfectCount { get; set; }
    
    [SugarColumn(ColumnName = "good_count")]
    public long GoodCount { get; set; }
    
    [SugarColumn(ColumnName = "bad_count")]
    public long BadCount { get; set; }
    
    [SugarColumn(ColumnName = "miss_count")]
    public long MissCount { get; set; }
    
    [SugarColumn(ColumnName = "max_combo")]
    public long MaxCombo {get; set;}
    
    [SugarColumn(ColumnName = "time")]
    public long AchieveTime { get; set; }

    [SugarColumn(ColumnName = "is_valid")]
    public bool IsValid { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(BeatmapId))]
    public BeatmapDb Beatmap { get; set; } = null!;
    
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public GameUser User { get; set; } = null!;
}