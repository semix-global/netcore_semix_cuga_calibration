using algocv_sharp;
using AwesomeAssertions;
using HalconDotNet;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using System.IO;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Bridge;

/// <summary>
/// 显示域算子双实现等价测试（准入门禁）：同一资产分别经 Halcon 实现（Net.Utilities.Calibration.BitmapImageExtensions）
/// 与 AlgoCSharp 实现（BitmapImageAlgoImageOperations）执行同名方法，逐像素/数值比对。
/// <para>等价通过的方法（几何四件套 + GetIntensity + GetHorizontalProjects）经逐像素/逐值断言接入 BitmapImage 同名分流（显式 enum 参数 + 全局默认）；
/// ToLinearImage/SaveImage 按 2026-10-24 裁定亦进同名分流，以容差断言锁定差异界（ToLinearImage ±1 灰度系统差、SaveImage 拉伸取整视觉近似差），代码注释【容差标记】。</para>
/// <para>2026-10-23 裁定：GetIntensity/GetHorizontalProjects 有生产消费，恢复自写等价实现（TODO 标记 algo-未完成，待 algolib 原生算子就绪后替换）；
/// GetMaxMinGrayValue/SubImage 生产零消费，不自写复刻，VM 调用点显式调用 Halcon 实现。</para>
/// </summary>
public class BitmapImageOperationsEquivalenceTest(ITestOutputHelper testOutputHelper)
{
    private const string TiffPath = @"Assets\DFPixelSize\test\OI-S10_20260613.tiff";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RotateCounterClockwise90Degree_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        using var halconResult = image.RotateCounterClockwise90Degree();
        using var algoResult = BitmapImageAlgoImageOperations.RotateCounterClockwise90Degree(image);

