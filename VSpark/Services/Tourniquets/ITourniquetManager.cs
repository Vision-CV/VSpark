namespace VSpark.Services.Tourniquets;

public interface ITourniquetManager
{
    public Task<bool> RequestUnlock(int id);

    public Task<bool> RequestLock(int id);
}
