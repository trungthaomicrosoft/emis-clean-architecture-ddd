namespace Identity.Domain.Enums;

/// <summary>
/// Vai trò người dùng trong hệ thống
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Admin trường - có quyền quản lý toàn bộ
    /// </summary>
    SchoolAdmin = 0,
    
    /// <summary>
    /// Giáo viên
    /// </summary>
    Teacher = 1,
    
    /// <summary>
    /// Phụ huynh
    /// </summary>
    Parent = 2,
    
    /// <summary>
    /// Nhân viên hành chính
    /// </summary>
    Staff = 3
}
