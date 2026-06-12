using VSpark.Models.Data;

namespace VSpark.Services;

public interface IIncidentsRepository
{
    public Task<IncidentData?> TryGetIncidentAsync(string guid);

    public Task<bool> TrySaveIncidentAsync(IncidentData incident, byte[] artifact);

    public Task<bool> TryDeleteIncidentAsync(string guid);

    public Task<bool> TryUpdateIncidentAsync(string guid, IncidentData newData);
}
