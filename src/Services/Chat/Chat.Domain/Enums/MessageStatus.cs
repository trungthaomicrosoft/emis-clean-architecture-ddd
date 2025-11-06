namespace Chat.Domain.Enums;

/// <summary>
/// Defines the delivery status of a message
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message has been sent to the server
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message has been delivered to recipient's device
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message has been read by recipient
    /// </summary>
    Read = 3
}
