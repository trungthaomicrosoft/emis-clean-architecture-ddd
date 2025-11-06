namespace Chat.Infrastructure.Configuration;

/// <summary>
/// Redis configuration settings
/// </summary>
public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "emis_chat:";
    public int DefaultCacheDurationMinutes { get; set; } = 60;
}
