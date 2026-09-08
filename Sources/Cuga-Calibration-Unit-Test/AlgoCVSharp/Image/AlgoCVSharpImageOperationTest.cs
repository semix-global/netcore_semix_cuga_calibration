using algocv_sharp;
using AwesomeAssertions;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon.Extensions;
using System.Runtime.InteropServices;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpImageOperationTest
{
    [Fact]
    public void Crop_ShouldExtractSubRegionCorrectly()
    {
        const int width = 8;
        const int height = 6;
        const int offsetX = 2;
        const int offsetY = 1;
        const int cropWidth = 4;
        const int cropHeight = 3;

        using var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (byte)((x + y) % 256));
            }
        }

        using var cropped = image.Crop(offsetX, offsetY, cropWidth, cropHeight);

        cropped.Width.Should().Be(cropWidth);
        cropped.Height.Should().Be(cropHeight);
        cropped.Channels.Should().Be(image.Channels);
        cropped.DataType.Should().Be(image.DataType);
        cropped.IsEmpty.Should().BeFalse();

        for (var y = 0; y < cropHeight; y++)
        {
            for (var x = 0; x < cropWidth; x++)
            {
                var expected = image.GetValue<byte>(offsetX + x, offsetY + y);
                cropped.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Crop_FullImage_ShouldMatchOriginal()
    {
        const int width = 5;
        const int height = 4;

        using var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (byte)(x * y));
            }
        }

        using var cropped = image.Crop(0, 0, width, height);

        cropped.Width.Should().Be(width);
        cropped.Height.Should().Be(height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                cropped.GetValue<byte>(x, y).Should().Be(image.GetValue<byte>(x, y));
            }
        }
    }

    [Theory]
    [InlineData(-1, 0, 1, 1)]
    [InlineData(0, -1, 1, 1)]
    [InlineData(5, 0, 2, 1)]
    [InlineData(0, 5, 1, 2)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    public void Crop_InvalidRegion_ShouldThrow(int x, int y, int cropWidth, int cropHeight)
    {
        using var image = new algocv_sharp.Image(4, 4, 1, ImageDataType.UInt8);

        var act = () => image.Crop(x, y, cropWidth, cropHeight);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ReduceDomain_ShouldPreserveDomainPixels()
    {
        const int width = 8;
        const int height = 6;
        const int offsetX = 1;
        const int offsetY = 2;
        const int domainWidth = 5;
        const int domainHeight = 3;

        using var image = new algocv_sharp.Image(width, height);
        ushort value = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, value);
                value++;
            }
        }

        using var reduced = image.ReduceDomain(offsetX, offsetY, domainWidth, domainHeight);

        // ReduceDomain keeps the original image dimensions and preserves the requested domain.
        reduced.Width.Should().Be(width);
        reduced.Height.Should().Be(height);
        reduced.Channels.Should().Be(image.Channels);
        reduced.DataType.Should().Be(image.DataType);
        reduced.IsEmpty.Should().BeFalse();

        for (var y = 0; y < domainHeight; y++)
        {
            for (var x = 0; x < domainWidth; x++)
            {
                var expected = image.GetValue<ushort>(offsetX + x, offsetY + y);
                reduced.GetValue<ushort>(offsetX + x, offsetY + y).Should().Be(expected);
            }
        }
    }

    [Theory]
    [InlineData(-1, 0, 1, 1)]
    [InlineData(0, -1, 1, 1)]
    [InlineData(5, 0, 2, 1)]
    [InlineData(0, 5, 1, 2)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    public void ReduceDomain_InvalidRegion_ShouldThrow(int x, int y, int domainWidth, int domainHeight)
    {
        using var image = new algocv_sharp.Image(4, 4, 1, ImageDataType.UInt8);

        var act = () => image.ReduceDomain(x, y, domainWidth, domainHeight);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Crop_ShouldMatchHalconCropPart()
    {
        const int width = 8;
        const int height = 6;
        const int offsetX = 2;
        const int offsetY = 1;
        const int cropWidth = 4;
        const int cropHeight = 3;

        var buffer = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                buffer[y * width + x] = (byte)((x + y) % 256);
            }
        }

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            using var algoImage = new algocv_sharp.Image(handle.AddrOfPinnedObject(), width, height, width, 1, ImageDataType.UInt8);
            using var halconImage = new HImage();
            halconImage.GenImage1("byte", width, height, handle.AddrOfPinnedObject());

            using var algoCropped = algoImage.Crop(offsetX, offsetY, cropWidth, cropHeight);
            using var halconCropped = halconImage.CropPart(offsetY, offsetX, cropWidth, cropHeight);

            algoCropped.Width.Should().Be(halconCropped.GetSize().Width);
            algoCropped.Height.Should().Be(halconCropped.GetSize().Height);

            var halconValues = halconCropped.GetGrayValuesL();
            for (var y = 0; y < cropHeight; y++)
            {
                for (var x = 0; x < cropWidth; x++)
                {
                    var expected = algoCropped.GetValue<byte>(x, y);
                    halconValues[y * cropWidth + x].Should().Be(expected);
                }
            }
        }
        finally
        {
            handle.Free();
        }
    }

    [Fact]
    public void ReduceDomain_ShouldMatchHalconReduceDomain()
    {
        const int width = 8;
        const int height = 6;
        const int offsetX = 1;
        const int offsetY = 2;
        const int domainWidth = 5;
        const int domainHeight = 3;

        var buffer = new ushort[width * height];
        ushort value = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                buffer[y * width + x] = value++;
            }
        }

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            using var algoImage = new algocv_sharp.Image(width, height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    algoImage.SetValue(x, y, buffer[y * width + x]);
                }
            }

            using var halconImage = new HImage();
            halconImage.GenImage1("uint2", width, height, handle.AddrOfPinnedObject());

            using var algoReduced = algoImage.ReduceDomain(offsetX, offsetY, domainWidth, domainHeight);

            HOperatorSet.GenRectangle1(out var regionObj, offsetY, offsetX, offsetY + domainHeight - 1, offsetX + domainWidth - 1);
            using var region = new HRegion(regionObj);
            using var halconReduced = halconImage.ReduceDomain(region);

            algoReduced.Width.Should().Be(width);
            algoReduced.Height.Should().Be(height);

            for (var y = offsetY; y < offsetY + domainHeight; y++)
            {
                for (var x = offsetX; x < offsetX + domainWidth; x++)
                {
                    var expected = algoReduced.GetValue<ushort>(x, y);
                    HOperatorSet.GetGrayval(halconReduced, y, x, out var halconValue);
                    using (halconValue)
                        halconValue.L.Should().Be(expected);
                }
            }
        }
        finally
        {
            handle.Free();
        }
    }
}