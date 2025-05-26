using Newtonsoft.Json;

namespace Net.Utilities.Helper.File;

public static class FileHelper
{
    /// <summary>
    /// 获取包含路径的全文件名称, 不包含扩展名
    /// </summary>
    /// <param name="filePath">路径</param>
    /// <returns>包含路径的全文件名称</returns>
    public static string GetFileFullName(string filePath)
    {
        return $"{Path.GetDirectoryName(filePath)}\\{Path.GetFileNameWithoutExtension(filePath)}";
    }

    /// <summary>
    /// 仅在路径可能超过 MAX_PATH (260) 时转换为长路径格式（添加 \\?\ 前缀）
    /// <remarks>
    /// 操作系统                  文件名最大长度    路径最大长度<br/>
    /// Linux                    255              4096<br/>
    /// MAC                      255              1024<br/>
    /// Windows   不开启长文件名   255              260<br/>
    ///           开启长文件名     255            32767<br/>
    /// </remarks>
    /// </summary>
    /// <param name="filePath">原始文件路径</param>
    /// <returns>如果需要，返回长路径格式，否则返回原路径</returns>
    public static string GetEnsureLongPathSupport(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath.StartsWith(@"\\?\")) return filePath;

        var fullPath = Path.GetFullPath(filePath);

        if (fullPath.Length < 260) return fullPath;

        if (fullPath.StartsWith(@"\\"))
        {
            // 网络路径格式：\\?\UNC\server\share
            return @"\\?\UNC\" + fullPath[2..];
        }

        // 本地路径格式：\\?\C:\path
        return @"\\?\" + fullPath;
    }


    /// <summary>
    /// 文件是否存在, 存在则删除
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void DeleteFileIfExists(string filePath)
    {
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void CreateFile(string filePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
        DeleteFileIfExists(filePath);

        using var _ = System.IO.File.Create(filePath);
    }

    /// <summary>
    /// 消除文件名中的非法字符
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件名</returns>
    public static string RemoveInvalidFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    /// <summary>
    /// 验证文件是否为图片文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为图片文件</returns>
    public static bool IsImageFile(string filePath)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico"];
        var fileExtension = Path.GetExtension(filePath).ToLower();

        return validExtensions.Any(extension => fileExtension == extension);
    }

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="bytes">bytes</param>
    /// <param name="filePath">路径</param>
    public static void Save(byte[] bytes, string filePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
        DeleteFileIfExists(filePath);

        System.IO.File.WriteAllBytes(filePath, bytes); // 存在覆盖
    }

    #region 序列化

    /// <summary> 对象序列化 </summary>
    /// <param name="obj">需要序列化的对象</param>
    /// <param name="filePath">要保存的路径名</param>
    public static void SerializeOperate(object obj, string filePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);

        var serializeObject = JsonConvert.SerializeObject(obj, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, serializeObject); // 删除之前的, 覆盖写入
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="filePath">文件路径</param>
    /// <returns>对象</returns>
    public static T? DeserializeOperate<T>(string filePath)
    {
        return System.IO.File.Exists(filePath) == false
            ? default
            : JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(filePath));
    }

    #endregion 序列化
}