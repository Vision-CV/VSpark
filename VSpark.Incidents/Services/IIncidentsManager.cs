using VSpark.Incidents.Models.Dtos;

namespace VSpark.Incidents.Services;

public interface IIncidentsManager
{
    public Task AddIncidentAsync(IncidentDto incident);

    public Task ChangeIncidentStatus(Guid incidentId);

    public Task DeleteIncidentAsync(Guid incidentId);

    public Task SaveArtifactAsync(string guid, Stream artifactStream);
}
