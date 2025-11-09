namespace Identity.Domain.Enums;

/// <summary>
/// Enum: Trạng thái của Tenant (Trường học)
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Đang hoạt động bình thường
    /// </summary>
    Active = 1,

    /// <summary>
    /// Tạm ngưng (hết hạn subscription, vi phạm chính sách)
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Ngừng hoạt động (chủ động hoặc bị hủy)
    /// </summary>
    Inactive = 3,

    /// <summary>
    /// Đang trong giai đoạn dùng thử
    /// </summary>
    Trial = 4
}
