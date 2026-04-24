using AwesomeAssertions;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.DarkField;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.ImageViewer.WPF.Drawables;
using System.IO;

namespace CugaCalibrationUnitTest;

public class BitmapImageTest
{
    [Theory]
    [InlineData(true, CIBProfileModeEnum.PMTLog)]
    [InlineData(false, CIBProfileModeEnum.PMTVoltage)]
    public void ImageSourceTest(bool isLinear, CIBProfileModeEnum cibProfileMode)
    {
        const string filePath = @"Assets\test.raw";

        var rawBytes = File.ReadAllBytes(filePath);

        using var oldHImage = isLinear == false
            ? RAWImageFactory.CreateImage(rawBytes, false)
            : cibProfileMode == CIBProfileModeEnum.PMTLog
                ? RAWImageFactory.CreateImage(rawBytes, true)
                : RAWImageFactory.CreateImage(rawBytes, false);

        using var oldBitmapImage = oldHImage.ToBitmapImage(cibProfileMode == CIBProfileModeEnum.PMTVoltage ? 16 : 12);

        var (size, _, _) = RAWImageFactory.GetSize(rawBytes);
        var darkFieldImageDTO = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { Size = size, RawImageCIBProfileModeEnum = cibProfileMode, RawImageFilePath = filePath, IsKeepRawImageCIBProfileModeEnum = !isLinear });
        using var newBitmapImage = darkFieldImageDTO.GetImage();
        using var newHImage = newBitmapImage.ToHImage();

        HOperatorSet.GetRegionPoints(oldHImage, out var expectedRowsHTuple, out var expectedColumnsHTuple);
        using var _3 = expectedRowsHTuple;
        using var _4 = expectedColumnsHTuple;
        HOperatorSet.GetGrayval(oldHImage, expectedRowsHTuple, expectedColumnsHTuple, out var expectedGrayValHTuple);
        using var _5 = expectedGrayValHTuple;

        newHImage.GetGrayValuesL().Should()
            .BeEquivalentTo(expectedGrayValHTuple.ToLArr(), options => options
                .WithStrictOrdering()
                .Using<long>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1))
                .WhenTypeIs<long>());

        ContentEquals(oldBitmapImage, newBitmapImage).Should().BeTrue();
    }

    [Theory]
    [InlineData(true, CIBProfileModeEnum.PMTLog)]
    [InlineData(false, CIBProfileModeEnum.PMTVoltage)]
    public void ConverterTest(bool isLinear, CIBProfileModeEnum cibProfileMode)
    {
        const string filePath = @"Assets\test.raw";

        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = isLinear == false
            ? RAWImageFactory.CreateImage(rawBytes, false)
            : cibProfileMode == CIBProfileModeEnum.PMTLog
                ? RAWImageFactory.CreateImage(rawBytes, true)
                : RAWImageFactory.CreateImage(rawBytes, false);

        // old
        var oldBitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = hImage.ToBitmapImage(cibProfileMode == CIBProfileModeEnum.PMTVoltage ? 16 : 12)
        };
        var (min, max) = oldBitmapImageDrawable.BitmapImage.GetChannelRange();
        oldBitmapImageDrawable.ChannelMinValue = min;
        oldBitmapImageDrawable.ChannelMaxValue = max;

        // new
        using var newBitmapImage = hImage.ToBitmapImage(cibProfileMode == CIBProfileModeEnum.PMTVoltage ? 16 : 12);
        var newBitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = newBitmapImage
        };
        (min, max) = newBitmapImageDrawable.BitmapImage.GetChannelRange();
        newBitmapImageDrawable.ChannelMinValue = min;
        newBitmapImageDrawable.ChannelMaxValue = max;

        // assert
        ContentEquals(oldBitmapImageDrawable.BitmapImage, newBitmapImageDrawable.BitmapImage).Should().BeTrue();
        oldBitmapImageDrawable.ChannelMinValue.Should().Be(newBitmapImageDrawable.ChannelMinValue);
        oldBitmapImageDrawable.ChannelMaxValue.Should().Be(newBitmapImageDrawable.ChannelMaxValue);
    }

    private bool ContentEquals(BitmapImage image, BitmapImage other, bool includeDisplayParameters = true)
    {
        if (ReferenceEquals(image, other)) return true;

        // 1) 基础参数比较（尺寸/像素格式/Alpha 等）
        if (image.ImageInfo != other.ImageInfo) return false;

        // 2) 业务参数比较（显示相关参数）
        if (includeDisplayParameters &&
            (image.ChannelMinValue != other.ChannelMinValue ||
             image.ChannelMaxValue != other.ChannelMaxValue ||
             image.ColorModeEnum != other.ColorModeEnum))
        {
            return false;
        }

        // 3) 像素逐字节比较
        return image.GetPixelSpan().SequenceEqual(other.GetPixelSpan());
    }
}