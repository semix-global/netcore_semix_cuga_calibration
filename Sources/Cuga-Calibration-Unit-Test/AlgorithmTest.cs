using AwesomeAssertions;
using HalconDotNet;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;
using System.Diagnostics;
using System.IO;
using Core.Utilities;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Xunit;

namespace CugaCalibrationUnitTest;

public sealed class AlgorithmTest
{
    [Fact]
    public void YPixelSizeTest()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw");
        var rawBytes = File.ReadAllBytes(filePath);
        using var image = RawImageFactory.CreateImage(rawBytes);

        image.GetBitsPerPixel().Should().Be(16);

        var algorithm = new Algorithm();

        // 算法
        algorithm.InvertTransformPatchImage128(image, out var linearImage);
        using var _ = linearImage;

        HOperatorSet.GetImageSize(linearImage, out var width, out var height);
        HOperatorSet.GetDomain(linearImage, out var region);
        HOperatorSet.GetRegionPoints(region, out var rowsHTuple, out var columnsHTuple);

        using var _0 = rowsHTuple;
        using var _1 = columnsHTuple;

        HOperatorSet.GetGrayval(linearImage, rowsHTuple, columnsHTuple, out var grayValHTuple);

        using var algorithmImage = new HImage("uint2", width, height);
        algorithmImage.SetGrayval(rowsHTuple, columnsHTuple, grayValHTuple);

        width.Dispose();
        height.Dispose();
        region.Dispose();
        grayValHTuple.Dispose();

        // 自定义
        var (width1, height1) = (SizeI)@image.GetSize();

        using var region1 = @image.GetDomain();
        region1.GetRegionPoints(out var rowsHTuple1, out var columnsHTuple1);

        using var _01 = rowsHTuple1;
        using var _11 = columnsHTuple1;

        using var grayValHTuple1 = @image.GetGrayval(rowsHTuple1, columnsHTuple1);

        // 2 ^ ((gray - 1500) / 128) -> [0, 4095]
        using var subHTuple = grayValHTuple1 - 1500d;
        using var divHTuple = subHTuple / 128d;
        using var exp2HTuple = divHTuple.TupleExp2();
        using var exp2MaxHTuple = exp2HTuple.TupleMax();
        using var scaleHTuple = exp2MaxHTuple / 4095d;
        using var exp2DivHTuple = exp2HTuple / scaleHTuple;
        using var intTuple = exp2DivHTuple.TupleInt();

        using var customImage = new HImage("uint2", width1, height1);
        customImage.SetGrayval(rowsHTuple1, columnsHTuple1, intTuple);

        grayValHTuple.ToLArr().Should().BeEquivalentTo(intTuple.ToLArr(), options => options.WithStrictOrdering());

        var algorithmResult = GetYPixelSize(algorithmImage);
        var customResult = GetYPixelSize(customImage);

        algorithmResult.Should().Be(customResult);
        return;


        double GetYPixelSize(HImage currentImage)
        {
            algorithm.DarkPixSizeCal(currentImage, 1, out var drawingImageObj, out var meanTuple);

            using var mean = meanTuple;

            using var drawingImage = new HImage(drawingImageObj);
            var yPixelSize = 10 / meanTuple.D;
            var drawImageFilePath = $"YPixelSize({yPixelSize:f3})_DrawImage_Guid({Guid.NewGuid()}).jpg";
            var imageFilePath = $"YPixelSize({yPixelSize:f3})_Image_Guid({Guid.NewGuid()}).jpg";

            currentImage.Save(imageFilePath);
            drawingImage.Save(drawImageFilePath);

            using var _0 = Process.Start(new ProcessStartInfo
            {
                FileName = drawImageFilePath,
                UseShellExecute = true
            });

            using var _1 = Process.Start(new ProcessStartInfo
            {
                FileName = imageFilePath,
                UseShellExecute = true
            });

            return yPixelSize;
        }
    }

    [Fact]
    public void Test()
    {
        var vector = Vector<double>.Build.Dense([
            -3.66663334716577E-06,
            30115.0885774169,
            7.06237915437669E-06,
            30115.7653065492,
            1.88306876225397E-05,
            30115.161836134,
            -3.42994462698698E-06,
            30118.8430104505,
            2.20059882849455E-06,
            30114.3431134508,
            -1.45942613016814E-05,
            30117.8308322228,
            -1.42839271575212E-05,
            30115.036446575,
            -8.38685082271695E-07,
            30117.9681661563,
            7.05179991200566E-06,
            30116.7353182929,
            -1.8802413251251E-05,
            2.21737255697371,
            30115.8965302019,
            -5.44742215424776E-06,
            30114.9240736202,
            7.78317917138338E-06,
            30117.1578004701,
            -2.85875285044312E-06,
            30114.9448583883,
            3.75654781237245E-06,
            30117.0616474719,
            2.75787897408009E-07,
            30114.8858639103,
            7.95780215412378E-06,
            30116.9705713233,
            30216.9705713233,
            1.82477524504066E-05
        ]);

        var quantile = vector.Quantile(0.25);

        var doubles = Filter.IQR(Vector<double>.Build.Dense([..vector.Where(t => t >= 100)]));

        var d = doubles[0];
    }
}