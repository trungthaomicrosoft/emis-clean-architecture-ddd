namespace Chat.Domain.Enums;

/// <summary>
/// Defines the types of conversations in the chat system
/// </summary>
public enum ConversationType
{
    /// <summary>
    /// One-to-one conversation between two users (e.g., parent and teacher)
    /// </summary>
    OneToOne = 1,

    /// <summary>
    /// Group conversation for a specific student (parents + primary teacher)
    /// Created when teacher initiates chat about a student
    /// </summary>
    StudentGroup = 2,

    /// <summary>
    /// Group conversation for a class (all parents + teachers)
    /// Auto-created when class is created
    /// </summary>
    ClassGroup = 3,

    /// <summary>
    /// Private group for teachers only
    /// </summary>
    TeacherGroup = 4,

    /// <summary>
    /// Announcement channel where only admins/teachers can post
    /// Parents can only read
    /// </summary>
    AnnouncementChannel = 5
}
