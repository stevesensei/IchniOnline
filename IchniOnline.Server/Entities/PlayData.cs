namespace IchniOnline.Server.Entities;

public class PlayData
{
    public Guid PlayDataId { get; set; }
    
    public Guid BeatmapId { get; set; }
    
    public Guid UserId { get; set; }
    
    public long PerfectCount { get; set; }
    
    public long GoodCount { get; set; }
    
    public long BadCount { get; set; }
    
    public long MissCount { get; set; }
    
    public long MaxCombo {get; set;}
    
    public long AchieveTime { get; set; }

    public bool IsValid { get; set; }

    public BeatmapDb Beatmap { get; set; } = null!;
    
    public GameUser User { get; set; } = null!;
}