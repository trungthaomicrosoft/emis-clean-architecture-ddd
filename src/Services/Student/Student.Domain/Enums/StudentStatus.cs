namespace Student.Domain.Enums;

/// <summary>
/// Trạng thái học sinh
/// </summary>
public enum StudentStatus
{
    Active = 1,        // Đang học
    Inactive = 2,      // Tạm nghỉ
    Graduated = 3,     // Đã tốt nghiệp
    Transferred = 4,   // Chuyển trường
    Expelled = 5       // Thôi học
}
