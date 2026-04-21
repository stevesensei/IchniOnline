using ErrorOr;
using IchniOnline.Server.Data;
using IchniOnline.Server.Models.Dto;
using StackExchange.Redis;
using System.Text.Json;
using IchniOnline.Server.Entities;
using IchniOnline.Server.Mapper;
using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Game;
using IchniOnline.Server.Service.Interface;
using IchniOnline.Server.Service.Storage;
using IchniOnline.Server.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IchniOnline.Server.Service;

/// <summary>
/// 谱面服务
/// </summary>
public class BeatmapService(AppDbContext db,
    IConnectionMultiplexer redis,
    IFileStorageService fileStorageService,
    ILogger<BeatmapService> logger) : IBeatmapService
{
    private const string BeatmapCollectionCachePrefix = "beatmap:collection:draft@";
    private const string BeatmapCachePrefix = "beatmap:collection@";
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

        if (file.Length <= 0)
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

    public async Task<ErrorOr<string>> CreateBeatmap(BeatmapDivisionDto divisionDto, IFormFile levelData,
        Guid collectionId)
    {
        //检查下输入合法性
        if (string.IsNullOrWhiteSpace(divisionDto.LevelDesigner))
            return Error.Validation("Beatmap.LevelDesigner", "Level designer is required");
        if(string.IsNullOrWhiteSpace(divisionDto.Difficulty))
            return Error.Validation("Beatmap.Difficulty", "Difficulty is required");
        if(string.IsNullOrWhiteSpace(divisionDto.LevelColor))
            return Error.Validation("Beatmap.LevelColor", "Level color is required");
        if(levelData.Length <= 0)
            return Error.Validation("Beatmap.LevelData", "Level data invalid");
        //先检查数据库的collection数据，获取第一个使用此collectionId的谱面
        var redisDb = redis.GetDatabase();
        var existing = await db.Beatmaps
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CollectionId == collectionId);
        var dtoData = new BeatmapDto();
        var cacheKey = $"{BeatmapCollectionCachePrefix}{collectionId}";
        if (existing is null)
        {
            //检查缓存中是否有暂存的Collection
            var dataSource =  await redisDb.StringGetAsync(cacheKey);
            if (dataSource.IsNullOrEmpty)
            {
                //返回错误
                return Error.NotFound("Beatmap.CollectionDraftNotFound", "Beatmap collection draft not found");
            }
            dtoData = JsonSerializer.Deserialize<BeatmapDto>(dataSource.ToString())!;
        }
        else
        {
            //直接应用数据库内容
            dtoData = new BeatmapDto
            {
                CollectionId = existing.CollectionId,
                SongName = existing.SongName,
                Illustrator = existing.Illustrator,
                IllustrateUrl = existing.IllustrateUrl,
                Composer = existing.Composer
            };
        }
        //文件内容解码
        using var ms = new MemoryStream();
        await levelData.CopyToAsync(ms);
        var hexContent = Convert.ToHexString(ms.ToArray());
        var (beatmapRoot, _) = EasySaveUtils.DecryptJson<BeatmapRoot>("Soullies515", hexContent, true);
        if (beatmapRoot is null)
        {
            return Error.Validation("Beatmap.LevelData", "Level data invalid");
        }
        var notes = BeatmapMapper.ToNoteDtos(beatmapRoot);
        //将谱面Dto转换回BeatmapDb
        var newData = new BeatmapDb()
        {
            BeatmapId = Guid.NewGuid(),
            CollectionId = dtoData.CollectionId,
            SongName = dtoData.SongName,
            Illustrator = dtoData.Illustrator,
            IllustrateUrl = dtoData.IllustrateUrl,
            Composer = dtoData.Composer,
            LevelDesigner = divisionDto.LevelDesigner,
            Difficulty = divisionDto.Difficulty,
            LevelColor = divisionDto.LevelColor,
            Notes = notes,
            //这俩等之后开发到再补全
            ScheduledReleaseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status = BeatmapStatus.Public
        };
        //传递到数据库
        db.Beatmaps.Add(newData);
        var result = await db.SaveChangesAsync();
        if (result <= 0)
        {
            return Error.Failure("Global.DatabaseError", "Failed to save beatmap to database");
        }
        //删除暂存
        await redisDb.KeyDeleteAsync(cacheKey);
        return newData.BeatmapId.ToString();
    }

    /// <summary>
    /// 获取合集
    /// </summary>
    /// <returns></returns>
    public async Task<ErrorOr<BeatmapDto>> GetBeatmapCollection(Guid collectionId,bool availableOnly)
    {
        List<BeatmapDb> results = new();
        try
        {
            var res = await GetBeatmapFromCollection(collectionId);
            if (res.IsError)
            {
                return Error.Failure(res.Errors.First().Code, res.Errors.First().Description);
            }
            results = res.Value;
        }
        catch (Exception e)
        {
            return Error.Failure("Global.DatabaseError", $"Failed to retrieve beatmap collection: {e.Message}");
        }

        if (availableOnly)
        {
            results = results.Where(b => b.Status == BeatmapStatus.Public).ToList();
        }
        //转换到Dto
        var beatmapDivisionDtos = results.Select(b => new BeatmapDivisionDto()
        {
            BeatmapId = b.BeatmapId,
            Difficulty = b.Difficulty,
            LevelColor = b.LevelColor,
            LevelDesigner = b.LevelDesigner
        }).ToList();
        var beatmapDto = results.Select(b => new BeatmapDto()
        {
            CollectionId = b.CollectionId,
            SongName = b.SongName,
            Illustrator = b.Illustrator,
            IllustrateUrl = b.IllustrateUrl,
            Composer = b.Composer,
            Divisions = beatmapDivisionDtos
        }).First();
        return beatmapDto;
    }
    
    private async Task<ErrorOr<List<BeatmapDb>>> GetBeatmapFromCollection(Guid collectionId)
    {
        List<BeatmapDb> results = new();
        var redisDb = redis.GetDatabase();
        if (redisDb.KeyExists($"{BeatmapCollectionCachePrefix}{collectionId}"))
        {
            results =
                JsonSerializer.Deserialize<List<BeatmapDb>>(
                    redisDb.StringGet($"{BeatmapCollectionCachePrefix}{collectionId}").ToString()) ?? new();
        }
        else
        {
            results = await db.Beatmaps
                .AsNoTracking()
                .Where(b => b.CollectionId == collectionId)
                .ToListAsync();
            //写缓存
            await redisDb.StringSetAsync($"{BeatmapCollectionCachePrefix}{collectionId}",
                JsonSerializer.Serialize(results), BeatmapCollectionCacheTtl);
        }
        return results;
    }

    /// <summary>
    /// 获取note柱状图
    /// </summary>
    /// <returns></returns>
    public async Task<ErrorOr<List<BeatmapNoteChartComponent>>> GetBeatmapChart(Guid beatmapGuid)
    {
        //获取collectionId
        var collectionId = db.Beatmaps.AsNoTracking()
            .Where(b => b.BeatmapId == beatmapGuid)
            .Select(b => b.CollectionId)
            .FirstOrDefault();
        if (collectionId == Guid.Empty)
        {
            return Error.NotFound("Beatmap.NotFound", "Beatmap not found");
        }
        //获取谱面数据
        var res = await GetBeatmapFromCollection(collectionId);
        if (res.IsError)
        {
            return Error.Failure(res.Errors.First().Code, res.Errors.First().Description);
        }
        var beatmap = res.Value.FirstOrDefault(x => x.BeatmapId == beatmapGuid);
        if (beatmap == null)
        {
            return Error.NotFound("Beatmap.NotFound", "Beatmap not found");
        }

        var charts = new List<BeatmapNoteChartComponent>()
        {
            new()            {
                From = 0,
                To = 10,
                Count = 0
            }
        };
        foreach (var note in beatmap.Notes)
        {
            var targetComp = charts.FirstOrDefault(c => c.From <= note.JudgeTime && c.To > note.JudgeTime);
            if (targetComp == null)
            {
                //循环创建
                while (charts.Max(x => x.From) < note.JudgeTime)
                {
                    charts.Add(new BeatmapNoteChartComponent()
                    {
                        From = charts.Max(x => x.From) + 10,
                        To = charts.Max(x => x.To) + 10,
                        Count = 0
                    });
                }
                targetComp = charts.FirstOrDefault(c => c.From <= note.JudgeTime && c.To > note.JudgeTime);
                targetComp.Count++;
            }
            else
            {
                targetComp.Count++;
            }
        }

        return charts;
    }
}