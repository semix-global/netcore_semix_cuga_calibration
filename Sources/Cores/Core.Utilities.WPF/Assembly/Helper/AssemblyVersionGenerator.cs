using CommunityToolkit.Diagnostics;
using Core.Utilities.WPF.ApplicationAbout.Model;
using Core.Utilities.WPF.Assembly.Model;
using System.IO;

namespace Core.Utilities.WPF.Assembly.Helper;

/// <summary>
/// 程序集版本信息生成器
/// </summary>
public static class AssemblyVersionGenerator
{
    /// <summary>
    /// 生成程序集版本信息JSON文件
    /// </summary>
    /// <returns>生成的JSON对象</returns>
    public static VersionInfo GenerateAssemblyVersionsJson(ApplicationInfo applicationInfo)
    {
        if (!Directory.Exists(applicationInfo.OutPutPath)) ThrowHelper.ThrowArgumentException("Publish output path does not exist!");
        var assemblyInfoList = new List<AssemblyInfo>();

        // 获取发布目录中的所有文件
        var publishedFiles = Directory.GetFiles(applicationInfo.OutPutPath, "*.*", SearchOption.AllDirectories);

        // 收集DLL和EXE文件
        var dllFiles = publishedFiles.Where(f => Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)).ToList();
        var exeFiles = publishedFiles.Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase)).ToList();

        if (dllFiles.Count == 0 || exeFiles.Count == 0)
            ThrowHelper.ThrowArgumentException("No DLL or EXE files found in the publish output directory!");

        var allAssemblies = dllFiles.Concat(exeFiles).ToList();

        // 收集每个程序集的版本信息
        foreach (var file in allAssemblies)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(file);

                assemblyInfoList.Add(new AssemblyInfo
                {
                    FileName = Path.GetFileName(file),
                    FullPath = file,
                    AssemblyVersion = fileVersionInfo.FileVersion ?? string.Empty,
                    ProductVersion = fileVersionInfo.ProductVersion ?? string.Empty,
                    Company = fileVersionInfo.CompanyName ?? string.Empty,
                    Description = fileVersionInfo.FileDescription ?? string.Empty,
                    SizeBytes = fileInfo.Length,
                    SizeKB = Math.Round(fileInfo.Length / 1024d, 2),
                    SizeMB = Math.Round(fileInfo.Length / (1024 * 1024d), 2),
                    LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastModifiedUTC = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                // 如果无法获取版本信息，添加基本信息
                var fileInfo = new FileInfo(file);
                assemblyInfoList.Add(new AssemblyInfo
                {
                    FileName = Path.GetFileName(file),
                    FullPath = file,
                    AssemblyVersion = string.Empty,
                    ProductVersion = string.Empty,
                    Company = string.Empty,
                    Description = "Get version faied:" + ex.Message,
                    SizeBytes = fileInfo.Length,
                    SizeKB = Math.Round(fileInfo.Length / 1024d, 2),
                    SizeMB = Math.Round(fileInfo.Length / (1024 * 1024d), 2),
                    LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastModifiedUTC = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }
        }

        // 创建JSON结构
        var jsonOutput = applicationInfo.VersionInfo.Clone();
        jsonOutput.Metadata.GeneratedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        jsonOutput.Metadata.TotalFiles = publishedFiles.Length;
        jsonOutput.Metadata.AssemblyCount = assemblyInfoList.Count;
        jsonOutput.Metadata.DLLCount = dllFiles.Count;
        jsonOutput.Metadata.ExecutableCount = exeFiles.Count;
        jsonOutput.Assemblies = [.. assemblyInfoList.OrderBy(a => a.FileName)];

        return jsonOutput;
    }
}