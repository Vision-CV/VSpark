using VSpark.Models.Data;
using VSpark.Persistence;

namespace VSpark.Services.Metrics;

public interface IIncidentsRepository
{
    public Task<IncidentData?> TryGetIncidentAsync(string guid);

    public Task<IncidentData?> TryGetIncidentAsync(string guid, SparkDbContext dbContext);

    public Task<bool> TrySaveIncidentAsync(IncidentData incident, byte[] artifact);

    public Task<bool> TryDeleteIncidentAsync(string guid);

    public Task<bool> TryUpdateIncidentAsync(string guid, IncidentData newData);
}
