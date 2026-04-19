namespace IchniOnline.Server.Models.Requests;

public record LoginRequest(
    string Username,
    string EncryptedPassword,
    string SessionKey
);
