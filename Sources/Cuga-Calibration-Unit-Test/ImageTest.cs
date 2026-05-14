// #define ImageTest

using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;

#if ImageTest
using System.Diagnostics;
using System.IO;
using Net.Utilities.Helpers.Helpers.Files;
#endif

namespace CugaCalibrationUnitTest;

public class ImageTest
{
    [Theory]
    [InlineData(@"YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw")]
    [InlineData(@"VSharpTest\test_1_4_true.raw")]
    [InlineData(@"VSharpTest\test_1_6_true.raw")]
    [InlineData(@"VSharpTest\20260315_1116_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1119_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1107_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1110_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1098_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1101_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_780_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_783_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1122_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1113_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1104_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_786_0_0_1_short_001000_PMT08-CH3_8.raw")]
    public void Test(string filePath)
    {
        using var originImage = RAWImageFactory.CreateImage(@$"Assets\{filePath}", true);

        using var expectedLinearImage = RAWImageFactoryV2.CreateImage(@$"Assets\{filePath}", true);

        originImage.GetGrayValuesL().Should()
            .BeEquivalentTo(expectedLinearImage.GetGrayValuesL(), options => options.WithStrictOrdering());

#if ImageTest
        var imageFullPath = Path.GetFullPath($"{nameof(ImageTest)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        originImage.Save(imageFullPath);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = imageFullPath,
            UseShellExecute = true
        });
#endif
    }
}