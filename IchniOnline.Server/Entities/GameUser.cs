using IchniOnline.Server.Models;

namespace IchniOnline.Server.Entities;

public class GameUser
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;
    
    public string DisplayName { get; set; } = null!;
    
    public string? AvatarUrl { get; set; }

    public UserPermission Permission { get; set; } = UserPermission.Guest;
    
    public string? PasswordHashed { get; set; }
}