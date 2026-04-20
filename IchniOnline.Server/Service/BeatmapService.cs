using ErrorOr;
using IchniOnline.Server.Models.Dto;
using SqlSugar;
using StackExchange.Redis;
using System.Text.Json;
using IchniOnline.Server.Service.Interface;
using IchniOnline.Server.Service.Storage;

namespace IchniOnline.Server.Service;

/// <summary>
/// 谱面服务
/// </summary>
public class BeatmapService(ISqlSugarClient db,
    IConnectionMultiplexer redis,
    IFileStorageService fileStorageService,
    ILogger<BeatmapService> logger) : IBeatmapService
{
    private const string BeatmapCollectionCachePrefix = "beatmap:collection:draft:";
    private static readonly TimeSpan BeatmapCollectionCacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 由于没有Collection对应的表，所以需要先将其校验后创建到缓存内
    /// 图片先上传到存储服务，获取到Url之后创建一个缓存
    /// 然后等待谱面上传
    /// </summary>
    /// <param name="beatmapDto"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<ErrorOr<string>> CreateBeatmapCollection(BeatmapDto beatmapDto, IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(beatmapDto.SongName))
            return Error.Validation("Beatmap.SongName", "SongName is required");

        if (string.IsNullOrWhiteSpace(beatmapDto.Illustrator))
            return Error.Validation("Beatmap.Illustrator", "Illustrator is required");

        if (string.IsNullOrWhiteSpace(beatmapDto.Composer))
            return Error.Validation("Beatmap.Composer", "Composer is required");

        if (file is null || file.Length <= 0)
            return Error.Validation("Beatmap.IllustrateImage", "Illustrate image is required");

        var collectionId = Guid.CreateVersion7();
        beatmapDto.CollectionId = collectionId;

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        var objectFileName = $"beatmap/illustrators/{collectionId}{extension.ToLowerInvariant()}";

        await using var stream = file.OpenReadStream();
        var uploadResult = await fileStorageService.UploadAsync(stream, objectFileName, file.ContentType);
        beatmapDto.IllustrateUrl = fileStorageService.GetFileUrl(uploadResult.FileName);

        var redisDb = redis.GetDatabase();
        var cacheKey = $"{BeatmapCollectionCachePrefix}{collectionId}";
        var payload = JsonSerializer.Serialize(beatmapDto);

        await redisDb.StringSetAsync(cacheKey, payload, BeatmapCollectionCacheTtl);

        logger.LogInformation("Beatmap collection draft created and cached: {CollectionId}", collectionId);
        return collectionId.ToString();
    }
}