// #define HAlgorithmTest

using AwesomeAssertions;
using HalconDotNet;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using System.Globalization;
using System.IO;
using System.Text;
using Point = Net.Utilities.Models.Geometries.Point;

#if HAlgorithmTest
using System.Diagnostics;
using Net.Utilities.Helpers.Helpers.Files;
#endif

namespace CugaCalibrationUnitTest;

public sealed class HAlgorithmTest
{
    private static readonly Algorithm Algorithm = new();
    private static readonly string[] First = ["Point.X"];

    [Fact]
    public void Test()
    {
        using var lineImage = RAWImageFactory.CreateImage(@"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw", true);
        using var expectedLinearImage = RAWImageFactoryV2.CreateImage(@"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw", true);

        using var saveResultImageHTuple = new HTuple(1);
        Algorithm.DarkPixSizeCal(expectedLinearImage, saveResultImageHTuple, out var expectedDrawImageHObject, out var expectedMeanTuple);
        using var _3 = expectedDrawImageHObject;
        using var _4 = expectedMeanTuple;

        Algorithm.DarkPixSizeCal(lineImage, saveResultImageHTuple, out var drawImageHObject, out var meanTuple);
        using var _5 = drawImageHObject;
        using var _6 = meanTuple;

        meanTuple.D.Should().Be(expectedMeanTuple.D);

#if HAlgorithmTest
        var imageFullPath = Path.GetFullPath($"{nameof(HAlgorithmTest)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        using var drawImage = new HImage(drawImageHObject);
        drawImage.Save(imageFullPath);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = imageFullPath,
            UseShellExecute = true
        });
#endif
    }

    [Fact]
    public void YProjectTest()
    {
        // 读图集合目录: Assets\Data\YGhost\Pic, 文件名格式 index_channelIndex*
        var picDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\YGhost\Pic");

        // 解析并排序: 先用 index 排序, 再用 channelIndex 排序
        var imageRecords = Directory.EnumerateFiles(picDirectory, "*.raw")
            .Select(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var parts = name.Split('_');
                var index = int.Parse(parts[0]);
                var channelIndex = int.Parse(parts[1]);
                return (Path: path, Name: name, Index: index, ChannelIndex: channelIndex, Parts: parts);
            })
            .OrderBy(t => t.Index)
            .ThenBy(t => t.ChannelIndex)
            .ToArray();

        // 遍历计算每张图的 y 方向投影
        var projections = new List<(string Header, Point[] Points)>();
        foreach (var (path, _, _, channelIndex, parts) in imageRecords)
        {
            using var hImage = RAWImageFactory.CreateImage(path, false);

            // 计算y方向的投影
            var yProjects = hImage.GetHorizontalProjects();
            var yProjectPoints = yProjects.Select((t, i) => new Point(i, t)).ToArray();

            // 列名: 图像名称中 channelIndex 数字前增加 "CH"
            parts[1] = $"CH{channelIndex}";
            var header = string.Join("_", parts);

            projections.Add((header, yProjectPoints));
        }

        // 生成 CSV: 横坐标=图像名称(CH), 纵坐标=Point.X, cell=Point.Y
        var maxCount = projections.Count == 0 ? 0 : projections.Max(t => t.Points.Length);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", First.Concat(projections.Select(t => t.Header))));
        for (var i = 0; i < maxCount; i++)
        {
            var temp = i;
            var cells = new[] { i.ToString() }.Concat(projections.Select(t => temp < t.Points.Length ? t.Points[temp].Y.ToString(CultureInfo.InvariantCulture) : string.Empty));
            sb.AppendLine(string.Join(",", cells));
        }

        // 保存在生成路径下
        var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{nameof(YProjectTest)}.csv");
        File.WriteAllText(csvPath, sb.ToString());
    }
}