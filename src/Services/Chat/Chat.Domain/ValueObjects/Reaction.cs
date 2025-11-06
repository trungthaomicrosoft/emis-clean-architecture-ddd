using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing an emoji reaction to a message
/// </summary>
public class Reaction : ValueObject
{
    public string EmojiCode { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; }
    public DateTime ReactedAt { get; private set; }

    private Reaction() 
    {
        EmojiCode = string.Empty;
        UserName = string.Empty;
    }

    private Reaction(string emojiCode, Guid userId, string userName, DateTime reactedAt)
    {
        EmojiCode = emojiCode;
        UserId = userId;
        UserName = userName;
        ReactedAt = reactedAt;
    }

    public static Reaction Create(string emojiCode, Guid userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(emojiCode))
            throw new ArgumentException("EmojiCode cannot be empty", nameof(emojiCode));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName cannot be empty", nameof(userName));

        return new Reaction(emojiCode, userId, userName, DateTime.UtcNow);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EmojiCode;
        yield return UserId;
        yield return UserName;
        // Note: ReactedAt is NOT included in equality - 
        // same user + same emoji = same reaction
    }
}
