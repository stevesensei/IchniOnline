using IchniOnline.Server.Models;

namespace IchniOnline.Server.Entities.ThirdParty;

public class TapTapOauth
{
    public Guid Id { get; set; }
    
    public Guid BindUserId { get; set; }
    
    /// <summary>
    /// 按游戏划分的id
    /// </summary>
    public string TapTapOpenId { get; set; } = null!;
    
    /// <summary>
    /// 按厂商划分的Id
    /// </summary>
    public string TapTapUnionId { get; set; } = null!;
    
    public ThirdPartyDataStatus Status { get; set; } = ThirdPartyDataStatus.Unavailable;
}