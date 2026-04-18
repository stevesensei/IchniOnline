using IchniOnline.Server.Models;
using SqlSugar;

namespace IchniOnline.Server.Entities.ThirdParty;

[SugarTable("taptap_inter_auth")]
public class TapTapOauth
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    public Guid Id { get; set; }
    
    [SugarColumn(ColumnName = "user_id")]
    public Guid BindUserId { get; set; }
    
    /// <summary>
    /// 按游戏划分的id
    /// </summary>
    [SugarColumn(ColumnName = "taptap_open_id")]
    public string TapTapOpenId { get; set; } = null!;
    
    /// <summary>
    /// 按厂商划分的Id
    /// </summary>
    [SugarColumn(ColumnName = "taptap_union_id")]
    public string TapTapUnionId { get; set; } = null!;
    
    [SugarColumn(ColumnName = "status")]
    public ThirdPartyDataStatus Status { get; set; } = ThirdPartyDataStatus.Unavailable;
}