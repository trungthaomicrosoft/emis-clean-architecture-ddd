using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Domain.Enums;
using Chat.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Chat.Infrastructure.Services;

/// <summary>
/// MinIO (S3-compatible) implementation of IFileStorageService
/// Handles file uploads, downloads, and lifecycle management
/// </summary>
public class MinIOFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinIOSettings _settings;
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB
    private const long MaxDocumentSize = 25 * 1024 * 1024; // 25MB

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
    private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".aac" };

    public MinIOFileStorageService(
        IMinioClient minioClient,
        IOptions<MinIOSettings> settings)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
    }

    public async Task<FileUploadResult> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file size
            var fileSize = fileStream.Length;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            ValidateFileSize(fileSize, extension);

            // Generate unique object name with folder structure
            var objectName = GenerateObjectName(tenantId, conversationId, fileName);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Upload to MinIO
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileSize)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            // Generate URL
            var url = await GetFileUrlAsync(objectName, cancellationToken);

            return new FileUploadResult
            {
                Success = true,
                FileUrl = url,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                StorageKey = objectName
            };
        }
        catch (Exception ex)
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = $"File upload failed: {ex.Message}"
            };
        }
    }

    public async Task<Stream?> DownloadFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract object name from URL
            var objectName = ExtractObjectNameFromUrl(fileUrl);

            var memoryStream = new MemoryStream();
            
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = ExtractObjectNameFromUrl(fileUrl);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate pre-signed URL valid for 7 days
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(storageKey)
                .WithExpiry(7 * 24 * 60 * 60); // 7 days in seconds

            return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
        }
        catch (Exception)
        {
            // Fallback to direct URL if pre-signed fails
            return $"{_settings.Endpoint}/{_settings.BucketName}/{storageKey}";
        }
    }

    public async Task<bool> FileExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = ExtractObjectNameFromUrl(fileUrl);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<long> GetFileSizeAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = ExtractObjectNameFromUrl(fileUrl);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName);

            var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return objectStat.Size;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public Task<string> GenerateThumbnailAsync(
        string fileUrl,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement thumbnail generation using ImageSharp or similar library
        // For now, return original file URL
        return Task.FromResult(fileUrl);
    }

    public async Task DeleteOldFilesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // List all objects in bucket
            var listArgs = new ListObjectsArgs()
                .WithBucket(_settings.BucketName)
                .WithRecursive(true);

            var objectsToDelete = new List<string>();
            var observable = _minioClient.ListObjectsEnumAsync(listArgs, cancellationToken);

            await foreach (var item in observable)
            {
                if (item.LastModifiedDateTime.HasValue && item.LastModifiedDateTime.Value < olderThan)
                {
                    objectsToDelete.Add(item.Key);
                }
            }

            // Delete objects in batches
            foreach (var objectName in objectsToDelete)
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Log error
            // TODO: Add logging
        }
    }

    public MessageType GetMessageTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (ImageExtensions.Contains(extension))
            return MessageType.Image;

        if (VideoExtensions.Contains(extension))
            return MessageType.Video;

        if (AudioExtensions.Contains(extension))
            return MessageType.Audio;

        if (DocumentExtensions.Contains(extension))
            return MessageType.File;

        return MessageType.File; // Default to File for unknown types
    }

    public bool ValidateFileType(string fileName, MessageType expectedType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return expectedType switch
        {
            MessageType.Image => ImageExtensions.Contains(extension),
            MessageType.Video => VideoExtensions.Contains(extension),
            MessageType.Audio => AudioExtensions.Contains(extension),
            MessageType.File => DocumentExtensions.Contains(extension) || !IsMediaExtension(extension),
            _ => false
        };
    }

    #region Private Methods

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(_settings.BucketName);

        bool exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(_settings.BucketName);

            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
        }
    }

    private string GenerateObjectName(Guid tenantId, Guid conversationId, string fileName)
    {
        // Structure: {tenantId}/{conversationId}/{year}/{month}/{guid}_{filename}
        var now = DateTime.UtcNow;
        var uniqueId = Guid.NewGuid();
        var safeFileName = SanitizeFileName(fileName);

        return $"{tenantId}/{conversationId}/{now.Year:D4}/{now.Month:D2}/{uniqueId}_{safeFileName}";
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    private string ExtractObjectNameFromUrl(string fileUrl)
    {
        // Extract object name from pre-signed URL or direct URL
        var uri = new Uri(fileUrl);
        var path = uri.AbsolutePath;
        
        // Remove bucket name from path if present
        var bucketPrefix = $"/{_settings.BucketName}/";
        if (path.StartsWith(bucketPrefix))
        {
            return path.Substring(bucketPrefix.Length);
        }

        return path.TrimStart('/');
    }

    private void ValidateFileSize(long fileSize, string extension)
    {
        if (ImageExtensions.Contains(extension))
        {
            if (fileSize > MaxImageSize)
            {
                throw new InvalidOperationException($"Image file size exceeds maximum limit of {MaxImageSize / 1024 / 1024}MB");
            }
        }
        else if (fileSize > MaxDocumentSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum limit of {MaxDocumentSize / 1024 / 1024}MB");
        }
    }

    private bool IsMediaExtension(string extension)
    {
        return ImageExtensions.Contains(extension) ||
               VideoExtensions.Contains(extension) ||
               AudioExtensions.Contains(extension);
    }

    #endregion
}
