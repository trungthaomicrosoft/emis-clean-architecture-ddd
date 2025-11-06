using EMIS.SharedKernel;

namespace Chat.Domain.ValueObjects;

/// <summary>
/// Value Object representing metadata for group conversations
/// Immutable data about the context of the conversation
/// </summary>
public class ConversationMetadata : ValueObject
{
    public Guid? StudentId { get; private set; }
    public string? StudentName { get; private set; }
    public Guid? ClassId { get; private set; }
    public string? ClassName { get; private set; }

    private ConversationMetadata() { }

    private ConversationMetadata(
        Guid? studentId,
        string? studentName,
        Guid? classId,
        string? className)
    {
        StudentId = studentId;
        StudentName = studentName;
        ClassId = classId;
        ClassName = className;
    }

    /// <summary>
    /// Create metadata for StudentGroup conversation
    /// </summary>
    public static ConversationMetadata CreateForStudent(Guid studentId, string studentName)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(studentId));
        if (string.IsNullOrWhiteSpace(studentName))
            throw new ArgumentException("StudentName cannot be empty", nameof(studentName));

        return new ConversationMetadata(studentId, studentName, null, null);
    }

    /// <summary>
    /// Create metadata for ClassGroup conversation
    /// </summary>
    public static ConversationMetadata CreateForClass(Guid classId, string className)
    {
        if (classId == Guid.Empty)
            throw new ArgumentException("ClassId cannot be empty", nameof(classId));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("ClassName cannot be empty", nameof(className));

        return new ConversationMetadata(null, null, classId, className);
    }

    /// <summary>
    /// Empty metadata for OneToOne, TeacherGroup, or AnnouncementChannel
    /// </summary>
    public static ConversationMetadata Empty() => new ConversationMetadata(null, null, null, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StudentId;
        yield return StudentName;
        yield return ClassId;
        yield return ClassName;
    }
}
