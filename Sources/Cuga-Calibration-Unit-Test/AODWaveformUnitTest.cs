using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using System.IO;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using MiniExcelLibs;
using Xunit;

namespace CugaCalibrationUnitTest;

public class AODWaveformUnitTest
{
    [Theory]
    [InlineData(false, false, nameof(AbstractGenerateAODWaveformParam.SincCoefficient))]
    [InlineData(false, true, nameof(AbstractGenerateAODWaveformParam.UniformityConfigurations))]
    [InlineData(true, false, nameof(AbstractGenerateAODWaveformParam.SincCoefficient))]
    [InlineData(true, true, nameof(AbstractGenerateAODWaveformParam.UniformityConfigurations))]
    public void AODWaveformGeneratorTest(bool isChirp, bool isUseUniformityConfigurations, string expectedName)
    {
        var baseDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AODWaveformFiles", isChirp ? "Chirp" : "Prescan");
        var outputDirectoryPath = Path.Combine(baseDirectoryPath, "Output");
        var expectedDirectoryPath = Path.Combine(baseDirectoryPath, expectedName);
        var uniformityConfigurations = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AODWaveformUniformity.xlsx")).ToArray();
        Assert.NotEmpty(uniformityConfigurations);

        AbstractGenerateAODWaveformParam param = new GeneratePrescanAODWaveformParam
        {
            OpticsMagTypeEnum = OpticsMagTypeEnum.Middle,
            IsHeaderAndFooter = false,
            BandWidth = 210d,
            CenterFrequency = 200d,
            FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Increasing,
            SampleRate = 10640d,
            Amplitude = 0.9d,
            DirectoryPath = outputDirectoryPath,
            ZeroSampleCount = 110,
            EndpointSampleCount = 10000,
            GenerateRetryTimes = 2000,
            ElectrodeConfigurations =
            [
                new GenerateAODWaveformElectrodeConfiguration { OffsetFrequency = 215d, OffsetFrequencyPeriodCoefficient = 0.8d },
                new GenerateAODWaveformElectrodeConfiguration { OffsetFrequency = 215d, OffsetFrequencyPeriodCoefficient = 1.5d }
            ],
            SincCoefficient = 1.1d,
            AstigmatismCompensationCoefficient = 1.2d,
            SphericalAberrationCompensationCoefficient = 1.3d,
            SecondaryAstigmatismCompensationCoefficient = 1.4d,
            ComaCompensationCoefficient = 1.55d,
            TrefoilCompensationCoefficient = 1.6d,
            QuadrafoilCompensationCoefficient = 1.7d,
            AlphaOrder = 1.8d,
            AlphaOrderCoefficient = 1.9d,
            FlatnessTime = 1000.3d
        };

        if (isChirp)
        {
            var temp = (GenerateChirpAODWaveformParam)new GenerateChirpAODWaveformParam().AdaptIn(param);
            temp.SoundPacketLength = 11.2d;
            temp.SoundSpeed = 5.742d;

            param = temp;
        }

        if (isUseUniformityConfigurations)
        {
            param.UniformityConfigurations = uniformityConfigurations;
            param.SincCoefficient = 0d;
        }

        var resultFilePath = isChirp
            ? $"chirp" +
              $"_{((GenerateChirpAODWaveformParam)param).OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
              $"_{((GenerateChirpAODWaveformParam)param).SoundPacketLength:0.###}mm" +
              $"_{((GenerateChirpAODWaveformParam)param).AdaptTo().LowFrequency:0.###}Mhz" +
              $"_{((GenerateChirpAODWaveformParam)param).AdaptTo().HighFrequency:0.###}Mhz" +
              $"{AODWaveformGenerator.ChirpAODWaveformFileExtension}"
            : $"prescan" +
              $"_{((GeneratePrescanAODWaveformParam)param).OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
              $"_{((GeneratePrescanAODWaveformParam)param).FlatnessTime:0.###}ns" +
              $"_{((GeneratePrescanAODWaveformParam)param).AdaptTo().LowFrequency:0.###}Mhz" +
              $"_{((GeneratePrescanAODWaveformParam)param).AdaptTo().HighFrequency:0.###}Mhz" +
              $"{AODWaveformGenerator.PrescanAODWaveformFileExtension}";

        object result;
        Exception? exception;
        if (isChirp)
            (result, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(((GenerateChirpAODWaveformParam)param).AdaptTo(), CancellationToken.None);
        else
            (result, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(((GeneratePrescanAODWaveformParam)param).AdaptTo(), CancellationToken.None);

        Assert.True(isChirp
            ? ((AODWaveformGenerator.ChirpAODWaveformResult)result).IsSuccess
            : ((AODWaveformGenerator.PrescanAODWaveformResult)result).IsSuccess);

        Assert.Null(exception);

        var dateTime = (isChirp
                ? ((AODWaveformGenerator.ChirpAODWaveformResult)result).FilePath
                : ((AODWaveformGenerator.PrescanAODWaveformResult)result).FilePath)
            .TrimStart(outputDirectoryPath.ToCharArray())
            .TrimEnd(resultFilePath.ToCharArray())
            .Trim(Path.DirectorySeparatorChar);
        Assert.NotNull(DateTimeHelper.String2DateTime(dateTime, Constants.LongFileDateTimeFormat));

        Assert.Equal(isChirp
            ? ((AODWaveformGenerator.ChirpAODWaveformResult)result).FilePath
            : ((AODWaveformGenerator.PrescanAODWaveformResult)result).FilePath, Path.Combine(outputDirectoryPath, dateTime, resultFilePath));

        IReadOnlyList<AbstractAODWaveformProfile> profiles = isChirp
            ? AODWaveformProfileFactory.CreateChirpList((AODWaveformGenerator.ChirpAODWaveformResult)result)
            : AODWaveformProfileFactory.CreatePrescanList((AODWaveformGenerator.PrescanAODWaveformResult)result);
        Assert.Equal(param.ElectrodeConfigurations.Count, profiles.Count);

        for (var i = 0; i < param.ElectrodeConfigurations.Count; i++)
        {
            var configuration = param.ElectrodeConfigurations[i];
            var profile = profiles[i];

            var profileFilePath = isChirp
                ? $"chirp_" +
                  $"{param.OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
                  $"${((GenerateChirpAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount:0.###}" +
                  $"$600$03" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt"
                : $"prescan_" +
                  $"{param.OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()}" +
                  $"${((GeneratePrescanAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount:0.###}" +
                  $"$600$02" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

            Assert.Equal(profile.FilePath, Path.Combine(outputDirectoryPath, dateTime, configuration.OpticsAODElectrodeEnum.ToString(), profileFilePath));

            Assert.Equal(File.ReadAllText(profile.FilePath), File.ReadAllText(Path.Combine(expectedDirectoryPath, profileFilePath)));
        }
    }
}