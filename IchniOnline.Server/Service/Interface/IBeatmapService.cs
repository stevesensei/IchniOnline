using ErrorOr;
using IchniOnline.Server.Models.Dto;

namespace IchniOnline.Server.Service.Interface;

public interface IBeatmapService
{
    Task<ErrorOr<string>> CreateBeatmapCollection(BeatmapDto beatmapDto, IFormFile file);
}

