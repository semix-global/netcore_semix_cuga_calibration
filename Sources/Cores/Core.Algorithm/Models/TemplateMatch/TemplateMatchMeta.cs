using Core.Models.Helper;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using System.IO;

namespace Core.Algorithm.Models.TemplateMatch;

/// <summary>
/// 模板匹配元数据（sidecar JSON，随模板文件持久化的模板侧静态事实；与 .shm/.bmp 等模板派生文件并列，按 TemplateFilePath 关联）
/// </summary>
public sealed class TemplateMatchMeta
{
    /// <summary>
    /// 自定义模板中心相对 ROI 中心的偏移量（图像像素坐标系，Y 向下；零偏移 = 默认 ROI 中心语义）
    /// </summary>
    public Point CenterOffset { get; init; } = Point.Origin;

    /// <summary>
    /// 保存元数据到模板路径对应的 sidecar 文件（{TemplateFilePath}.json）
    /// </summary>
    /// <param name="templateFilePath">模板文件路径（基路径，无扩展名）</param>
    /// <param name="meta">元数据</param>
    public static void Save(string templateFilePath, TemplateMatchMeta meta)
    {
        var metaFilePath = CalibrationConstantsHelper.TemplatePathToTemplateMetaFilePath(templateFilePath);
        DirectoryHelper.CreateFileDirectoryIfNotExists(metaFilePath);
        FileHelper.DeleteFileIfExists(metaFilePath);
        File.WriteAllText(metaFilePath, JsonConvert.SerializeObject(meta, Formatting.Indented));
    }

    /// <summary>
    /// 从模板路径对应的 sidecar 文件读取元数据；文件不存在（存量模板）返回零偏移，行为与旧版完全一致
    /// </summary>
    /// <param name="templateFilePath">模板文件路径（基路径，无扩展名）</param>
    public static TemplateMatchMeta Load(string templateFilePath)
    {
        var metaFilePath = CalibrationConstantsHelper.TemplatePathToTemplateMetaFilePath(templateFilePath);
        return File.Exists(metaFilePath) == false
            ? new TemplateMatchMeta()
            : JsonConvert.DeserializeObject<TemplateMatchMeta>(File.ReadAllText(metaFilePath)) ?? new TemplateMatchMeta();
    }
}

/// <summary>
/// 匹配位姿：匹配实例相对模板原位的相似变换（角度 + 缩放）。缩放扩展时仅需在引擎适配处传入实际 scale，本结构公式不变
/// </summary>
/// <param name="angle">匹配角度（弧度；中立口径：图像像素坐标系（Y 向下）下屏幕逆时针为正）</param>
/// <param name="scale">匹配缩放（相对模板原位，默认 1）</param>
public readonly struct TemplateMatchPose(double angle, double scale = 1d)
{
    public double Angle { get; } = angle;

    public double Scale { get; } = scale;

    /// <summary>
    /// 把模板内偏移向量按位姿变换到匹配实例坐标系：s·R(θ)·d
    /// </summary>
    public Vector Transform(Point offset) => new(
        Scale * (offset.X * Math.Cos(Angle) + offset.Y * Math.Sin(Angle)),
        Scale * (offset.Y * Math.Cos(Angle) - offset.X * Math.Sin(Angle)));
}
