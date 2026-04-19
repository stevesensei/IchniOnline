namespace IchniOnline.Server.Models.Requests;

public record RegisterRequest(
    string Username,
    string Password,
    string DisplayName
);
