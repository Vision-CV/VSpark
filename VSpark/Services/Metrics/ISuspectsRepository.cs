using VSpark.Models.Data;

namespace VSpark.Services.Metrics;

public interface ISuspectsRepository
{
    public Task<Guid?> TryOpenCase(SuspiciousActivityData data);

    public Task<bool> TryCloseCase(Guid caseId, bool shouldBeSaved = false);
}
