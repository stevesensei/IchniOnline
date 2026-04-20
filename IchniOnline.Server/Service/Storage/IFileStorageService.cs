namespace IchniOnline.Server.Service.Storage;

/// <summary>
/// 图片处理状态
/// </summary>
public enum ImageProcessingStatus
{
    /// <summary>图片不存在</summary>
    NotFound,
    /// <summary>正在处理中（原始文件存在，WebP 未生成）</summary>
    Processing,
    /// <summary>处理完成（WebP 已生成）</summary>
    Completed
}

/// <summary>
/// 文件上传结果
/// </summary>
/// <param name="FileId">文件 ID (GUID)</param>
/// <param name="FileName">存储的文件名（含路径）</param>
/// <param name="Size">文件大小（字节）</param>
/// <param name="ContentType">文件 MIME 类型</param>
public record FileUploadResult(
    Guid FileId,
    string FileName,
    long Size,
    string ContentType
);

/// <summary>
/// 图片上传结果
/// </summary>
/// <param name="FileId">文件 ID (GUID)</param>
/// <param name="OriginalFileName">原图文件名</param>
/// <param name="ThumbnailFileName">缩略图文件名</param>
/// <param name="OriginalSize">原图大小（字节）</param>
/// <param name="ThumbnailSize">缩略图大小（字节）</param>
public record ImageUploadResult(
    Guid FileId,
    string OriginalFileName,
    string ThumbnailFileName,
    long OriginalSize,
    long ThumbnailSize
);

/// <summary>
/// 文件内容结果
/// </summary>
/// <param name="Stream">文件流</param>
/// <param name="ContentType">MIME 类型</param>
/// <param name="FileName">文件名</param>
public record FileContentResult(
    Stream Stream,
    string ContentType,
    string FileName
);

/// <summary>
/// 文件存储服务接口
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件（通用接口）
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名（含路径，如 "images/xxx.webp"）</param>
    /// <param name="contentType">MIME 类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件上传结果</returns>
    Task<FileUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件流
    /// </summary>
    /// <param name="fileName">文件名（含路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件流，如果文件不存在返回 null</returns>
    Task<FileContentResult?> GetStreamAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileName">文件名（含路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="fileName">文件名（含路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件的完整访问 URL
    /// </summary>
    /// <param name="fileName">文件名（含路径）</param>
    /// <returns>完整 URL</returns>
    string GetFileUrl(string fileName);
}
