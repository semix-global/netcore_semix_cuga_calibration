using AwesomeAssertions;
using Core.Utilities;
using HalconDotNet;
using Xunit;

namespace CugaCalibrationUnitTest;

public sealed class RawImageFactoryVerificationTest
{
    [Fact]
    public void GetMatrix_ForByteImage_ShouldMatchDimensions()
    {
        using var image = new HImage();
        image.GenImageConst("byte", 5, 4);

        var matrix = image.GetMatrix();

        matrix.GetLength(0).Should().Be(4);
        matrix.GetLength(1).Should().Be(5);

        foreach (var v in matrix) v.Should().Be(0);
    }

    [Fact]
    public void GetMatrix_ForInt2Image_ShouldMatchDimensions()
    {
        using var image = new HImage();
        image.GenImageConst("int2", 3, 2);

        var matrix = image.GetMatrix();

        matrix.GetLength(0).Should().Be(2);
        matrix.GetLength(1).Should().Be(3);

        foreach (var v in matrix) v.Should().Be(0);
    }
}