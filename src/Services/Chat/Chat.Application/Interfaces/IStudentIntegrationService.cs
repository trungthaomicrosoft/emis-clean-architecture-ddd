namespace Chat.Application.Interfaces;

/// <summary>
/// Service to integrate with Student microservice
/// </summary>
public interface IStudentIntegrationService
{
    /// <summary>
    /// Get student information including parents
    /// </summary>
    Task<StudentInfoDto?> GetStudentWithParentsAsync(
        Guid tenantId, 
        Guid studentId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for student information from Student service
/// </summary>
public class StudentInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ClassId { get; set; }
    public List<ParentInfoDto> Parents { get; set; } = new();
}

/// <summary>
/// DTO for parent information
/// </summary>
public class ParentInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty; // Father, Mother, Guardian
}
