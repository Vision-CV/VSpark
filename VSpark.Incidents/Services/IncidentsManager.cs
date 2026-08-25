using VSpark.Incidents.Models.Dtos;

namespace VSpark.Incidents.Services;

public class IncidentsManager : IIncidentsManager
{
    public Task AddIncidentAsync(IncidentDto incident)
    {
        throw new NotImplementedException();
    }

    public Task ChangeIncidentStatus(Guid incidentId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteIncidentAsync(Guid incidentId)
    {
        throw new NotImplementedException();
    }

    public async Task SaveArtifactAsync(string guid, MemoryStream artifactStream)
    {
        if (!Directory.Exists("Artifacts"))
            Directory.CreateDirectory("Artifacts");

        string targetFilePath = Path.Combine("Artifacts", $"{guid}.jpg");

        if (File.Exists(targetFilePath))
            throw new Exception("Artifact with the same ID already exists.");

        await using FileStream artifactWriteStream = new FileStream(targetFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 2048, true);

        await artifactStream.CopyToAsync(artifactWriteStream);
    }

    public Task SaveArtifactAsync(string guid, Stream artifactStream)
    {
        throw new NotImplementedException();
    }
}
