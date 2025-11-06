namespace Chat.Domain.Enums;

/// <summary>
/// Defines the types of messages that can be sent
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Plain text message
    /// </summary>
    Text = 1,

    /// <summary>
    /// Image file (JPEG, PNG, GIF, etc.)
    /// Max size: 10MB
    /// </summary>
    Image = 2,

    /// <summary>
    /// Video file (MP4, AVI, MOV, etc.)
    /// Max size: 25MB
    /// </summary>
    Video = 3,

    /// <summary>
    /// Audio/Voice message (MP3, WAV, M4A, etc.)
    /// Max size: 10MB
    /// </summary>
    Audio = 4,

    /// <summary>
    /// File attachment (PDF, DOC, XLS, etc.)
    /// Max size: 10MB
    /// </summary>
    File = 5,

    /// <summary>
    /// System-generated message (e.g., "User joined", "User left")
    /// </summary>
    System = 6
}
