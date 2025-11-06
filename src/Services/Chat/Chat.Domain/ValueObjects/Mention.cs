using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing a user mention (@username) in a message
/// </summary>
public class Mention : ValueObject
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; }
    public int StartIndex { get; private set; }
    public int Length { get; private set; }

    private Mention() 
    {
        UserName = string.Empty;
    }

    private Mention(Guid userId, string userName, int startIndex, int length)
    {
        UserId = userId;
        UserName = userName;
        StartIndex = startIndex;
        Length = length;
    }

    public static Mention Create(Guid userId, string userName, int startIndex, int length)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName cannot be empty", nameof(userName));
        if (startIndex < 0)
            throw new ArgumentException("StartIndex cannot be negative", nameof(startIndex));
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        return new Mention(userId, userName, startIndex, length);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return UserName;
        yield return StartIndex;
        yield return Length;
    }
}
