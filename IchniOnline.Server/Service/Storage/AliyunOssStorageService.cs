using Aliyun.OSS;
using Microsoft.Extensions.Options;
using MimeTypes;

namespace IchniOnline.Server.Service.Storage;

/// <summary>
/// 阿里云 OSS 存储配置
/// </summary>
public class AliyunOssOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "AliyunOss";

    /// <summary>
    /// Endpoint（如 "oss-cn-hangzhou.aliyuncs.com"）
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access Key ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Access Key Secret
    /// </summary>
    public string AccessKeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Bucket 名称
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 自定义域名（CDN 加速域名，如 "https://cdn.example.com"）
    /// 如果为空，则使用 OSS 默认域名
    /// </summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// 文件存储前缀（目录），如 "delivery/"
    /// </summary>
    public string Prefix { get; set; } = string.Empty;
}

/// <summary>
/// 阿里云 OSS 存储服务实现
/// </summary>
public class AliyunOssStorageService : IFileStorageService
{
    private readonly AliyunOssOptions _options;
    private readonly OssClient _client;
    private readonly ILogger<AliyunOssStorageService> _logger;

    public AliyunOssStorageService(
        IOptions<AliyunOssOptions> options,
        ILogger<AliyunOssStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 创建 OSS 客户端
        _client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
    }

    /// <inheritdoc />
    public async Task<FileUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // 拼接完整的对象键（含前缀）
        var objectKey = string.IsNullOrEmpty(_options.Prefix)
            ? fileName
            : $"{_options.Prefix.TrimEnd('/')}/{fileName}";

        var metadata = new ObjectMetadata
        {
            ContentType = contentType,
            // 设置缓存控制
            CacheControl = "max-age=31536000"
        };

        // OSS SDK 是同步的，包装为异步
        var size = stream.Length;
        await Task.Run(() => _client.PutObject(_options.BucketName, objectKey, stream, metadata), cancellationToken);

        // 从文件名中提取 GUID
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (fileNameWithoutExt.EndsWith("_thumb"))
        {
            fileNameWithoutExt = fileNameWithoutExt[..^6];
        }
        var fileId = Guid.TryParse(fileNameWithoutExt, out var id) ? id : Guid.CreateVersion7();

        _logger.LogInformation("文件已上传到阿里云 OSS: {ObjectKey}, 大小: {Size} bytes, ID: {FileId}", objectKey, size, fileId);

        return new FileUploadResult(
            FileId: fileId,
            FileName: fileName,
            Size: size,
            ContentType: contentType
        );
    }

    /// <inheritdoc />
    public async Task<FileContentResult?> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrEmpty(_options.Prefix)
            ? fileName
            : $"{_options.Prefix.TrimEnd('/')}/{fileName}";

        var exists = await Task.Run(() => _client.DoesObjectExist(_options.BucketName, objectKey), cancellationToken);
        if (!exists)
        {
            return null;
        }

        var ossObject = await Task.Run(() => _client.GetObject(_options.BucketName, objectKey), cancellationToken);
        var extension = Path.GetExtension(fileName);
        var contentType = MimeTypeMap.GetMimeType(extension);

        return new FileContentResult(
            Stream: ossObject.Content,
            ContentType: contentType,
            FileName: Path.GetFileName(fileName)
        );
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrEmpty(_options.Prefix)
            ? fileName
            : $"{_options.Prefix.TrimEnd('/')}/{fileName}";

        await Task.Run(() => _client.DeleteObject(_options.BucketName, objectKey), cancellationToken);
        _logger.LogInformation("文件已从阿里云 OSS 删除: {ObjectKey}", objectKey);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrEmpty(_options.Prefix)
            ? fileName
            : $"{_options.Prefix.TrimEnd('/')}/{fileName}";

        return await Task.Run(() => _client.DoesObjectExist(_options.BucketName, objectKey), cancellationToken);
    }

    /// <inheritdoc />
    public string GetFileUrl(string fileName)
    {
        var objectKey = string.IsNullOrEmpty(_options.Prefix)
            ? fileName
            : $"{_options.Prefix.TrimEnd('/')}/{fileName}";

        // 优先使用自定义域名（CDN）
        if (!string.IsNullOrEmpty(_options.CustomDomain))
        {
            var domain = _options.CustomDomain.TrimEnd('/');
            return $"{domain}/{objectKey}";
        }

        // 使用 OSS 默认域名
        return $"https://{_options.BucketName}.{_options.Endpoint}/{objectKey}";
    }
}
