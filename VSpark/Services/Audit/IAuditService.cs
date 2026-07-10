namespace VSpark.Services.Audit;

public interface IAuditor
{
    public Task SaveIncidentAsync();

    public Task SaveSuspectAsync();

    public Task GetIncidentAsync(Guid id);

    public Task GetSuspectAsync(Guid id);
}
