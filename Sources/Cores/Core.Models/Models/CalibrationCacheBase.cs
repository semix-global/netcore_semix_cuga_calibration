using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models;

public partial class CalibrationCacheBase : ObservableCacheBase
{
}

public abstract class CalibrationCacheBase<T> : CalibrationCacheBase, ICloneable<T> where T : CalibrationCacheBase<T>, new()
{
    public static readonly T Default = new();

    public abstract T Clone();
}