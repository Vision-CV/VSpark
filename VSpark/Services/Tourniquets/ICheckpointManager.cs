namespace VSpark.Services.Tourniquets;

public interface ICheckpointManager
{
    public Task<bool> RequestUnlock(int id);

    public Task<bool> RequestLock(int id);
}
