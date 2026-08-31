using algocv_sharp;
using AwesomeAssertions;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using System.IO;

namespace CugaCalibrationUnitTest;

public class BitmapImageTest
{
    [Theory]
    [InlineData(CIBProfileModeEnum.PMTLog)]
    [InlineData(CIBProfileModeEnum.PMTVoltage)]
    public void ImageSourceTest(CIBProfileModeEnum cibProfileMode)
    {
        const string filePath = @"Assets\test.raw";

        var isToLiner = cibProfileMode == CIBProfileModeEnum.PMTLog;

        var rawBytes = File.ReadAllBytes(filePath);

        using var oldHImage = RAWImageFactory.CreateImage(rawBytes, isToLiner);

        using var oldBitmapImage = oldHImage.ToBitmapImage(isToLiner ? 16 : 12);

        var (size, _, _) = RAWImageFactory.GetSize(rawBytes);
        using var darkFieldImageDTO = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { Size = size, RawImageCIBProfileModeEnum = cibProfileMode, RawImageFilePath = filePath, IsKeepRawImageCIBProfileModeEnum = !isToLiner });
        using var newBitmapImage = darkFieldImageDTO.GetImage();
        using var newHImage = newBitmapImage.ToHImage();

        newHImage.GetGrayValuesL().Should()
            .BeEquivalentTo(oldHImage.GetGrayValuesL(), options => options.WithStrictOrdering());

        newBitmapImage.ChannelMinValue.Should().Be(oldBitmapImage.ChannelMinValue);
        newBitmapImage.ChannelMaxValue.Should().Be(oldBitmapImage.ChannelMaxValue);
        newBitmapImage.ImageInfo.Should().Be(oldBitmapImage.ImageInfo);
        newBitmapImage.GetPixelSpan().SequenceEqual(oldBitmapImage.GetPixelSpan()).Should().BeTrue();
    }

    [Fact]
    public void ToAlgoCVImage_With16BitBitmapImage_ShouldProduceMatchingImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, true);
        using var bitmapImage = hImage.ToBitmapImage(16);

        using var algoImage = bitmapImage.ToAlgoCVImage();

        algoImage.Width.Should().Be(bitmapImage.Width);
        algoImage.Height.Should().Be(bitmapImage.Height);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        var algoPixels = algoImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(hImage.GetGrayValuesL(), value => (ushort)value);

        algoPixels.Should().BeEquivalentTo(halconPixels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToAlgoCVImage_With12BitBitmapImage_ShouldThrowNotSupportedException()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        var act = bitmapImage.ToAlgoCVImage;

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ToAlgoCVImage_With12BitBitmapImage_ShouldMatchingImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        using var algoCVImage = bitmapImage.ToAlgoCVImage();
        algoCVImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\BitmapImageToAlgoImage.raw");

        var algoImageSpans = algoCVImage.ToSpan();
        bitmapImage.GetPixelSpan().SequenceEqual(algoImageSpans).Should().BeTrue();
    }
}