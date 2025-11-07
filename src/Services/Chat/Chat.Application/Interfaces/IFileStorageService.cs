using Chat.Application.DTOs;
using Chat.Domain.Enums;

namespace Chat.Application.Interfaces;

/// <summary>
/// Interface for file storage service (MinIO implementation in Infrastructure layer)
/// Handles file uploads, downloads, and lifecycle management
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    Task<FileUploadResult> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    Task<Stream?> DownloadFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pre-signed URL for file access
    /// </summary>
    Task<string> GetFileUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists in storage
    /// </summary>
    Task<bool> FileExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    Task<long> GetFileSizeAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate thumbnail for image/video
    /// </summary>
    Task<string> GenerateThumbnailAsync(
        string fileUrl,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old files (cleanup job)
    /// </summary>
    Task DeleteOldFilesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message type from file extension
    /// </summary>
    MessageType GetMessageTypeFromFileName(string fileName);

    /// <summary>
    /// Validate file type matches expected message type
    /// </summary>
    bool ValidateFileType(string fileName, MessageType expectedType);
}
