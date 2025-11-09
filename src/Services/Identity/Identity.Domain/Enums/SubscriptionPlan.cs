namespace Identity.Domain.Enums;

/// <summary>
/// Enum: Gói dịch vụ subscription
/// </summary>
public enum SubscriptionPlan
{
    /// <summary>
    /// Gói dùng thử - 30 ngày miễn phí
    /// </summary>
    Trial = 1,

    /// <summary>
    /// Gói cơ bản - Tối đa 100 học sinh
    /// </summary>
    Basic = 2,

    /// <summary>
    /// Gói tiêu chuẩn - Tối đa 500 học sinh
    /// </summary>
    Standard = 3,

    /// <summary>
    /// Gói chuyên nghiệp - Tối đa 2000 học sinh
    /// </summary>
    Professional = 4,

    /// <summary>
    /// Gói doanh nghiệp - Không giới hạn
    /// </summary>
    Enterprise = 5
}
