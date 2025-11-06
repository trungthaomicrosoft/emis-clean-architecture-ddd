namespace Chat.Infrastructure.Configuration;

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "emis_chat";
    
    /// <summary>
    /// Collection names
    /// </summary>
    public string ConversationsCollection { get; set; } = "conversations";
    public string MessagesCollection { get; set; } = "messages";
}
