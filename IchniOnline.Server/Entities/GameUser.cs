using IchniOnline.Server.Models;
using SqlSugar;

namespace IchniOnline.Server.Entities;

[SugarTable("game_user")]
public class GameUser
{
    [SugarColumn(IsPrimaryKey = true,ColumnName = "id")]
    public Guid UserId { get; set; }

    [SugarColumn(ColumnName = "username")]
    public string Username { get; set; } = null!;
    
    [SugarColumn(ColumnName = "display_name")]
    public string DisplayName { get; set; } = null!;
    
    [SugarColumn(ColumnName = "avatar_url")]
    public string? AvatarUrl { get; set; }

    [SugarColumn(ColumnName = "permission")]
    public UserPermission Permission { get; set; } = UserPermission.Guest;
    
    [SugarColumn(ColumnName = "password_hashed")]
    public string? PasswordHashed { get; set; }
}