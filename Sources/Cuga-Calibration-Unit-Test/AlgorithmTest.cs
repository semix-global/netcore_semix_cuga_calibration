using System;
using System.Diagnostics;
using System.IO;
using AwesomeAssertions;
using Core.Models.Models.Common.DarkField;
using Core.Utilities;
using HalconDotNet;
using HAlgorithm;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Plottables;
using ScottPlot;
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

        using var algorithmDarkFieldImage = new DarkFieldImageDTO { Image = algorithmImage };
        algorithmDarkFieldImage.RawImageFilePath = filePath;
        using var customDarkFieldImage = new DarkFieldImageDTO { Image = customImage };
        customDarkFieldImage.RawImageFilePath = filePath;

        var algorithmResult = GetYPixelSize(algorithmDarkFieldImage);
        var customResult = GetYPixelSize(customDarkFieldImage);

        algorithmResult.Should().Be(customResult);
        return;


        double GetYPixelSize(DarkFieldImageDTO darkFieldImage)
        {
            algorithm.DarkPixSizeCal(darkFieldImage.Image, 1, out var drawingImageObj, out var meanTuple);

            using var mean = meanTuple;

            using var drawingImage = new HImage(drawingImageObj);
            var yPixelSize = 10 / meanTuple.D;
            var drawImageFilePath = $"YPixelSize({yPixelSize:f3})_DrawImage_Guid({Guid.NewGuid()}).jpg";
            var imageFilePath = $"YPixelSize({yPixelSize:f3})_Image_Guid({Guid.NewGuid()}).jpg";

            darkFieldImage.Image.Save(imageFilePath);
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
}