using EMIS.SharedKernel;

namespace Chat.Domain.Events;

/// <summary>
/// Domain event raised when a participant is added to a conversation
/// </summary>
public class ParticipantAddedEvent : DomainEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public string UserName { get; }
    public string ParticipantRole { get; }

    public ParticipantAddedEvent(
        Guid conversationId,
        Guid userId,
        string userName,
        string participantRole)
    {
        ConversationId = conversationId;
        UserId = userId;
        UserName = userName;
        ParticipantRole = participantRole;
    }
}
