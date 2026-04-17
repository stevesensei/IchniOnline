using System.Text.Json.Serialization;
using IchniOnline.Server.Models;

namespace IchniOnline.Server.Models.Game;

public abstract class GameElement
{
    [JsonPropertyName("__type")]
    public virtual SaveDataType SaveDataType { get; set; } = SaveDataType.GameElement;

    [JsonPropertyName("elementGuid")]
    public ElementGuid ElementGuid { get; set; } = null!;

    [JsonPropertyName("attachedElementGuid")]
    public ElementGuid? AttachedElementGuid { get; set; }

    [JsonPropertyName("elementName")]
    public string ElementName { get; set; } = null!;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("exactJudgeTime")]
    public double ExactJudgeTime { get; set; }
}

[Serializable]
public class ElementGuid
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}

public abstract class SaveBaseData : GameElement
{
    [JsonPropertyName("__type")]
    public override abstract SaveDataType SaveDataType { get; set; }
}

/// <summary>
/// Tap note
/// </summary>
[Serializable]
public sealed class TapElement : GameElement
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.Tap;
}

/// <summary>
/// Stay note
/// </summary>
[Serializable]
public sealed class StayElement : GameElement
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.Stay;
}

/// <summary>
/// Hold note
/// </summary>
[Serializable]
public sealed class HoldElement : GameElement
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.Hold;
}

/// <summary>
/// Flick note
/// </summary>
[Serializable]
public sealed class FlickElement : GameElement
{
    [JsonPropertyName("__type")]
    public override SaveDataType SaveDataType { get; set; } = SaveDataType.Flick;
}