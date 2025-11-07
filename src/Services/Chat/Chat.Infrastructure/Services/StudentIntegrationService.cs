using System.Net.Http.Json;
using Chat.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Implementation of Student integration service using HTTP client
/// </summary>
public class StudentIntegrationService : IStudentIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StudentIntegrationService> _logger;

    public StudentIntegrationService(
        HttpClient httpClient,
        ILogger<StudentIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StudentInfoDto?> GetStudentWithParentsAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Call Student Service API
            var response = await _httpClient.GetAsync(
                $"api/v1/students/{studentId}/with-parents?tenantId={tenantId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch student {StudentId} from Student service. Status: {StatusCode}",
                    studentId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<StudentInfoDto>>(
                cancellationToken: cancellationToken);

            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling Student service for student {StudentId}",
                studentId);
            return null;
        }
    }

    /// <summary>
    /// Wrapper for API response from Student service
    /// </summary>
    private class ApiResponseWrapper<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; }
    }
}
