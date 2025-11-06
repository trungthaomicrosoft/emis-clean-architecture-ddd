using Chat.Domain.Enums;
using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing a participant in a conversation
/// </summary>
public class Participant : ValueObject
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    public int UnreadCount { get; private set; }

    private Participant() 
    {
        UserName = string.Empty;
    }

    private Participant(
        Guid userId,
        string userName,
        ParticipantRole role,
        DateTime joinedAt)
    {
        UserId = userId;
        UserName = userName;
        Role = role;
        JoinedAt = joinedAt;
        LastReadAt = null;
        UnreadCount = 0;
    }

    public static Participant Create(Guid userId, string userName, ParticipantRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName cannot be empty", nameof(userName));

        return new Participant(userId, userName, role, DateTime.UtcNow);
    }

    /// <summary>
    /// Update last read timestamp and reset unread count
    /// </summary>
    public Participant MarkAsRead(DateTime readAt)
    {
        return new Participant(UserId, UserName, Role, JoinedAt)
        {
            LastReadAt = readAt,
            UnreadCount = 0
        };
    }

    /// <summary>
    /// Increment unread count when new message received
    /// </summary>
    public Participant IncrementUnreadCount()
    {
        return new Participant(UserId, UserName, Role, JoinedAt)
        {
            LastReadAt = this.LastReadAt,
            UnreadCount = this.UnreadCount + 1
        };
    }

    /// <summary>
    /// Change participant role (e.g., promote to admin)
    /// </summary>
    public Participant ChangeRole(ParticipantRole newRole)
    {
        return new Participant(UserId, UserName, newRole, JoinedAt)
        {
            LastReadAt = this.LastReadAt,
            UnreadCount = this.UnreadCount
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return UserName;
        yield return Role;
        yield return JoinedAt;
    }
}
