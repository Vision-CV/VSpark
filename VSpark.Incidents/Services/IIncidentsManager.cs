namespace VSpark.Incidents.Services;

public interface IIncidentsManager
{
    public Task AddIncidentAsync(Stream artifactStream);

    public Task ChangeIncidentStatus(Guid incidentId);

    public Task DeleteIncidentAsync();
}
