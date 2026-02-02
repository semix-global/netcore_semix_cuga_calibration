using Local.NoSQL.DB.Providers.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationCacheProvider
{
    bool TrySave(string? filePath = null);

    bool TryExport(string filePath);

    bool TryImport(string filePath);

    bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name);
}