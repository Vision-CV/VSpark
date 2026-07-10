using System.Collections.Concurrent;
using VSpark.Models.Data;
using VSpark.Services.Audit;

namespace VSpark.Services.Metrics;

public class SuspectsRepository(IAuditor auditor) : ISuspectsRepository
{
    private ConcurrentDictionary<Guid, SuspiciousActivityData> _currentSuspects = new ();

    public async Task<bool> TryCloseCase(Guid caseId, bool shouldBeSaved = false)
    {
        if (!_currentSuspects.TryRemove(caseId, out SuspiciousActivityData? target))
            return false;

        if (shouldBeSaved)
            await auditor.SaveSuspectAsync();

        return true;
    }

    public async Task<Guid?> TryOpenCase(SuspiciousActivityData data)
    {
        Guid guid = Guid.NewGuid();

        if (!_currentSuspects.TryAdd(guid, data))
            return null;

        return guid;
    }
}
