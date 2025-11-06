using Chat.Domain.Enums;
using Chat.Domain.Events;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.BuildingBlocks.MultiTenant;
using EMIS.SharedKernel;

namespace Chat.Domain.Aggregates;

/// <summary>
/// Conversation Aggregate Root
/// Manages conversation lifecycle, participants, and enforces business rules
/// </summary>
public class Conversation : TenantEntity, IAggregateRoot
{
    public Guid ConversationId { get; private set; }
    public ConversationType Type { get; private set; }
    public string Name { get; private set; }
    public ConversationMetadata Metadata { get; private set; }
    
    private readonly List<Participant> _participants = new();
    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();
    
    public MessageSummary? LastMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    // Business rules constants
    private const int MIN_PARTICIPANTS_ONE_TO_ONE = 2;
    private const int MAX_PARTICIPANTS_ONE_TO_ONE = 2;
    private const int MAX_NAME_LENGTH = 255;

    // Private constructor for EF Core
    private Conversation()
    {
        Name = string.Empty;
        Metadata = ConversationMetadata.Empty();
    }

    // Private constructor for creating conversations
    private Conversation(
        Guid tenantId,
        ConversationType type,
        string name,
        ConversationMetadata metadata)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty", nameof(tenantId));

        ConversationId = Guid.NewGuid();
        TenantId = tenantId;
        Type = type;
        Name = name;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;

