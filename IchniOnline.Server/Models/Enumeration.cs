namespace IchniOnline.Server.Models;

public enum UserPermission
{
    Guest = 0,
    Player = 1,
    Admin = 2
}

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