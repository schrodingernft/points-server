using System.Linq;
using System.Threading.Tasks;
using PointsServer.Worker.Provider;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Services;

public interface IApplyStatusService
{
    Task ApplyStatusChangeAsync();
}

public class ApplyStatusService : IApplyStatusService, ISingletonDependency
{
    private readonly IPointsIndexerProvider _pointsIndexerProvider;
    private readonly IApplyStatusProvider _applyStatusProvider;

    public ApplyStatusService(IPointsIndexerProvider pointsIndexerProvider, IApplyStatusProvider applyStatusProvider)
    {
        _pointsIndexerProvider = pointsIndexerProvider;
        _applyStatusProvider = applyStatusProvider;
    }

    public async Task ApplyStatusChangeAsync()
    {
        var applyingList = await _applyStatusProvider.GetApplyingListAsync();
        var applyingDomains = applyingList.Select(i => i.Domain).ToList();
        var domainAppliedList= await _pointsIndexerProvider.GetDomainAppliedListAsync(applyingDomains);
        await _applyStatusProvider.BatchUpdateApplyStatusAsync(domainAppliedList);
    }
}