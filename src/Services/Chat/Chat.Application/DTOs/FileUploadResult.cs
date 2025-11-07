namespace Chat.Application.DTOs;

/// <summary>
/// Result of file upload operation
/// </summary>
public class FileUploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
