using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using VSpark.Incidents.Enums;
using VSpark.Incidents.Models.Adapters;
using VSpark.Incidents.Models.Configs;
using VSpark.Incidents.Models.Dtos;
using VSpark.Incidents.Models.Entities;
using VSpark.Incidents.Persistence;

namespace VSpark.Incidents.Services;

public class IncidentsManager(IDbContextFactory<EventDbContext> dbFactory, IOptions<ArtifactsConfig> artifactsConfig) : IIncidentsManager
{
    public async Task<OpResult> AddIncidentAsync(NewIncidentDto incident)
    {
        await using EventDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IncidentEntity newIncident = new ()
        {
            Id = Guid.NewGuid(),
            Type = (IncidentType)incident.Type,
            Status = IncidentStatus.Active,
            Priority = (IncidentPriority)incident.Priority
        };

        await dbContext.Incidents.AddAsync(newIncident);

        await dbContext.SaveChangesAsync();

        return OpResult.Success(newIncident.Id.ToString());
    }

    public async Task<OpResult> ChangeIncidentStatus(Guid incidentId, IncidentStatus status)
    {
        await using EventDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IncidentEntity? targetIncident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId);

        if (targetIncident == null)
            return OpResult.NotFound();

        targetIncident.Status = status;

        await dbContext.SaveChangesAsync();

        return OpResult.Success();
    }

    public async Task<OpResult> DeleteIncidentAsync(Guid incidentId)
    {
        await using EventDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IncidentEntity? targetIncident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId);

        if (targetIncident == null)
            return OpResult.NotFound();

        dbContext.Incidents.Remove(targetIncident);

        await dbContext.SaveChangesAsync();

        return OpResult.Success();
    }

    public async Task<IncidentDto?> GetIncidentAsync(Guid incidentId)
    {
        await using EventDbContext dbContext = await dbFactory.CreateDbContextAsync();

        IncidentEntity? targetIncident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId);

        if (targetIncident == null)
            return null;

        IncidentDto resultDto = new(incidentId, targetIncident.Type, targetIncident.Status, targetIncident.Priority);

        return resultDto;
    }

    public async Task<OpResult> SaveArtifactAsync(string guid, Stream artifactStream)
    {
        string? savingPath = artifactsConfig.Value.SavingPath;

        if (savingPath == null)
            return OpResult.InternalError();

        if (!Directory.Exists(savingPath))
            Directory.CreateDirectory(savingPath);

        string targetFilePath = Path.Combine(savingPath, $"{guid}.jpg");

        if (File.Exists(targetFilePath))
            return OpResult.BadRequest();

        await using FileStream artifactWriteStream = new FileStream(targetFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 2048, true);

        await artifactStream.CopyToAsync(artifactWriteStream);

        return OpResult.Success();
    }
}
