namespace Chat.Domain.Enums;

/// <summary>
/// Defines the role of a participant in a conversation
/// </summary>
public enum ParticipantRole
{
    /// <summary>
    /// Regular member who can read and send messages
    /// </summary>
    Member = 1,

    /// <summary>
    /// Administrator who can manage the conversation
    /// Can add/remove participants, pin messages, delete messages
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Read-only access (used in AnnouncementChannel for parents)
    /// Can only read messages, cannot send
    /// </summary>
    ReadOnly = 3
}
