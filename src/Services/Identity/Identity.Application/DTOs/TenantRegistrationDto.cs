namespace Identity.Application.DTOs;

/// <summary>
/// DTO: Kết quả đăng ký tenant mới
/// </summary>
public class TenantRegistrationDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public string Subdomain { get; set; } = null!;
    public string AccessUrl { get; set; } = null!; // e.g., https://school-abc.emis.com
    
    public Guid AdminUserId { get; set; }
    public string AdminPhoneNumber { get; set; } = null!;
    public string AdminFullName { get; set; } = null!;
    
    public string SubscriptionPlan { get; set; } = null!;
    public DateTime SubscriptionExpiresAt { get; set; }
    public int MaxUsers { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
