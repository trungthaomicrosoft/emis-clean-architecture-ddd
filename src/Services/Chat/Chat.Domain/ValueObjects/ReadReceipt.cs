using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing when a user read a message
/// Used for read receipts ("Seen at HH:mm")
/// </summary>
public class ReadReceipt : ValueObject
{
    public Guid UserId { get; private set; }
    public DateTime ReadAt { get; private set; }

    private ReadReceipt() { }

    private ReadReceipt(Guid userId, DateTime readAt)
    {
        UserId = userId;
        ReadAt = readAt;
    }

    public static ReadReceipt Create(Guid userId, DateTime readAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        return new ReadReceipt(userId, readAt);
    }

    public static ReadReceipt CreateNow(Guid userId) => Create(userId, DateTime.UtcNow);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        // Note: ReadAt is NOT included - one receipt per user
    }
}
