using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using System.IO;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using Xunit;

namespace CugaCalibrationUnitTest;

public class AODWaveformUnitTest
{
    [Fact]
    public void Test1()
    {
        var sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameof(AODWaveformGenerator));
        var destDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AODWaveformFiles");
        var generatePrescanAODWaveformParam = new GeneratePrescanAODWaveformParam
        {
            OpticsMagTypeEnum = OpticsMagTypeEnum.Middle,
            IsHeaderAndFooter = false,
            BandWidth = 210,
            CenterFrequency = 200,
            FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Increasing,
            SampleRate = 10640,
            Amplitude = 0.9,
            DirectoryPath = sourceDirectory,
            ZeroSampleCount = 110,
            FlatnessTime = 1000,
            EndpointSampleCount = 10000,
            GenerateRetryTimes = 2000,
            ElectrodeConfigurations =
            [
                new GenerateAODWaveformElectrodeConfiguration { OffsetFrequency = 215, OffsetFrequencyPeriodCoefficient = 0.8 },
                new GenerateAODWaveformElectrodeConfiguration { OffsetFrequency = 215, OffsetFrequencyPeriodCoefficient = 1.5 }
            ],
            SincCoefficient = 1.1,
            AstigmatismCompensationCoefficient = 1.2,
            SphericalAberrationCompensationCoefficient = 1.3,
            SecondaryAstigmatismCompensationCoefficient = 1.4,
            ComaCompensationCoefficient = 1.55,
            TrefoilCompensationCoefficient = 1.6,
            QuadrafoilCompensationCoefficient = 1.7,
            AlphaOrder = 1.8,
            AlphaOrderCoefficient = 1.9
        };
        var (prescanAODWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), CancellationToken.None);

        var prescanAODWaveformResultFilePath = $"prescan" +
                                               $"_{generatePrescanAODWaveformParam.OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
                                               $"_{generatePrescanAODWaveformParam.FlatnessTime:0.###}ns" +
                                               $"_{generatePrescanAODWaveformParam.AdaptTo().LowFrequency:0.###}Mhz" +
                                               $"_{generatePrescanAODWaveformParam.AdaptTo().HighFrequency:0.###}Mhz" +
                                               $"{AODWaveformGenerator.PrescanAODWaveformFileExtension}";

        Assert.True(prescanAODWaveformResult.IsSuccess);
        Assert.Null(exception);

        var dateTime = prescanAODWaveformResult.FilePath
            .TrimStart(sourceDirectory.ToCharArray())
            .TrimEnd(prescanAODWaveformResultFilePath.ToCharArray())
            .Trim(Path.DirectorySeparatorChar);
        Assert.NotNull(DateTimeHelper.String2DateTime(dateTime, Constants.LongFileDateTimeFormat));

        Assert.Equal(prescanAODWaveformResult.FilePath, Path.Combine(sourceDirectory, dateTime, prescanAODWaveformResultFilePath));

        var prescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        Assert.Equal(generatePrescanAODWaveformParam.ElectrodeConfigurations.Count, prescanAODWaveformProfiles.Count);

        for (var i = 0; i < generatePrescanAODWaveformParam.ElectrodeConfigurations.Count; i++)
        {
            var configuration = generatePrescanAODWaveformParam.ElectrodeConfigurations[i];
            var prescanAODWaveformProfile = prescanAODWaveformProfiles[i];

            var prescanAODWaveformProfileFilePath = $"prescan_" +
                                                    $"{generatePrescanAODWaveformParam.OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
                                                    $"${generatePrescanAODWaveformParam.AdaptTo().NumberOfSamples + generatePrescanAODWaveformParam.ZeroSampleCount}" +
                                                    $"${generatePrescanAODWaveformParam.ZeroSampleCount:0.###}" +
                                                    $"$600$02" +
                                                    $"${configuration.OffsetFrequency:0.###}" +
                                                    $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

            Assert.Equal(prescanAODWaveformProfile.FilePath, Path.Combine(sourceDirectory, dateTime, configuration.OpticsAODElectrodeEnum.ToString(), prescanAODWaveformProfileFilePath));

            Assert.Equal(File.ReadAllText(prescanAODWaveformProfile.FilePath), File.ReadAllText(Path.Combine(destDirectory, prescanAODWaveformProfileFilePath)));
        }
    }
}