using System.Text.Json.Serialization;

namespace IchniOnline.Server.Models.Game;

public abstract class GameElement: SaveBaseData
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.GameElement;
}

/// <summary>
/// Note计数用
/// </summary>
[Serializable]
public sealed class NoteElement : GameElement
{
    /// <summary>
    /// 元素唯一Id，不是很懂为啥还要套一层
    /// </summary>
    [JsonPropertyName("elementGuid")] 
    public ElementGuid Guid { get; set; } = null!;
    
    /// <summary>
    /// 准确判定时间
    /// </summary>
    [JsonPropertyName("exactJudgeTime")]
    public double AccurateTime { get; set; }
}

[Serializable]
public class ElementGuid
{
    [JsonPropertyName("value")]
    public Guid Guid { get; set; }
}