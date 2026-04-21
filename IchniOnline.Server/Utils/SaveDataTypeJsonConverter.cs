using System.Text.Json;
using System.Text.Json.Serialization;
using IchniOnline.Server.Models;

namespace IchniOnline.Server.Utils;

public class SaveDataTypeJsonConverter : JsonConverter<SaveDataType>
{
    private static readonly Dictionary<SaveDataType, string> EnumToString = new()
    {
        { SaveDataType.BeatmapContainer, "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp" },
        { SaveDataType.GameElement, "Ichni.RhythmGame.Beatmap.GameElement_BM,Assembly-CSharp" },
        { SaveDataType.Tap, "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp" },
        { SaveDataType.Flick, "Ichni.RhythmGame.Beatmap.Flick_BM,Assembly-CSharp" },
        { SaveDataType.Hold, "Ichni.RhythmGame.Beatmap.Hold_BM,Assembly-CSharp" },
        { SaveDataType.Stay, "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp" },
    };

    private static readonly Dictionary<string, SaveDataType> StringToEnum = new()
    {
        { "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp", SaveDataType.BeatmapContainer },
        { "Ichni.RhythmGame.Beatmap.GameElement_BM,Assembly-CSharp", SaveDataType.GameElement },
        { "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp", SaveDataType.Tap },
        { "Ichni.RhythmGame.Beatmap.Flick_BM,Assembly-CSharp", SaveDataType.Flick },
        { "Ichni.RhythmGame.Beatmap.Hold_BM,Assembly-CSharp", SaveDataType.Hold },
        { "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp", SaveDataType.Stay },
    };

    public override SaveDataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (!reader.TryGetInt32(out var intValue))
                throw new JsonException("Invalid numeric value for SaveDataType");

            return Enum.IsDefined(typeof(SaveDataType), intValue)
                ? (SaveDataType)intValue
                : SaveDataType.None;
        }

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected string or number for SaveDataType, got {reader.TokenType}");

        var value = reader.GetString()!;
        if (int.TryParse(value, out var parsedInt))
        {
            return Enum.IsDefined(typeof(SaveDataType), parsedInt)
                ? (SaveDataType)parsedInt
                : SaveDataType.None;
        }

        return StringToEnum.TryGetValue(value, out var result)
            ? result
            : SaveDataType.None;
    }

    public override void Write(Utf8JsonWriter writer, SaveDataType value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}
