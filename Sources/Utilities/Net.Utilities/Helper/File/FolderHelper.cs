using System.Text.RegularExpressions;

namespace Net.Utilities.Helper.File;

public static class FolderHelper
{
    /// <summary>
    /// copy一个文件夹副本,文件夹已存在时，生成带索引后缀的副本路径
    /// </summary>
    /// <param name="sourceFolder">base文件夹路径</param>
    /// <param name="targetDirectory">保存的文件夹路径</param>
    /// <returns>带索引后缀的文件夹路径</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GenerateIndexedDirectoryPath(string sourceFolder, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        var baseName = Path.GetFileName(sourceFolder);
        var fileExtension = Path.GetExtension(sourceFolder);
        var existingFiles = new SortedDictionary<int, string>();
        var pattern = $@"^{Regex.Escape(baseName)}(?:\((\d+)\))?{Regex.Escape(fileExtension)}$";

        // 扫描所有可能的相关文件
        foreach (var dir in Directory.GetDirectories(targetDirectory))
        {
            var dirName = Path.GetFileName(dir);

            // 匹配两种模式：基础名称(数字).扩展名 或 纯基础名称.扩展名（视为索引0）
            var match = Regex.Match(dirName, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var index = 0;
                if (match.Groups[1].Success)
                {
                    if (!int.TryParse(match.Groups[1].Value, out index)) continue;
                }

                existingFiles.Add(index, dirName);
            }
        }

        // 生成连续索引序列
        var expectedIndex = 0;
        foreach (var kvp in existingFiles.OrderBy(x => x.Key))
        {
            if (kvp.Key > expectedIndex) break;
            expectedIndex++;
        }

        // 构建新文件名
        var newFileName = expectedIndex == 0 ? $"{baseName}{fileExtension}" : $"{baseName}({expectedIndex}){fileExtension}";

        var newFilePath = Path.Combine(targetDirectory, newFileName);

        // 冲突检测
        if (System.IO.File.Exists(newFilePath))
            throw new InvalidOperationException("The destination file already exists!");

        return newFilePath;
    }
}