        ValidateName(name, type);
    }

    #region Factory Methods

    /// <summary>
    /// Create a OneToOne conversation between two users
    /// Business Rule: Must have exactly 2 participants
    /// </summary>
    public static Conversation CreateOneToOne(
        Guid tenantId,
        Guid user1Id,
        string user1Name,
        Guid user2Id,
        string user2Name)
    {
        if (user1Id == user2Id)
            throw new BusinessRuleValidationException("Cannot create conversation with the same user");

        var conversation = new Conversation(
            tenantId,
            ConversationType.OneToOne,
            $"{user1Name} - {user2Name}",
            ConversationMetadata.Empty());

        // Add both participants as members
        conversation.AddParticipantInternal(user1Id, user1Name, ParticipantRole.Member);
        conversation.AddParticipantInternal(user2Id, user2Name, ParticipantRole.Member);

        conversation.AddDomainEvent(new ConversationCreatedEvent(
            conversation.ConversationId,
            tenantId,
            ConversationType.OneToOne.ToString(),
            new List<Guid> { user1Id, user2Id }));

        return conversation;
    }

    /// <summary>
    /// Create a StudentGroup conversation for a specific student
    /// Business Rule: Must have StudentId in metadata
    /// Business Rule: Created when teacher initiates chat about a student
    /// </summary>
    public static Conversation CreateStudentGroup(
        Guid tenantId,
        Guid studentId,
        string studentName,
        Guid teacherUserId,
        string teacherName,
        List<(Guid UserId, string UserName)> parents)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(studentId));

        if (!parents.Any())
            throw new BusinessRuleValidationException("StudentGroup must have at least one parent");

        var metadata = ConversationMetadata.CreateForStudent(studentId, studentName);
        var conversation = new Conversation(
            tenantId,
            ConversationType.StudentGroup,
            $"Student: {studentName}",
            metadata);

        // Add teacher as admin
        conversation.AddParticipantInternal(teacherUserId, teacherName, ParticipantRole.Admin);

        // Add all parents as members
        foreach (var parent in parents)
        {
            conversation.AddParticipantInternal(parent.UserId, parent.UserName, ParticipantRole.Member);
        }

        var allParticipantIds = parents.Select(p => p.UserId).Append(teacherUserId).ToList();
        conversation.AddDomainEvent(new ConversationCreatedEvent(
            conversation.ConversationId,
            tenantId,
            ConversationType.StudentGroup.ToString(),
            allParticipantIds));

        return conversation;
    }

    /// <summary>
    /// Create a ClassGroup conversation for a class
    /// Business Rule: Must have ClassId in metadata
    /// Business Rule: Auto-created when class is created
    /// </summary>
    public static Conversation CreateClassGroup(
        Guid tenantId,
        Guid classId,
        string className,
        List<(Guid UserId, string UserName)> teachers,
        List<(Guid UserId, string UserName)> parents)
    {
        if (classId == Guid.Empty)
            throw new ArgumentException("ClassId cannot be empty", nameof(classId));

        if (!teachers.Any())
            throw new BusinessRuleValidationException("ClassGroup must have at least one teacher");

        var metadata = ConversationMetadata.CreateForClass(classId, className);
        var conversation = new Conversation(
            tenantId,
            ConversationType.ClassGroup,
            $"Class: {className}",
            metadata);

        // Add all teachers as admins
        foreach (var teacher in teachers)
        {
            conversation.AddParticipantInternal(teacher.UserId, teacher.UserName, ParticipantRole.Admin);
        }

        // Add all parents as members
        foreach (var parent in parents)
        {
            conversation.AddParticipantInternal(parent.UserId, parent.UserName, ParticipantRole.Member);
        }

        var allParticipantIds = teachers.Select(t => t.UserId)
            .Concat(parents.Select(p => p.UserId))
            .ToList();

        conversation.AddDomainEvent(new ConversationCreatedEvent(
            conversation.ConversationId,
            tenantId,
            ConversationType.ClassGroup.ToString(),
            allParticipantIds));

        return conversation;
    }

    /// <summary>
    /// Create a TeacherGroup conversation
    /// Business Rule: All participants must be teachers
    /// </summary>
    public static Conversation CreateTeacherGroup(
        Guid tenantId,
        string groupName,
        Guid creatorUserId,
        string creatorName,
        List<(Guid UserId, string UserName)> teachers)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("TeacherGroup must have a name", nameof(groupName));

        var conversation = new Conversation(
            tenantId,
            ConversationType.TeacherGroup,
            groupName,
            ConversationMetadata.Empty());

        // Add creator as admin
        conversation.AddParticipantInternal(creatorUserId, creatorName, ParticipantRole.Admin);

        // Add all teachers as members
        foreach (var teacher in teachers.Where(t => t.UserId != creatorUserId))
        {
            conversation.AddParticipantInternal(teacher.UserId, teacher.UserName, ParticipantRole.Member);
        }

        var allParticipantIds = teachers.Select(t => t.UserId).Append(creatorUserId).Distinct().ToList();
        conversation.AddDomainEvent(new ConversationCreatedEvent(
            conversation.ConversationId,
            tenantId,
            ConversationType.TeacherGroup.ToString(),
            allParticipantIds));

        return conversation;
    }

    /// <summary>
    /// Create an AnnouncementChannel
    /// Business Rule: Parents have read-only access
    /// Business Rule: Only admins/teachers can post
    /// </summary>
    public static Conversation CreateAnnouncementChannel(
        Guid tenantId,
        string channelName,
        Guid creatorUserId,
        string creatorName,
        List<(Guid UserId, string UserName)> admins,
        List<(Guid UserId, string UserName)> readOnlyUsers)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("AnnouncementChannel must have a name", nameof(channelName));

        var conversation = new Conversation(
            tenantId,
            ConversationType.AnnouncementChannel,
            channelName,
            ConversationMetadata.Empty());

        // Add creator as admin
        conversation.AddParticipantInternal(creatorUserId, creatorName, ParticipantRole.Admin);

        // Add all admins
        foreach (var admin in admins.Where(a => a.UserId != creatorUserId))
        {
            conversation.AddParticipantInternal(admin.UserId, admin.UserName, ParticipantRole.Admin);
        }

        // Add all read-only users (parents)
        foreach (var user in readOnlyUsers)
        {
            conversation.AddParticipantInternal(user.UserId, user.UserName, ParticipantRole.ReadOnly);
        }

        var allParticipantIds = admins.Select(a => a.UserId)
            .Concat(readOnlyUsers.Select(u => u.UserId))
            .Append(creatorUserId)
            .Distinct()
            .ToList();

        conversation.AddDomainEvent(new ConversationCreatedEvent(
            conversation.ConversationId,
            tenantId,
            ConversationType.AnnouncementChannel.ToString(),
            allParticipantIds));

        return conversation;
    }

    #endregion

    #region Participant Management

    /// <summary>
    /// Add a participant to the conversation
    /// Business Rule: Cannot add to OneToOne conversation
    /// Business Rule: Validate participant count limits
    /// </summary>
    public void AddParticipant(Guid userId, string userName, ParticipantRole role)
    {
        // Business rule: Cannot modify OneToOne conversations
        if (Type == ConversationType.OneToOne)
            throw new BusinessRuleValidationException("Cannot add participants to OneToOne conversations");

        // Check if already exists
        if (_participants.Any(p => p.UserId == userId))
            throw new BusinessRuleValidationException("Participant already exists in conversation");

        AddParticipantInternal(userId, userName, role);

        AddDomainEvent(new ParticipantAddedEvent(
            ConversationId,
            userId,
            userName,
            role.ToString()));

        UpdateTimestamp();
    }

    /// <summary>
    /// Remove a participant from the conversation
    /// Business Rule: Cannot remove from OneToOne conversation
    /// Business Rule: Must keep at least one admin in group conversations
    /// </summary>
    public void RemoveParticipant(Guid userId, Guid removedBy)
    {
        // Business rule: Cannot modify OneToOne conversations
        if (Type == ConversationType.OneToOne)
            throw new BusinessRuleValidationException("Cannot remove participants from OneToOne conversations");

        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            throw new BusinessRuleValidationException("Participant not found in conversation");

        // Business rule: Must keep at least one admin
        if (participant.Role == ParticipantRole.Admin)
        {
            var adminCount = _participants.Count(p => p.Role == ParticipantRole.Admin);
            if (adminCount <= 1)
                throw new BusinessRuleValidationException("Cannot remove the last admin from the conversation");
        }

        _participants.Remove(participant);

        AddDomainEvent(new ParticipantRemovedEvent(ConversationId, userId));

        UpdateTimestamp();
    }

    /// <summary>
    /// Promote participant to admin
    /// </summary>
    public void PromoteToAdmin(Guid userId, Guid promotedBy)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            throw new BusinessRuleValidationException("Participant not found in conversation");

        if (participant.Role == ParticipantRole.Admin)
            return; // Already admin

        var index = _participants.IndexOf(participant);
        _participants[index] = participant.ChangeRole(ParticipantRole.Admin);

        UpdateTimestamp();
    }

    /// <summary>
    /// Check if user is participant
    /// </summary>
    public bool IsParticipant(Guid userId)
    {
        return _participants.Any(p => p.UserId == userId);
    }

    /// <summary>
    /// Check if user is admin
    /// </summary>
    public bool IsAdmin(Guid userId)
    {
        return _participants.Any(p => p.UserId == userId && p.Role == ParticipantRole.Admin);
    }

    /// <summary>
    /// Check if user can send messages
    /// Business Rule: Read-only users cannot send messages
    /// </summary>
    public bool CanSendMessage(Guid userId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            return false;

        return participant.Role != ParticipantRole.ReadOnly;
    }

    /// <summary>
    /// Get participant by userId
    /// </summary>
    public Participant? GetParticipant(Guid userId)
    {
        return _participants.FirstOrDefault(p => p.UserId == userId);
    }

    #endregion

    #region Message Management

    /// <summary>
    /// Update last message summary
    /// Called when a new message is sent
    /// </summary>
    public void UpdateLastMessage(Guid messageId, string content, Guid senderId, string senderName, DateTime sentAt)
    {
        LastMessage = MessageSummary.Create(messageId, content, senderId, senderName, sentAt);
        UpdateTimestamp();

        // Increment unread count for all participants except sender
        for (int i = 0; i < _participants.Count; i++)
        {
            if (_participants[i].UserId != senderId)
            {
                _participants[i] = _participants[i].IncrementUnreadCount();
            }
        }
    }

    /// <summary>
    /// Mark messages as read for a user
    /// </summary>
    public void MarkMessagesAsRead(Guid userId, DateTime readAt)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            return;

        var index = _participants.IndexOf(participant);
        _participants[index] = participant.MarkAsRead(readAt);

        UpdateTimestamp();
    }

    /// <summary>
    /// Get unread count for a user
    /// </summary>
    public int GetUnreadCount(Guid userId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        return participant?.UnreadCount ?? 0;
    }

    #endregion

    #region Conversation Lifecycle

    /// <summary>
    /// Archive conversation (soft delete)
    /// </summary>
    public void Archive()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Restore archived conversation
    /// </summary>
    public void Restore()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Update conversation name (for group conversations)
    /// </summary>
    public void UpdateName(string newName, Guid updatedBy)
    {
        if (Type == ConversationType.OneToOne)
            throw new BusinessRuleValidationException("Cannot rename OneToOne conversations");

        if (Type == ConversationType.StudentGroup || Type == ConversationType.ClassGroup)
            throw new BusinessRuleValidationException("Cannot rename auto-generated group conversations");

        if (!IsAdmin(updatedBy))
            throw new BusinessRuleValidationException("Only admins can rename the conversation");

        ValidateName(newName, Type);

        Name = newName;
        UpdateTimestamp();
    }

    #endregion

    #region Private Helper Methods

    private void AddParticipantInternal(Guid userId, string userName, ParticipantRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        var participant = Participant.Create(userId, userName, role);
        _participants.Add(participant);
    }

    private void ValidateName(string name, ConversationType type)
    {
        if (type == ConversationType.OneToOne)
            return; // Auto-generated name, no validation needed

        if (type == ConversationType.TeacherGroup || type == ConversationType.AnnouncementChannel)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleValidationException("Conversation name cannot be empty");
        }

        if (!string.IsNullOrEmpty(name) && name.Length > MAX_NAME_LENGTH)
            throw new BusinessRuleValidationException($"Conversation name cannot exceed {MAX_NAME_LENGTH} characters");
    }

    private void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
