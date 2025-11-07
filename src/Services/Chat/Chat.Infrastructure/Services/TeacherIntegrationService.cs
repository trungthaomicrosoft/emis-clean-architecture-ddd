using System.Net.Http.Json;
using Chat.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Implementation of Teacher integration service using HTTP client
/// </summary>
public class TeacherIntegrationService : ITeacherIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TeacherIntegrationService> _logger;

    public TeacherIntegrationService(
        HttpClient httpClient,
        ILogger<TeacherIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TeacherInfoDto>> GetTeachersByClassIdAsync(
        Guid tenantId,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Call Teacher Service API
            var response = await _httpClient.GetAsync(
                $"api/v1/teachers/by-class/{classId}?tenantId={tenantId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch teachers for class {ClassId} from Teacher service. Status: {StatusCode}",
                    classId, response.StatusCode);
                return new List<TeacherInfoDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<TeacherInfoDto>>>(
                cancellationToken: cancellationToken);

            return result?.Data ?? new List<TeacherInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling Teacher service for class {ClassId}",
                classId);
            return new List<TeacherInfoDto>();
        }
    }

    /// <summary>
    /// Wrapper for API response from Teacher service
    /// </summary>
    private class ApiResponseWrapper<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; }
    }
}
