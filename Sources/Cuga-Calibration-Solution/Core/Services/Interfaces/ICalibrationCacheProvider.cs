using Core.Models.Models.Common.Version;
using Local.SQL.Cache.Providers.Services.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationCacheProvider
{
    Task<bool> TrySaveAsync(CalibrationVersionDTO calibrationVersionDTO, CancellationToken cancellationToken);

    Task<(bool IsSuccess, string Message)> TryExportAsync(string filePath, CancellationToken cancellationToken);

    Task<(bool IsSuccess, string Message)> TryImportAsync(string filePath, CancellationToken cancellationToken);

    bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name, CancellationToken token);
}