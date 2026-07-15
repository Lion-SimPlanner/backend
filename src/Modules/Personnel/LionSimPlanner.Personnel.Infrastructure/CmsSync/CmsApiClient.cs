using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace LionSimPlanner.Personnel.Infrastructure.CmsSync;

/// <summary>
/// Typed HTTP client wrapping the external Crew Management System REST API.
/// Responsible for: roster sync (GET) and training record submission (POST).
/// Configured with base address and API key via CmsOptions.
/// </summary>
public sealed class CmsApiClient(
    HttpClient httpClient,
    ILogger<CmsApiClient> logger)
{
    private const string PilotsEndpoint      = "/api/v1/cms/roster/pilots";
    private const string InstructorsEndpoint = "/api/v1/cms/roster/instructors";
    private const string TrainingEndpoint    = "/api/v1/cms/training/records";

    /// <summary>
    /// Fetches the full pilot roster from the CMS.
    /// Called daily at 00:00 by CmsSyncJob.
    /// </summary>
    public async Task<IReadOnlyList<CmsPilotRecord>> GetPilotsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[CMS Sync] Fetching pilot roster from {Endpoint}", PilotsEndpoint);
        var result = await httpClient.GetFromJsonAsync<List<CmsPilotRecord>>(PilotsEndpoint, ct);
        return result ?? [];
    }

    /// <summary>
    /// Fetches the full instructor roster from the CMS.
    /// Called daily at 00:00 by CmsSyncJob.
    /// </summary>
    public async Task<IReadOnlyList<CmsInstructorRecord>> GetInstructorsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[CMS Sync] Fetching instructor roster from {Endpoint}", InstructorsEndpoint);
        var result = await httpClient.GetFromJsonAsync<List<CmsInstructorRecord>>(InstructorsEndpoint, ct);
        return result ?? [];
    }

    /// <summary>
    /// POSTs a completed training record to the CMS.
    /// This is the authoritative write-back that makes the CMS the system of record.
    /// Called immediately when an Instructor completes a grading form.
    /// </summary>
    public async Task PostTrainingRecordAsync(CmsTrainingRecordPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[CMS Sync] Posting training record for session {SessionId}, employee {EmployeeCode}",
            payload.SessionId, payload.EmployeeCode);

        var response = await httpClient.PostAsJsonAsync(TrainingEndpoint, payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "[CMS Sync] Failed to post training record. Status: {Status}, Body: {Body}",
                response.StatusCode, body);
            throw new InvalidOperationException(
                $"CMS training record POST failed ({response.StatusCode}): {body}");
        }

        logger.LogInformation("[CMS Sync] Training record posted successfully for session {SessionId}", payload.SessionId);
    }
}

/// <summary>Bound from appsettings.json "Cms" section.</summary>
public sealed class CmsOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey  { get; init; } = string.Empty;
}
