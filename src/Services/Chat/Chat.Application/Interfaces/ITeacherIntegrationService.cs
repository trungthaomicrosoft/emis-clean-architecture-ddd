namespace Chat.Application.Interfaces;

/// <summary>
/// Service to integrate with Teacher microservice
/// </summary>
public interface ITeacherIntegrationService
{
    /// <summary>
    /// Get all teachers assigned to a class
    /// </summary>
    Task<List<TeacherInfoDto>> GetTeachersByClassIdAsync(
        Guid tenantId, 
        Guid classId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for teacher information from Teacher service
/// </summary>
public class TeacherInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHeadTeacher { get; set; }
}
