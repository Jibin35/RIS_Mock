using System.Collections.Concurrent;
using RisLearning.Shared;

namespace RisLearning.Server;

public sealed class RadiologyStore
{
    private readonly ConcurrentDictionary<string, RadiologyStudy> _studies = new();

    public void Add(RadiologyStudy study)
    {
        _studies[study.OpenEmrOrderId] = study;
    }

    public IReadOnlyCollection<RadiologyStudy> GetAll()
    {
        return _studies.Values.ToList();
    }

    public IReadOnlyCollection<RadiologyStudy> GetActive()
    {
        return _studies.Values
            .Where(x => x.Status == "Active")
            .ToList();
    }
}