namespace IchniOnline.Server.Models.Dto;

public record BeatmapNoteDto(
    Guid NoteGuid, 
    SaveDataType NoteType,
    double JudgeTime);