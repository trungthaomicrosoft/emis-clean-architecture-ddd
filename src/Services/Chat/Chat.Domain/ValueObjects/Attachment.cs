using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing a file attachment in a message
/// </summary>
public class Attachment : ValueObject
{
    public Guid AttachmentId { get; private set; }
    public string FileName { get; private set; }
    public string FileType { get; private set; }
    public long FileSize { get; private set; }
    public string Url { get; private set; }
    public string? ThumbnailUrl { get; private set; }

    // File size limits (in bytes)
    public const long MAX_IMAGE_SIZE = 10 * 1024 * 1024; // 10MB
    public const long MAX_VIDEO_SIZE = 25 * 1024 * 1024; // 25MB
    public const long MAX_AUDIO_SIZE = 10 * 1024 * 1024; // 10MB
    public const long MAX_FILE_SIZE = 10 * 1024 * 1024;  // 10MB

    private Attachment() 
    {
        FileName = string.Empty;
        FileType = string.Empty;
        Url = string.Empty;
    }

    private Attachment(
        Guid attachmentId,
        string fileName,
        string fileType,
        long fileSize,
        string url,
        string? thumbnailUrl)
    {
        AttachmentId = attachmentId;
        FileName = fileName;
        FileType = fileType;
        FileSize = fileSize;
        Url = url;
        ThumbnailUrl = thumbnailUrl;
    }

    public static Attachment Create(
        string fileName,
        string fileType,
        long fileSize,
        string url,
        string? thumbnailUrl = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileType))
            throw new ArgumentException("FileType cannot be empty", nameof(fileType));
        if (fileSize <= 0)
            throw new ArgumentException("FileSize must be greater than 0", nameof(fileSize));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty", nameof(url));

        // Validate file size based on type
        ValidateFileSize(fileType, fileSize);

        return new Attachment(Guid.NewGuid(), fileName, fileType, fileSize, url, thumbnailUrl);
    }

    private static void ValidateFileSize(string fileType, long fileSize)
    {
        var extension = Path.GetExtension(fileType).ToLowerInvariant();
        
        // Image extensions
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        if (imageExtensions.Contains(extension))
        {
            if (fileSize > MAX_IMAGE_SIZE)
                throw new ArgumentException($"Image file size cannot exceed {MAX_IMAGE_SIZE / (1024 * 1024)}MB");
            return;
        }

        // Video extensions
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
        if (videoExtensions.Contains(extension))
        {
            if (fileSize > MAX_VIDEO_SIZE)
                throw new ArgumentException($"Video file size cannot exceed {MAX_VIDEO_SIZE / (1024 * 1024)}MB");
            return;
        }

        // Audio extensions
        var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".wma" };
        if (audioExtensions.Contains(extension))
        {
            if (fileSize > MAX_AUDIO_SIZE)
                throw new ArgumentException($"Audio file size cannot exceed {MAX_AUDIO_SIZE / (1024 * 1024)}MB");
            return;
        }

        // Other files
        if (fileSize > MAX_FILE_SIZE)
            throw new ArgumentException($"File size cannot exceed {MAX_FILE_SIZE / (1024 * 1024)}MB");
    }

    public bool IsImage()
    {
        var extension = Path.GetExtension(FileType).ToLowerInvariant();
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        return imageExtensions.Contains(extension);
    }

    public bool IsVideo()
    {
        var extension = Path.GetExtension(FileType).ToLowerInvariant();
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
        return videoExtensions.Contains(extension);
    }

    public bool IsAudio()
    {
        var extension = Path.GetExtension(FileType).ToLowerInvariant();
        var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".wma" };
        return audioExtensions.Contains(extension);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return AttachmentId;
        yield return FileName;
        yield return FileType;
        yield return FileSize;
        yield return Url;
    }
}
