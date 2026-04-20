using ErrorOr;
using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Responses;
using IchniOnline.Server.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IchniOnline.Server.Controller;

[ApiController]
[Route("/api/beatmap")]
public class BeatmapController(IBeatmapService beatmapService): ControllerBase
{
    /// <summary>
    /// 全新创建谱面合集
    /// </summary>
    /// <returns></returns>
    [HttpPost("collection")]
    [Consumes("multipart/form-data")]
    //[Authorize(Roles = nameof(UserPermission.Admin))]
    public async Task<GlobalResponse<string>> CreateBeatmapCollectionAsync(
        [FromForm] string songName,
        [FromForm] string illustrator,
        [FromForm] IFormFile illustrateImage,
        [FromForm] string composer
        )
    {
        var beatmapDto = new BeatmapDto
        {
            SongName = songName,
            Illustrator = illustrator,
            Composer = composer
        };

        var result = await beatmapService.CreateBeatmapCollection(beatmapDto, illustrateImage);

        return result.Match(
            collectionId => GlobalResponse<string>.Ok(collectionId, "Beatmap collection draft created"),
            ToErrorResponse<string>);
    }

    [NonAction]
    private static GlobalResponse<T> ToErrorResponse<T>(List<Error> errors)
    {
        var first = errors.First();

        return first.Type switch
        {
            ErrorType.Validation => GlobalResponse<T>.BadRequest(first.Description),
            ErrorType.Unauthorized => GlobalResponse<T>.Unauthorized(first.Description),
            ErrorType.Forbidden => GlobalResponse<T>.Forbidden(first.Description),
            ErrorType.NotFound => GlobalResponse<T>.NotFound(first.Description),
            _ => GlobalResponse<T>.InternalServerError(first.Description)
        };
    }
}