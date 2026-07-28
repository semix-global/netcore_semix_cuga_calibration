using AwesomeAssertions;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
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
}