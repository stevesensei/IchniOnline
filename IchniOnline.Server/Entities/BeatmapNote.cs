using IchniOnline.Server.Models;

namespace IchniOnline.Server.Entities;

public class BeatmapNote
{
    public Guid NoteGuid { get; set; }
    public SaveDataType NoteType { get; set; }
    public double JudgeTime { get; set; }
}
