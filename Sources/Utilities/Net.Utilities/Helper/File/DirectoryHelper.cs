namespace Net.Utilities.Helper.File;

public static class DirectoryHelper
{
    /// <summary>
    /// 创建路径
    /// 如果目标文件夹不存在，它会自动创建；如果目标文件夹已经存在，它会忽略这个操作。同时，它还会沿途创建所有不存在的文件夹（类似 mkdir 的 -p 参数
    /// </summary>
    /// <param name="directoryPath">路径</param>
    public static void CreateDirectoryIfNotExists(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// 创建文件路径
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void CreateFileDirectoryIfNotExists(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath);

        // Path.GetDirectoryName 方法有可能返回空。这一情况通常发生在文件位于根目录的情况（例如 Windows 的 C:\，或 Unix 的 /）
        if (directoryPath is not null) Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// 消除文件夹名中的非法字符
    /// </summary>
    /// <param name="directoryName">文件夹名</param>
    /// <returns>文件夹名</returns>
    public static string RemoveInvalidDirectoryName(string directoryName)
    {
        return string.Join("_", directoryName.Split(Path.GetInvalidPathChars()));
    }

#if NET
    /// <summary>
    /// 拷贝文件夹
    /// </summary>
    /// <param name="sourceFolderPath">源文件夹</param>
    /// <param name="targetFolderPath">目标文件夹</param>
    public static void CopyDirectory(string sourceFolderPath, string targetFolderPath)
    {
        Directory.CreateDirectory(targetFolderPath);

        foreach (var filePath in Directory.GetFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceFolderPath, filePath);
            var targetFilePath = Path.Combine(targetFolderPath, relativePath);
            var subTargetFolderPath = Path.GetDirectoryName(targetFilePath);
            if (subTargetFolderPath is not null) Directory.CreateDirectory(subTargetFolderPath);
            System.IO.File.Copy(filePath, targetFilePath);
        }
    }
#endif
}