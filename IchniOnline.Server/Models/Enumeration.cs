using System.Text.Json.Serialization;
using IchniOnline.Server.Utils;

namespace IchniOnline.Server.Models;

public enum UserPermission
{
    Guest = 0,
    Player = 1,
    Admin = 2
}

[JsonConverter(typeof(SaveDataTypeJsonConverter))]
public enum SaveDataType
{
    None = 0,
    BeatmapContainer = 1,
    GameElement = 2,
    Tap = 3,
    Flick = 4,
    Hold = 5,
    Stay = 6,
}

public enum BeatmapStatus
{
    /// <summary>
    /// 已发布并且可用
    /// </summary>
    Public = 0,
    /// <summary>
    /// 未发布
    /// </summary>
    Private = 1,
    /// <summary>
    /// 定时发布
    /// </summary>
    Scheduled = 2,
}

/// <summary>
/// 游戏将要发布的平台
/// </summary>
public enum GamePublishPlatform
{
    TapTapInternational = 0,
    GooglePlay = 1,
    AppStore = 2,
    Steam = 3
}

public enum ThirdPartyDataStatus
{
    /// <summary>
    /// 等待用户注册或绑定账号/用户删除等情况
    /// </summary>
    Unavailable = 0,
    /// <summary>
    /// 正常可用
    /// </summary>
    Available = 1
}