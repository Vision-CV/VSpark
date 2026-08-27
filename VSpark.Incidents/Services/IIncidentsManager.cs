using VSpark.Incidents.Enums;
using VSpark.Incidents.Models.Adapters;
using VSpark.Incidents.Models.Dtos;

namespace VSpark.Incidents.Services;

public interface IIncidentsManager
{
    public Task<IncidentDto?> GetIncidentAsync(Guid incidentId);

    // Service takes the responsibility of formatting the input data. Wrong.
    public Task<OpResult> AddIncidentAsync(NewIncidentDto incident);

    public Task<OpResult> ChangeIncidentStatus(Guid incidentId, IncidentStatus status);

    public Task<OpResult> DeleteIncidentAsync(Guid incidentId);

    public Task<OpResult> SaveArtifactAsync(string guid, Stream artifactStream);
}
