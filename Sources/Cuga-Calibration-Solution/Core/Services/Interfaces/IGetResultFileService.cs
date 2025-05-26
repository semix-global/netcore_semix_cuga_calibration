namespace CugaCalibration.Core.Services.Interfaces;

public interface IGetResultFileService
{
    /// <summary>
    /// result文件全路径
    /// </summary>
    /// <param name="filePath"></param>
    void SetResultFilePath(string filePath);

    /// <summary>
    /// 根据默认缓存对象的文件名, 获取缓存对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">输出缓存对象</param>
    /// <returns>是否成功</returns>
    bool TryGet<T>(out T obj) where T : class;

    /// <summary>
    /// 保存result文件
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">输出缓存对象</param>
    /// <returns>是否成功</returns>
    bool TrySave<T>(T obj) where T : class;

    /// <summary>
    /// 保存result备份文件
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">输出缓存对象</param>
    /// <returns>是否成功</returns>
    bool TrySaveBackUp<T>(T obj) where T : class;
}