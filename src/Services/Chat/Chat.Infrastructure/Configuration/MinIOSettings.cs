namespace Chat.Infrastructure.Configuration;

/// <summary>
/// MinIO configuration settings (S3-compatible storage)
/// </summary>
public class MinIOSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "emis-chat-files";
    public bool UseSSL { get; set; } = false;
    
    /// <summary>
    /// File retention in days (default 365 days = 1 year)
    /// </summary>
    public int FileRetentionDays { get; set; } = 365;
}
