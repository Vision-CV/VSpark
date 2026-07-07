using Microsoft.EntityFrameworkCore;

using VSpark.Data;
using VSpark.Models.Data;
using VSpark.Models.Extensions;

namespace VSpark.Services.Implementation;

public class IncidentsRepository : IIncidentsRepository
{
    private IDbContextFactory<SparkDbContext> _dbFactory;

    private string _artifactsFolder;

    public IncidentsRepository(IDbContextFactory<SparkDbContext> dbFactory, IWebHostEnvironment env)
    {
        _dbFactory = dbFactory;

        _artifactsFolder = Path.Combine("/app/wwwroot/artifacts", "artifacts");
    }

    public async Task<IncidentData?> TryGetIncidentAsync(string guid)
    {
        using SparkDbContext metricsDbContext = await _dbFactory.CreateDbContextAsync();

        if (!Guid.TryParse(guid, out Guid targetGuid))
            return null;

        return await metricsDbContext.Incidents.FirstOrDefaultAsync(x => x.Guid == targetGuid);
    }

    public async Task<IncidentData?> TryGetIncidentAsync(string guid, SparkDbContext dbContext)
    {
        if (!Guid.TryParse(guid, out Guid targetGuid))
            return null;

        return await dbContext.Incidents.FirstOrDefaultAsync(x => x.Guid == targetGuid);
    }

    public async Task<bool> TrySaveIncidentAsync(IncidentData incident, byte[] artifact)
    {
        string? artifactPath = await TryAddArtifact(incident.Guid.ToString(), artifact);

        if (artifactPath == null)
            return false;

        incident.Artifact = artifactPath;

        bool isIncidentSaved = await TryAddIncident(incident);

        if (!isIncidentSaved)
            return false;

        return true;
    }

    public async Task<bool> TryUpdateIncidentAsync(string guid, IncidentData newData)
    {
        using SparkDbContext metricsDbContext = await _dbFactory.CreateDbContextAsync();

        IncidentData? targetIncident = await TryGetIncidentAsync(guid, metricsDbContext);

        if (targetIncident == null)
            return false;

        targetIncident.MergeWith(newData);

        await metricsDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> TryDeleteIncidentAsync(string guid)
    {
        using SparkDbContext metricsDbContext = await _dbFactory.CreateDbContextAsync();

        IncidentData? targetIncident = await TryGetIncidentAsync(guid, metricsDbContext);

        if (targetIncident == null)
            return false;

        metricsDbContext.Remove(targetIncident);

        await metricsDbContext.SaveChangesAsync();

        string targetArtifactPath = Path.Combine(_artifactsFolder, $"{guid}.jpg");

        if (File.Exists(targetArtifactPath))
            File.Delete(targetArtifactPath);

        return true;
    }

    private async Task<string?> TryAddArtifact(string guid, byte[] artifact)
    {
        if (!Directory.Exists(_artifactsFolder))
            Directory.CreateDirectory(_artifactsFolder);

        string artifactPath = Path.Combine(_artifactsFolder, $"{guid}.jpg");

        if (File.Exists(artifactPath))
            return null;

        using FileStream targetArtifactStream = new FileStream(artifactPath, FileMode.Create, FileAccess.Write, FileShare.None, 8096, true);

        if (targetArtifactStream == null)
            return null;

        await targetArtifactStream.WriteAsync(artifact, 0, artifact.Length);

        await targetArtifactStream.FlushAsync();

        return artifactPath;
    }

    private async Task<bool> TryAddIncident(IncidentData incident)
    {
        using SparkDbContext metricsDbContext = await _dbFactory.CreateDbContextAsync();

        if (await metricsDbContext.Incidents.AnyAsync(x => x.Guid == incident.Guid)) 
            return false;

        await metricsDbContext.Incidents.AddAsync(incident);

        return true;
    }
}
