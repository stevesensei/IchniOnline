#if DEBUG
using IchniOnline.Server.Mapper;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Game;
using IchniOnline.Server.Models.Responses;
using IchniOnline.Server.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace IchniOnline.Server.Controller;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpPost("beatmap/decode")]
    public GlobalResponse<List<BeatmapNoteDto>> DecodeBeatmap(
        IFormFile file,
        [FromForm] string password,
        [FromForm] bool isGzip)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        var hexContent = Convert.ToHexString(ms.ToArray());

        var (beatmapRoot, _) = EasySaveUtils.DecryptJson<BeatmapRoot>(password, hexContent, isGzip);

        if (beatmapRoot is null)
            return GlobalResponse<List<BeatmapNoteDto>>.BadRequest("Failed to decrypt or parse beatmap");

        var notes = BeatmapMapper.ToNoteDtos(beatmapRoot);
        return GlobalResponse<List<BeatmapNoteDto>>.Ok(notes, "Beatmap decoded");
    }
}

#endif
