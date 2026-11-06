using AwesomeAssertions;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using System.IO;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.RAW;

/// <summary>
/// RAWImageFactoryV3 契约测试：以 Halcon 实现的 RAWImageFactoryV2 为金标准，锁定方向不变量与等价性。
/// <para>I1 尺寸等价；I2 log 域逐像素等价；I3 竖图零重排互证；I4 线性域 ±1 精度差界内等价（原生 LogToLinear float32 取整 vs V2 双精度截断）；I8 DTO 调用方视角等价。</para>
/// </summary>
public class RAWImageFactoryV3Test(ITestOutputHelper testOutputHelper)
{
    private const string RawPath = @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw";

    [Theory]
    [InlineData(RawPath)]
    public void GetSize_ShouldBeEqualToV2(string filePath)
    {
        var v2 = RAWImageFactoryV2.GetSize(filePath);
        var v3 = RAWImageFactoryV3.GetSize(filePath);

        testOutputHelper.WriteLine($"V2: {v2.Size}, body[{v2.BodyBytesStartIndex}, {v2.BodyBytesLength}]");
        testOutputHelper.WriteLine($"V3: {v3.Size}, body[{v3.BodyBytesStartIndex}, {v3.BodyBytesLength}]");

        v3.Size.Should().Be(v2.Size);
        v3.BodyBytesStartIndex.Should().Be(v2.BodyBytesStartIndex);
        v3.BodyBytesLength.Should().Be(v2.BodyBytesLength);
    }

    [Theory]
    [InlineData(RawPath)]
    public void CreateImage_LogDomain_ShouldBePixelEqualToV2(string filePath) => AssertPixelEqualToV2(filePath, false);

    [Theory]
    [InlineData(RawPath)]
    public void CreateImage_LinearDomain_ShouldBeWithinToleranceOfV2(string filePath) => AssertPixelEqualToV2(filePath, true);

    [Theory]
    [InlineData(RawPath)]
    public void CreateProtocolImage_ShouldBeTransposeOfCreateImage(string filePath)
    {
        using var protocolImage = RAWImageFactoryV3.CreateProtocolImage(filePath);
        using var forwardImage = RAWImageFactoryV3.CreateImage(filePath, false);

        // 竖图与物理正向图互为转置：尺寸交换
        protocolImage.Width.Should().Be(forwardImage.Height);
        protocolImage.Height.Should().Be(forwardImage.Width);

        var protocolPixels = protocolImage.ToSpan<ushort>();
        var forwardPixels = forwardImage.ToSpan<ushort>();

        var mismatchCount = 0;
        for (var r = 0; r < protocolImage.Height; r++)
        {
            for (var c = 0; c < protocolImage.Width; c++)
            {
                if (protocolPixels[r * protocolImage.Width + c] == forwardPixels[c * forwardImage.Width + r]) continue;

                mismatchCount++;
                if (mismatchCount <= 5)
                    testOutputHelper.WriteLine($"mismatch @({r}, {c}): protocol={protocolPixels[r * protocolImage.Width + c]}, forward({c}, {r})={forwardPixels[c * forwardImage.Width + r]}");
            }
        }

        testOutputHelper.WriteLine($"mismatchCount={mismatchCount}");
        mismatchCount.Should().Be(0);
    }

    [Theory]
    [InlineData(RawPath)]
    public void DarkFieldDtoGetImage_ShouldBePixelEqualToV2Chain(string filePath)
    {
        // 调用方视角：DTO.GetImage()（V3 底座）与手工 V2 链（V2.CreateImage → hImage.ToBitmapImage(12)）产出应逐像素、位深一致
        var dto = new DarkFieldRawScanImageDTO { RawImageFilePath = Path.GetFullPath(filePath) };
        using var v3BitmapImage = dto.GetImage();

        using var hImage = RAWImageFactoryV2.CreateImage(filePath, false);
        using var v2BitmapImage = hImage.ToBitmapImage(12);

        v3BitmapImage.Size.Width.Should().Be(v2BitmapImage.Size.Width);
        v3BitmapImage.Size.Height.Should().Be(v2BitmapImage.Size.Height);
        v3BitmapImage.ImageInfo.PixelFormatEnum.Should().Be(v2BitmapImage.ImageInfo.PixelFormatEnum);

        using var v3AlgoImage = v3BitmapImage.ToAlgoCVImage();
        using var v2AlgoImage = v2BitmapImage.ToAlgoCVImage();

        var v3Pixels = v3AlgoImage.ToSpan<ushort>();
        var v2Pixels = v2AlgoImage.ToSpan<ushort>();
        v3Pixels.Length.Should().Be(v2Pixels.Length);

        var mismatchCount = 0;
        for (var i = 0; i < v3Pixels.Length; i++)
        {
            if (v3Pixels[i] == v2Pixels[i]) continue;

            mismatchCount++;
            if (mismatchCount <= 5)
                testOutputHelper.WriteLine($"mismatch @{i}: v3={v3Pixels[i]}, v2={v2Pixels[i]}");
        }

        testOutputHelper.WriteLine($"mismatchCount={mismatchCount}");
        mismatchCount.Should().Be(0);
    }

    /// <summary>
    /// 断言 V3.CreateImage 与 V2.CreateImage 同为物理正向图（世界口径宽高）。
    /// <para>log 域（isToLinear=false）：逐像素相等。</para>
    /// <para>线性域（isToLinear=true）：V3 用原生 ImageProc.LogToLinear（float32 取整），V2 为双精度截断 LUT，
    /// 存在 ±1 灰度精度差（2026-10-23 裁定：优先用 algolib 现成方法），断言 |diff| ≤ 1 并输出实际最大差。</para>
    /// </summary>
    private void AssertPixelEqualToV2(string filePath, bool isToLinear)
    {
        using var hImage = RAWImageFactoryV2.CreateImage(filePath, isToLinear);
        var matrix = hImage.RAW16BitsPerPixelToMatrix();
        using var algoImage = RAWImageFactoryV3.CreateImage(filePath, isToLinear);

        hImage.GetImageSize(out int v2Width, out int v2Height);
        testOutputHelper.WriteLine($"isToLinear={isToLinear}, V2: {v2Width} x {v2Height}, V3: {algoImage.Width} x {algoImage.Height}");

        algoImage.Width.Should().Be(v2Width);
        algoImage.Height.Should().Be(v2Height);
        matrix.GetLength(0).Should().Be(v2Height);
        matrix.GetLength(1).Should().Be(v2Width);

        var pixels = algoImage.ToSpan<ushort>();
        var tolerance = isToLinear ? 1 : 0;
        var mismatchCount = 0;
        var maxDiff = 0;
        for (var r = 0; r < algoImage.Height; r++)
        {
            for (var c = 0; c < algoImage.Width; c++)
            {
                var diff = Math.Abs(pixels[r * algoImage.Width + c] - matrix[r, c]);
                if (diff > maxDiff) maxDiff = diff;
                if (diff <= tolerance) continue;

                mismatchCount++;
                if (mismatchCount <= 5)
                    testOutputHelper.WriteLine($"mismatch @({r}, {c}): v3={pixels[r * algoImage.Width + c]}, v2={matrix[r, c]}");
            }
        }

        testOutputHelper.WriteLine($"mismatchCount={mismatchCount}, maxDiff={maxDiff}, tolerance={tolerance}");
        mismatchCount.Should().Be(0);
    }
}
