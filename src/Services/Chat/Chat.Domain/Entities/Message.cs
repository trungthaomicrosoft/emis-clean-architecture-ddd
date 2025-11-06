using Chat.Domain.Enums;
using Chat.Domain.Events;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.SharedKernel;

namespace Chat.Domain.Entities;

/// <summary>
/// Message entity representing a single message in a conversation
/// Contains business logic for editing, deleting, reactions, and pins
/// </summary>
public class Message : Entity
{
    public Guid MessageId { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string SenderName { get; private set; }
    public string Content { get; private set; }
    public MessageType Type { get; private set; }
    
    private readonly List<Attachment> _attachments = new();
    public IReadOnlyList<Attachment> Attachments => _attachments.AsReadOnly();
    
    public ReplyToMessage? ReplyTo { get; private set; }
    
    private readonly List<Mention> _mentions = new();
    public IReadOnlyList<Mention> Mentions => _mentions.AsReadOnly();
    
    private readonly List<Reaction> _reactions = new();
    public IReadOnlyList<Reaction> Reactions => _reactions.AsReadOnly();
    
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsPinned { get; private set; }
    public Guid? PinnedBy { get; private set; }
    public DateTime? PinnedAt { get; private set; }
    
    private readonly List<ReadReceipt> _readReceipts = new();
    public IReadOnlyList<ReadReceipt> ReadReceipts => _readReceipts.AsReadOnly();

    // Business rules constants
    private const int EDIT_WINDOW_MINUTES = 15;
    private const int MAX_CONTENT_LENGTH = 5000;

    // Private constructor for EF Core
    private Message()
    {
        SenderName = string.Empty;
        Content = string.Empty;
    }

    // Public constructor for creating new messages
    private Message(
        Guid conversationId,
        Guid senderId,
        string senderName,
        string content,
        MessageType type)
    {
        MessageId = Guid.NewGuid();
        ConversationId = conversationId;
        SenderId = senderId;
        SenderName = senderName;
        Content = content;
        Type = type;
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        IsDeleted = false;
        IsPinned = false;

        ValidateContent(content, type);
    }

    /// <summary>
    /// Create a new text message
    /// </summary>
    public static Message CreateText(
        Guid conversationId,
        Guid senderId,
        string senderName,
        string content,
        ReplyToMessage? replyTo = null,
        List<Mention>? mentions = null)
    {
        var message = new Message(conversationId, senderId, senderName, content, MessageType.Text);
        message.ReplyTo = replyTo;
        
        if (mentions != null && mentions.Any())
        {
            message._mentions.AddRange(mentions);
        }

        return message;
    }

    /// <summary>
    /// Create a new message with attachments (image, video, audio, file)
    /// </summary>
    public static Message CreateWithAttachment(
        Guid conversationId,
        Guid senderId,
        string senderName,
        MessageType type,
        List<Attachment> attachments,
        string? caption = null,
        ReplyToMessage? replyTo = null)
    {
        if (type == MessageType.Text || type == MessageType.System)
            throw new BusinessRuleValidationException("Cannot create attachment message with Text or System type");

        if (attachments == null || !attachments.Any())
            throw new BusinessRuleValidationException("Attachments are required for non-text messages");

        var message = new Message(conversationId, senderId, senderName, caption ?? string.Empty, type);
        message._attachments.AddRange(attachments);
        message.ReplyTo = replyTo;

        return message;
    }

    /// <summary>
    /// Create a system message (auto-generated)
    /// </summary>
    public static Message CreateSystem(Guid conversationId, string content)
    {
        return new Message(conversationId, Guid.Empty, "System", content, MessageType.System);
    }

    /// <summary>
    /// Edit message content (within 15-minute window)
    /// Business Rule: Can only edit within 15 minutes of sending
    /// Business Rule: Cannot edit deleted messages
    /// Business Rule: Cannot edit system messages
    /// </summary>
    public void Edit(string newContent, Guid editorUserId)
    {
        // Business rule validation
        if (IsDeleted)
            throw new BusinessRuleValidationException("Cannot edit a deleted message");

        if (Type == MessageType.System)
            throw new BusinessRuleValidationException("Cannot edit system messages");

        if (editorUserId != SenderId)
            throw new BusinessRuleValidationException("Only the message sender can edit the message");

        var minutesSinceSent = (DateTime.UtcNow - SentAt).TotalMinutes;
        if (minutesSinceSent > EDIT_WINDOW_MINUTES)
            throw new BusinessRuleValidationException($"Cannot edit message after {EDIT_WINDOW_MINUTES} minutes");

        if (string.IsNullOrWhiteSpace(newContent))
            throw new BusinessRuleValidationException("Message content cannot be empty");

        ValidateContent(newContent, Type);

        var oldContent = Content;
        Content = newContent;
        EditedAt = DateTime.UtcNow;

        AddDomainEvent(new MessageEditedEvent(MessageId, ConversationId, oldContent, newContent, EditedAt.Value));
    }

    /// <summary>
    /// Soft delete message
    /// Business Rule: Keep sender info for audit trail
    /// </summary>
    public void Delete(Guid deleterUserId)
    {
        if (IsDeleted)
            return; // Already deleted, idempotent

        // Business rule: Only sender or admin can delete
        if (deleterUserId != SenderId)
        {
            // TODO: Check if deleterUserId is admin (needs authorization service)
            // For now, only allow sender to delete
            throw new BusinessRuleValidationException("Only the message sender can delete the message");
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Content = "[Message deleted]"; // Replace content but keep metadata

        AddDomainEvent(new MessageDeletedEvent(MessageId, ConversationId, deleterUserId, DeletedAt.Value));
    }

    /// <summary>
    /// Add emoji reaction
    /// Business Rule: One reaction per emoji per user (update timestamp if already exists)
    /// </summary>
    public void AddReaction(string emojiCode, Guid userId, string userName)
    {
        if (IsDeleted)
            throw new BusinessRuleValidationException("Cannot react to a deleted message");

        // Remove existing reaction from same user with same emoji (if exists)
        var existingReaction = _reactions.FirstOrDefault(r => r.UserId == userId && r.EmojiCode == emojiCode);
        if (existingReaction != null)
        {
            _reactions.Remove(existingReaction);
        }

        var reaction = Reaction.Create(emojiCode, userId, userName);
        _reactions.Add(reaction);

        AddDomainEvent(new ReactionAddedEvent(MessageId, ConversationId, emojiCode, userId, userName));
    }

    /// <summary>
    /// Remove reaction
    /// </summary>
    public void RemoveReaction(string emojiCode, Guid userId)
    {
        var reaction = _reactions.FirstOrDefault(r => r.UserId == userId && r.EmojiCode == emojiCode);
        if (reaction != null)
        {
            _reactions.Remove(reaction);
        }
    }

    /// <summary>
    /// Pin message
    /// Business Rule: Only admins can pin in AnnouncementChannel
    /// </summary>
    public void Pin(Guid pinnedByUserId)
    {
        if (IsDeleted)
            throw new BusinessRuleValidationException("Cannot pin a deleted message");

        if (IsPinned)
            return; // Already pinned, idempotent

        IsPinned = true;
        PinnedBy = pinnedByUserId;
        PinnedAt = DateTime.UtcNow;

        // Domain event will be raised by Conversation aggregate
    }

    /// <summary>
    /// Unpin message
    /// </summary>
    public void Unpin(Guid unpinnedByUserId)
    {
        if (!IsPinned)
            return; // Already unpinned, idempotent

        IsPinned = false;
        PinnedBy = null;
        PinnedAt = null;

        AddDomainEvent(new MessageUnpinnedEvent(MessageId, ConversationId, unpinnedByUserId));
    }

    /// <summary>
    /// Mark message as delivered to a user
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status == MessageStatus.Sent)
        {
            Status = MessageStatus.Delivered;
        }
    }

    /// <summary>
    /// Mark message as read by a user
    /// </summary>
    public void MarkAsRead(Guid userId)
    {
        // Update status to Read
        if (Status != MessageStatus.Read)
        {
            Status = MessageStatus.Read;
        }

        // Add read receipt if not exists
        var existingReceipt = _readReceipts.FirstOrDefault(r => r.UserId == userId);
        if (existingReceipt == null)
        {
            _readReceipts.Add(ReadReceipt.CreateNow(userId));
        }
    }

    /// <summary>
    /// Validate message content based on type
    /// </summary>
    private void ValidateContent(string content, MessageType type)
    {
        if (type == MessageType.Text && string.IsNullOrWhiteSpace(content))
        {
            throw new BusinessRuleValidationException("Text message content cannot be empty");
        }

        if (!string.IsNullOrEmpty(content) && content.Length > MAX_CONTENT_LENGTH)
        {
            throw new BusinessRuleValidationException($"Message content cannot exceed {MAX_CONTENT_LENGTH} characters");
        }
    }
}
