namespace Identity.Domain.Enums;

/// <summary>
/// Trạng thái tài khoản người dùng
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Chờ thiết lập mật khẩu lần đầu
    /// </summary>
    PendingActivation = 0,
    
    /// <summary>
    /// Đang hoạt động
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Tạm khóa
    /// </summary>
    Suspended = 2,
    
    /// <summary>
    /// Đã xóa/vô hiệu hóa
    /// </summary>
    Inactive = 3
}
