using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Game;

namespace IchniOnline.Server.Mapper;

public static class BeatmapMapper
{
    private static readonly HashSet<SaveDataType> NoteTypes =
    [
        SaveDataType.Tap,
        SaveDataType.Flick,
        SaveDataType.Hold,
        SaveDataType.Stay
    ];

    public static List<BeatmapNoteDto> ToNoteDtos(BeatmapRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return root.Beatmap.Value.Elements
            .Where(e => NoteTypes.Contains(e.SaveDataType))
            .Select(e => new BeatmapNoteDto(
                Guid.Parse(e.ElementGuid.Value),
                e.SaveDataType,
                e.ExactJudgeTime))
            .ToList();
    }
}