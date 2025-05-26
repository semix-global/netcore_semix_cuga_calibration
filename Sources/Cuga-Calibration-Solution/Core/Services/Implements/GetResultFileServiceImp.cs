using CugaCalibration.Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.File;
using System.IO;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(IGetResultFileService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class GetResultFileServiceImp : IGetResultFileService
{
    /// <summary>
    /// result文件全路径
    /// </summary>
    private string _filePath = string.Empty;

    /// <summary>
    /// result文件全路径
    /// </summary>
    /// <param name="filePath"></param>
    public void SetResultFilePath(string filePath)
    {
        _filePath = filePath;
    }

    public bool TryGet<T>(out T obj) where T : class
    {
        try
        {
            var tryGetObj = FileHelper.DeserializeOperate<T>(_filePath);
            obj = tryGetObj!;

            return tryGetObj is not null;
        }
        catch (Exception)
        {
            throw new ArgumentNullException(nameof(T));
        }
    }

    public bool TrySave<T>(T obj) where T : class
    {
        try
        {
            FileHelper.SerializeOperate(obj, _filePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool TrySaveBackUp<T>(T obj) where T : class
    {
        try
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                return false;
            }

            var directoryPath = Path.Combine(Path.GetDirectoryName(_filePath), "Backup");
            DirectoryHelper.CreateDirectoryIfNotExists(directoryPath);
            var backupFilePath = Path.Combine(directoryPath, Path.GetFileName(_filePath));
            FileHelper.SerializeOperate(obj, backupFilePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}