using System.Text.Json.Serialization;

namespace IchniOnline.Server.Models.Game;

[Serializable]
public class TapAccessToken
{
    [JsonPropertyName("kid")] 
    public string Token { get; set; } = null!;
    
    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = null!;
    
    [JsonPropertyName("macKey")]
    public string MacKey { get; set; } = null!;
    
    [JsonPropertyName("macAlgorithm")]
    public string MacAlgorithm { get; set; } = null!;
}

