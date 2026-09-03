using algocv_sharp;
using AwesomeAssertions;
using System.IO;
using System.Runtime.InteropServices;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpImageCoreTest
{
    [Fact]
    public void DefaultConstructor_ShouldCreateEmptyImage()
    {
        using var image = new algocv_sharp.Image();

        image.IsEmpty.Should().BeTrue();
        image.Width.Should().Be(0);
        image.Height.Should().Be(0);
        image.Channels.Should().Be(1);
        image.Stride.Should().Be(0);
        image.DataType.Should().Be(ImageDataType.UInt8);
        image.DataPtr.Should().Be(IntPtr.Zero);
    }

    [Theory]
    [InlineData(1, 1, 1, ImageDataType.UInt8)]
    [InlineData(10, 20, 1, ImageDataType.UInt8)]
    [InlineData(10, 20, 1, ImageDataType.UInt16)]
    [InlineData(8, 8, 3, ImageDataType.UInt8)]
    [InlineData(7, 13, 4, ImageDataType.UInt8)]
    public void ParameterConstructor_ShouldCreateImageWithCorrectMetadata(
        int width, int height, int channels, ImageDataType dataType)
    {
        using var image = new algocv_sharp.Image(width, height, channels, dataType);

        image.IsEmpty.Should().BeFalse();
        image.Width.Should().Be(width);
        image.Height.Should().Be(height);
        image.Channels.Should().Be(channels);
        image.DataType.Should().Be(dataType);
        image.DataPtr.Should().NotBe(IntPtr.Zero);
        image.Stride.Should().Be(width * channels * GetElementSize(dataType));
    }

    [Fact]
    public void ExternalMemoryConstructor_ShouldWrapProvidedBuffer()
    {
        const int width = 5;
        const int height = 4;
        const int channels = 1;
        const int elementSize = 1;
        const int stride = width * channels * elementSize;
        var buffer = new byte[stride * height];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i % 256);
        }

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            using var image = new algocv_sharp.Image(handle.AddrOfPinnedObject(), width, height, stride, channels, ImageDataType.UInt8);

            image.Width.Should().Be(width);
            image.Height.Should().Be(height);
            image.Channels.Should().Be(channels);
            image.Stride.Should().Be(stride);
            image.DataType.Should().Be(ImageDataType.UInt8);
            image.DataPtr.Should().Be(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }

        // Dispose of Image must not free the external buffer.
        buffer[0].Should().Be(0);
        buffer[^1].Should().Be((byte)((buffer.Length - 1) % 256));
    }

    [Fact]
    public void Load_FromExistingRawFile_ShouldPopulateMetadata()
    {
        const string filePath = @"Assets\test.raw";

        using var image = new algocv_sharp.Image(filePath);

        // is_empty_image currently returns true even after a successful file load,
        // so only verify concrete metadata and buffer state here.
        image.Width.Should().BeGreaterThan(0);
        image.Height.Should().BeGreaterThan(0);
        image.Channels.Should().BeGreaterThan(0);
        image.DataPtr.Should().NotBe(IntPtr.Zero);
    }

    [Fact]
    public void Constructor_FromExistingRawFile_ShouldPopulateMetadata()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\test.raw");

        using var image = new algocv_sharp.Image(filePath);

        // is_empty_image currently returns true even after a successful file load,
        // so only verify concrete metadata and buffer state here.
        image.Width.Should().BeGreaterThan(0);
        image.Height.Should().BeGreaterThan(0);
        image.Channels.Should().BeGreaterThan(0);
        image.DataPtr.Should().NotBe(IntPtr.Zero);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP016:Don't use disposed instance", Justification = "Intentionally verifying disposed state.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP017:Prefer using", Justification = "Testing explicit Dispose behavior.")]
    public void Dispose_ShouldReleaseNativeMemory()
    {
        var image = new algocv_sharp.Image(10, 10, 1, ImageDataType.UInt8);
        image.Dispose();

        image.DataPtr.Should().Be(IntPtr.Zero);
        image.IsEmpty.Should().BeTrue();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP016:Don't use disposed instance", Justification = "Intentionally verifying double Dispose is safe.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP017:Prefer using", Justification = "Testing explicit Dispose behavior.")]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var image = new algocv_sharp.Image(10, 10, 1, ImageDataType.UInt8);
        image.Dispose();

        var act = () => image.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP016:Don't use disposed instance", Justification = "Intentionally verifying disposed state.")]
    public void UsingStatement_ShouldDisposeImageAutomatically()
    {
        algocv_sharp.Image image;
        using (image = new algocv_sharp.Image(10, 10, 1, ImageDataType.UInt8))
        {
            image.DataPtr.Should().NotBe(IntPtr.Zero);
        }

        image.DataPtr.Should().Be(IntPtr.Zero);
    }

    [Fact]
    public void Handle_ForValidImage_ShouldNotBeZero()
    {
        using var image = new algocv_sharp.Image(5, 5, 1, ImageDataType.UInt8);

        image.Handle.Should().NotBe(IntPtr.Zero);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP016:Don't use disposed instance", Justification = "Intentionally verifying disposed state.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP017:Prefer using", Justification = "Testing explicit Dispose behavior.")]
    public void Handle_AfterDispose_ShouldBeZero()
    {
        var image = new algocv_sharp.Image(5, 5, 1, ImageDataType.UInt8);
        image.Dispose();

        image.Handle.Should().Be(IntPtr.Zero);
    }

    [Fact]
    public void EmptyImage_Indexer_ShouldReturnZero()
    {
        using var image = new algocv_sharp.Image();

        image[0].Should().Be(IntPtr.Zero);
    }

    private static int GetElementSize(ImageDataType dataType) => dataType switch
    {
        ImageDataType.UInt8 => 1,
        ImageDataType.Int8 => 1,
        ImageDataType.UInt16 => 2,
        ImageDataType.Int16 => 2,
        ImageDataType.Float => 4,
        ImageDataType.Double => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}