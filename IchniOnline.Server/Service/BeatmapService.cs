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
    private const string BeatmapCollectionDraftCachePrefix = "beatmap:collection:draft@";
    private const string BeatmapCollectionDetailCachePrefix = "beatmap:collection:detail@";
    private const string BeatmapCollectionIdsCachePrefix = "beatmap:collection:ids@";
    private const double ChartBucketSizeSeconds = 10d;
    private static readonly TimeSpan BeatmapCollectionDraftCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan BeatmapCollectionDetailCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BeatmapCollectionIdsCacheTtl = TimeSpan.FromMinutes(1);

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
        var cacheKey = $"{BeatmapCollectionDraftCachePrefix}{collectionId}";
        var payload = JsonSerializer.Serialize(beatmapDto);

        await redisDb.StringSetAsync(cacheKey, payload, BeatmapCollectionDraftCacheTtl);

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
        BeatmapDto dtoData;
        var cacheKey = $"{BeatmapCollectionDraftCachePrefix}{collectionId}";
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
        await InvalidateCollectionCachesAsync(collectionId);
        return newData.BeatmapId.ToString();
    }

    /// <summary>
    /// 获取合集
    /// </summary>
    /// <returns></returns>
    public async Task<ErrorOr<BeatmapDto>> GetBeatmapCollection(Guid collectionId,bool availableOnly)
    {
        if (collectionId == Guid.Empty)
        {
            return Error.Validation("Beatmap.CollectionId", "Collection id is required");
        }

        var redisDb = redis.GetDatabase();
        var cacheKey = BuildCollectionCacheKey(collectionId, availableOnly);
        var cachedCollectionPayload = await redisDb.StringGetAsync(cacheKey);
        if (!cachedCollectionPayload.IsNullOrEmpty)
        {
            var cachedCollection = JsonSerializer.Deserialize<BeatmapDto>(cachedCollectionPayload.ToString());
            if (cachedCollection is not null)
            {
                return cachedCollection;
            }
        }

        List<BeatmapDb> results;
        try
        {
            var query = db.Beatmaps
                .AsNoTracking()
                .Where(b => b.CollectionId == collectionId);

            if (availableOnly)
            {
                query = query.Where(b => b.Status == BeatmapStatus.Public);
            }

            results = await query.ToListAsync();
        }
        catch (Exception e)
        {
            return Error.Failure("Global.DatabaseError", $"Failed to retrieve beatmap collection: {e.Message}");
        }

        if (results.Count == 0)
        {
            return Error.NotFound("Beatmap.CollectionNotFound", "Beatmap collection not found");
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

        await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(beatmapDto), BeatmapCollectionDetailCacheTtl);
        return beatmapDto;
    }

    public async Task<ErrorOr<BeatmapPagedDto>> GetBeatmapCollectionsPage(int page, int pageSize, bool availableOnly)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var redisDb = redis.GetDatabase();
        var collectionIdsCacheKey = BuildCollectionIdsCacheKey(availableOnly);

        List<Guid>? collectionIds = null;
        var cachedIdsPayload = await redisDb.StringGetAsync(collectionIdsCacheKey);
        if (!cachedIdsPayload.IsNullOrEmpty)
        {
            collectionIds = JsonSerializer.Deserialize<List<Guid>>(cachedIdsPayload.ToString());
        }

        try
        {
            var query = db.Beatmaps
                .AsNoTracking()
                .AsQueryable();

            if (availableOnly)
            {
                query = query.Where(b => b.Status == BeatmapStatus.Public);
            }

            if (collectionIds is null)
            {
                collectionIds = await query
                    .GroupBy(b => b.CollectionId)
                    .Select(g => new
                    {
                        CollectionId = g.Key,
                        LatestReleaseTime = g.Max(x => x.ScheduledReleaseTime)
                    })
                    .OrderByDescending(x => x.LatestReleaseTime)
                    .ThenBy(x => x.CollectionId)
                    .Select(x => x.CollectionId)
                    .ToListAsync();

                await redisDb.StringSetAsync(collectionIdsCacheKey,
                    JsonSerializer.Serialize(collectionIds), BeatmapCollectionIdsCacheTtl);
            }

            var total = collectionIds.Count;

            var skip = (normalizedPage - 1) * normalizedPageSize;
            var pagedCollectionIds = collectionIds
                .Skip(skip)
                .Take(normalizedPageSize)
                .ToList();

            var pageRows = pagedCollectionIds.Count == 0
                ? new List<BeatmapDb>()
                : await query
                    .Where(b => pagedCollectionIds.Contains(b.CollectionId))
                    .ToListAsync();

            var groupedRows = pageRows
                .GroupBy(b => b.CollectionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = new List<BeatmapListItemDto>(pagedCollectionIds.Count);
            foreach (var collectionId in pagedCollectionIds)
            {
                if (!groupedRows.TryGetValue(collectionId, out var collectionRows) || collectionRows.Count == 0)
                {
                    continue;
                }

                var first = collectionRows[0];
                items.Add(new BeatmapListItemDto
                {
                    CollectionId = first.CollectionId,
                    SongName = first.SongName,
                    IllustrateUrl = first.IllustrateUrl,
                    Illustrator = first.Illustrator,
                    Composer = first.Composer,
                    Divisions = collectionRows.Select(row => new BeatmapDivisionDto
                    {
                        BeatmapId = row.BeatmapId,
                        Difficulty = row.Difficulty,
                        LevelColor = row.LevelColor,
                        LevelDesigner = row.LevelDesigner
                    }).ToList()
                });
            }

            var result = new BeatmapPagedDto
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Total = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize),
                Items = items
            };
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve beatmap collections page");
            return Error.Failure("Global.DatabaseError", "Failed to retrieve beatmap collections page");
        }
    }

    /// <summary>
    /// 获取note柱状图
    /// </summary>
    /// <returns></returns>
    public async Task<ErrorOr<List<BeatmapNoteChartComponent>>> GetBeatmapChart(Guid beatmapGuid)
    {
        if (beatmapGuid == Guid.Empty)
        {
            return Error.Validation("Beatmap.BeatmapId", "Beatmap id is required");
        }

        try
        {
            var beatmap = await db.Beatmaps
                .AsNoTracking()
                .Where(b => b.BeatmapId == beatmapGuid)
                .Select(b => new { b.Notes })
                .FirstOrDefaultAsync();

            if (beatmap is null)
            {
                return Error.NotFound("Beatmap.NotFound", "Beatmap not found");
            }

            return BuildNoteChart(beatmap.Notes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build beatmap chart for {BeatmapId}", beatmapGuid);
            return Error.Failure("Global.DatabaseError", "Failed to build beatmap chart");
        }
    }

    private static List<BeatmapNoteChartComponent> BuildNoteChart(List<BeatmapNoteDto>? notes)
    {
        var bucketCounts = new Dictionary<int, long>();

        if (notes is not null)
        {
            foreach (var note in notes)
            {
                if (double.IsNaN(note.JudgeTime) || double.IsInfinity(note.JudgeTime))
                {
                    continue;
                }

                var normalizedTime = Math.Max(0d, note.JudgeTime);
                var bucketIndex = (int)Math.Floor(normalizedTime / ChartBucketSizeSeconds);
                bucketCounts[bucketIndex] = bucketCounts.GetValueOrDefault(bucketIndex) + 1;
            }
        }

        var maxBucketIndex = bucketCounts.Count == 0 ? 0 : bucketCounts.Keys.Max();
        var charts = new List<BeatmapNoteChartComponent>(maxBucketIndex + 1);

        for (var i = 0; i <= maxBucketIndex; i++)
        {
            charts.Add(new BeatmapNoteChartComponent
            {
                From = i * ChartBucketSizeSeconds,
                To = (i + 1) * ChartBucketSizeSeconds,
                Count = bucketCounts.GetValueOrDefault(i)
            });
        }

        return charts;
    }

    private static string BuildCollectionCacheKey(Guid collectionId, bool availableOnly)
        => $"{BeatmapCollectionDetailCachePrefix}{collectionId}:a{availableOnly}";

    private static string BuildCollectionIdsCacheKey(bool availableOnly)
        => $"{BeatmapCollectionIdsCachePrefix}a{availableOnly}";

    private async Task InvalidateCollectionCachesAsync(Guid collectionId)
    {
        var redisDb = redis.GetDatabase();
        await redisDb.KeyDeleteAsync(BuildCollectionCacheKey(collectionId, true));
        await redisDb.KeyDeleteAsync(BuildCollectionCacheKey(collectionId, false));
        await redisDb.KeyDeleteAsync(BuildCollectionIdsCacheKey(true));
        await redisDb.KeyDeleteAsync(BuildCollectionIdsCacheKey(false));
    }
}