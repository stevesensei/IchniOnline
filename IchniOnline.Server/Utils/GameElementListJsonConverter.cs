using System.Text.Json;
using System.Text.Json.Serialization;
using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Game;

namespace IchniOnline.Server.Utils;

public class GameElementListJsonConverter : JsonConverterFactory
{
    private static readonly Dictionary<string, Type> TypeMap = new()
    {
        { "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp", typeof(TapElement) },
        { "Ichni.RhythmGame.Beatmap.Flick_BM,Assembly-CSharp", typeof(FlickElement) },
        { "Ichni.RhythmGame.Beatmap.Hold_BM,Assembly-CSharp", typeof(HoldElement) },
        { "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp", typeof(StayElement) },
    };

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType
           && typeToConvert.GetGenericTypeDefinition() == typeof(List<>)
           && typeToConvert.GetGenericArguments()[0] == typeof(GameElement);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter?)Activator.CreateInstance(typeof(ConverterImpl));

    private class ConverterImpl : JsonConverter<List<GameElement>>
    {
        public override List<GameElement>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of array");

            var list = new List<GameElement>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("__type", out var typeProperty))
                        continue;

                    var typeValue = typeProperty.GetString();
                    if (string.IsNullOrEmpty(typeValue) || !TypeMap.TryGetValue(typeValue, out var elementType))
                        continue; // Skip unknown element types

                    var json = root.GetRawText();
                    var element = JsonSerializer.Deserialize(json, elementType, options) as GameElement;
                    if (element != null)
                        list.Add(element);
                }
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<GameElement> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, options);
    }
}
