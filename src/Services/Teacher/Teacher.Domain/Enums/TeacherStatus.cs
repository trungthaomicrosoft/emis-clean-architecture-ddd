namespace Teacher.Domain.Enums;

/// <summary>
/// Trạng thái giáo viên
/// </summary>
public enum TeacherStatus
{
    Active = 1,        // Đang làm việc
    OnLeave = 2,       // Đang nghỉ phép
    Resigned = 3,      // Đã nghỉ việc
    Terminated = 4     // Bị sa thải
}