        AssertPixelEqual(halconResult, algoResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalFlip_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        using var halconResult = image.HorizontalFlip();
        using var algoResult = BitmapImageAlgoImageOperations.HorizontalFlip(image);

        AssertPixelEqual(halconResult, algoResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VerticalFlip_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        using var halconResult = image.VerticalFlip();
        using var algoResult = BitmapImageAlgoImageOperations.VerticalFlip(image);

        AssertPixelEqual(halconResult, algoResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToROI_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        var rect = new Rect(10d, 20d, 300d, 400d);
        using var halconResult = image.ToROI(rect);
        using var algoResult = BitmapImageAlgoImageOperations.ToROI(image, rect);

        AssertPixelEqual(halconResult, algoResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIntensity_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        var (halconAverage, halconDeviation) = image.GetIntensity();
        var (algoAverage, algoDeviation) = BitmapImageAlgoImageOperations.GetIntensity(image);

        testOutputHelper.WriteLine($"8bit={is8Bit}: Halcon ({halconAverage:0.######}, {halconDeviation:0.######}), Algo ({algoAverage:0.######}, {algoDeviation:0.######})");

        algoAverage.Should().BeApproximately(halconAverage, Math.Abs(halconAverage) * 1e-9 + 1e-6);
        algoDeviation.Should().BeApproximately(halconDeviation, Math.Abs(halconDeviation) * 1e-9 + 1e-6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetHorizontalProjects_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        var halconProjects = image.GetHorizontalProjects();
        var algoProjects = BitmapImageAlgoImageOperations.GetHorizontalProjects(image);

        algoProjects.Should().Equal(halconProjects);
    }

    [Fact]
    public void ToLinearImage_ShouldBeWithinTolerance()
    {
        const string rawPath = @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw";

        using var hImage = RAWImageFactory.CreateImage(rawPath, false);
        using var image = hImage.ToBitmapImage(12);
        image.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Gray12);

        using var halconResult = image.ToLinearImage(AlgorithmEngineTypeEnum.HAlgorithm);
        using var algoResult = image.ToLinearImage(AlgorithmEngineTypeEnum.AlgoCSharp);

        halconResult.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Gray12);
        algoResult.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Gray12);

        // 【容差标记】algo 版用原生 ImageProc.LogToLinear（float32 取整），Halcon 版为双精度截断 LUT，存在 ±1 灰度系统差：
        // 容差断言锁定差异界，防止 algolib 实现漂移导致差异扩大（2026-10-24 裁定同名分流）
        AssertPixelWithinTolerance(halconResult, algoResult, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveImage_ShouldBeEquivalent(bool is8Bit)
    {
        using var image = CreateTestBitmapImage(is8Bit);
        var halconPath = Path.Combine(Path.GetTempPath(), $"equivalence_halcon_{Guid.NewGuid():N}.png");
        var algoPath = Path.Combine(Path.GetTempPath(), $"equivalence_algo_{Guid.NewGuid():N}.png");
        try
        {
            image.SaveImage(halconPath, AlgorithmEngineTypeEnum.HAlgorithm);
            image.SaveImage(algoPath, AlgorithmEngineTypeEnum.AlgoCSharp);

            using var halconSaved = new HImage(halconPath);
            using var algoSaved = new HImage(algoPath);

            using var halconBitmap = halconSaved.ToBitmapImage();
            using var algoBitmap = algoSaved.ToBitmapImage();

            // 【容差标记】Halcon scale_image_max 黑盒取整与 algo 原生 ScaleImageMax（float 取整）存在数值差（视觉近似）：
            // png 无损保存读回后逐像素容差断言锁定差异界（2026-10-24 裁定同名分流，不强行位级对齐）
            AssertPixelWithinTolerance(halconBitmap, algoBitmap, 2);
        }
        finally
        {
            File.Delete(halconPath);
            File.Delete(algoPath);
        }
    }

    /// <summary>测试资产：16bit = tiff 直读；8bit = 同图线性拉伸到 8bit（保证两种位深同源）.</summary>
    private static BitmapImage CreateTestBitmapImage(bool is8Bit)
    {
        using var hImage = new HImage(TiffPath);

        return is8Bit
            ? hImage.ScaleImageMax().ToBitmapImage()
            : hImage.ToBitmapImage(16);
    }

    /// <summary>逐像素容差断言：结构（尺寸/类型/通道）一致 + |像素差| ≤ tolerance（失败时定位首个超界像素）.</summary>
    private static void AssertPixelWithinTolerance(BitmapImage halconImage, BitmapImage algoImage, int tolerance)
    {
        using var halconAlgoImage = halconImage.ToAlgoCVImage();
        using var algoAlgoImage = algoImage.ToAlgoCVImage();

        halconAlgoImage.Width.Should().Be(algoAlgoImage.Width);
        halconAlgoImage.Height.Should().Be(algoAlgoImage.Height);
        halconAlgoImage.DataType.Should().Be(algoAlgoImage.DataType);
        halconAlgoImage.Channels.Should().Be(algoAlgoImage.Channels);

        if (halconAlgoImage.DataType == ImageDataType.UInt8)
        {
            var halconPixels = halconAlgoImage.ToSpan<byte>();
            var algoPixels = algoAlgoImage.ToSpan<byte>();
            AssertSpanWithinTolerance(halconPixels, algoPixels, tolerance);
        }
        else
        {
            var halconPixels = halconAlgoImage.ToSpan<ushort>();
            var algoPixels = algoAlgoImage.ToSpan<ushort>();
            AssertSpanWithinTolerance(halconPixels, algoPixels, tolerance);
        }
    }

    private static void AssertSpanWithinTolerance<T>(Span<T> expected, Span<T> actual, int tolerance) where T : struct, IEquatable<T>, IConvertible
    {
        var maxDiff = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            var diff = Math.Abs(expected[i].ToInt32(null) - actual[i].ToInt32(null));
            if (diff > maxDiff) maxDiff = diff;
            if (diff <= tolerance) continue;

            Assert.Fail($"首个超界像素 index={i}: halcon={expected[i]}, algo={actual[i]}, diff={diff} > tolerance={tolerance} (maxDiff={maxDiff})");
        }
    }

    /// <summary>逐像素等价断言：结构（尺寸/类型/通道）一致 + 像素位级相等（失败时定位首个差异）.</summary>
    private static void AssertPixelEqual(BitmapImage halconImage, BitmapImage algoImage)
    {
        using var halconAlgoImage = halconImage.ToAlgoCVImage();
        using var algoAlgoImage = algoImage.ToAlgoCVImage();

        halconAlgoImage.Width.Should().Be(algoAlgoImage.Width);
        halconAlgoImage.Height.Should().Be(algoAlgoImage.Height);
        halconAlgoImage.DataType.Should().Be(algoAlgoImage.DataType);
        halconAlgoImage.Channels.Should().Be(algoAlgoImage.Channels);

        if (halconAlgoImage.DataType == ImageDataType.UInt8)
        {
            var halconPixels = halconAlgoImage.ToSpan<byte>();
            var algoPixels = algoAlgoImage.ToSpan<byte>();
            AssertSpanEqual(halconPixels, algoPixels);
        }
        else
        {
            var halconPixels = halconAlgoImage.ToSpan<ushort>();
            var algoPixels = algoAlgoImage.ToSpan<ushort>();
            AssertSpanEqual(halconPixels, algoPixels);
        }
    }

    private static void AssertSpanEqual<T>(Span<T> expected, Span<T> actual) where T : IEquatable<T>
    {
        if (expected.SequenceEqual(actual)) return;

        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i].Equals(actual[i])) continue;

            Assert.Fail($"首个差异像素 index={i}: halcon={expected[i]}, algo={actual[i]}");
        }
    }
}
