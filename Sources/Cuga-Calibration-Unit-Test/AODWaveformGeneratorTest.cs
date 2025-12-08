using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using System.IO;
using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.DTO.Swath;
using Xunit;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
#endif

namespace CugaCalibrationUnitTest;

public class AODWaveformUnitTest
{
    [Theory]
    [InlineData(false, false, false, false, nameof(AbstractGenerateAODWaveformParam.SincCoefficient))]
    [InlineData(false, false, true, false, "Uniformity")]
    [InlineData(true, false, false, false, nameof(AbstractGenerateAODWaveformParam.SincCoefficient))]
    [InlineData(true, false, true, false, "Uniformity")]
    [InlineData(false, false, false, true, $"{nameof(AbstractGenerateAODWaveformParam.SincCoefficient)}_SlopeDeltaK")]
    [InlineData(false, false, true, true, $"Uniformity_SlopeDeltaK")]
    [InlineData(true, false, false, true, $"{nameof(AbstractGenerateAODWaveformParam.SincCoefficient)}_SlopeDeltaK")]
    [InlineData(true, false, true, true, $"Uniformity_SlopeDeltaK")]
    [InlineData(false, true, false, false, nameof(FunctionMonotonicTypeEnum.Flatness))]
    [InlineData(true, true, false, false, nameof(FunctionMonotonicTypeEnum.Flatness))]
    public void AODWaveformGeneratorTest(bool isChirp, bool isFunctionMonotonicTypeEnumFlatness, bool isUseUniformityConfigurations, bool isUseSlopeDeltaKs, string expectedName)
    {
        var baseDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AODWaveformFiles", isChirp ? "Chirp" : "Prescan");
        var outputDirectoryPath = Path.Combine(baseDirectoryPath, "Output");
        var expectedDirectoryPath = Path.Combine(baseDirectoryPath, expectedName);
        var uniformityConfigurations = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AODWaveformUniformity.xlsx")).ToArray();
        Assert.NotEmpty(uniformityConfigurations);

        AbstractGenerateAODWaveformParam param = new GeneratePrescanAODWaveformParam
        {
            ProductivityInformation = ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = string.Empty, Mag = SxMAGEnum.Mid, Speed = (SxSpeedEnum)(-1), IsUsed = true }, new CgSwathSpeedInfo(), -1),
            IsHeaderAndFooter = false,
            BandWidth = 210d,
            CenterFrequency = 200d,
            FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Increasing,
            SampleRate = 10640d,
            DirectoryPath = outputDirectoryPath,
            ZeroSampleCount = 110,
            EndpointSampleCount = 10000,
            GenerateRetryTimes = 2000,
            ElectrodeConfigurations =
            [
                new GenerateAODWaveformElectrodeConfiguration { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1, OffsetFrequency = 215d, OffsetFrequencyPeriodCoefficient = 0.8d, Amplitude = 0.9d },
                new GenerateAODWaveformElectrodeConfiguration { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2, OffsetFrequency = 215d, OffsetFrequencyPeriodCoefficient = 1.5d, Amplitude = 0.9d }
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
            temp.SoundPacketLength = 10.2d;
            temp.SoundSpeed = 5.742d;

            param = temp;
        }

        if (isFunctionMonotonicTypeEnumFlatness)
        {
            param.FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness;
            param.BandWidth = 0d;
            foreach (var item in param.ElectrodeConfigurations) item.UniformityConfigurations = [];
            param.SincCoefficient = 0d;
            param.AstigmatismCompensationCoefficient = 0d;
            param.SphericalAberrationCompensationCoefficient = 0d;
            param.SecondaryAstigmatismCompensationCoefficient = 0d;
            param.ComaCompensationCoefficient = 0d;
            param.TrefoilCompensationCoefficient = 0d;
            param.QuadrafoilCompensationCoefficient = 0d;
            param.AlphaOrder = 0d;
            param.AlphaOrderCoefficient = 0d;
        }

        if (isUseUniformityConfigurations)
        {
            foreach (var item in param.ElectrodeConfigurations) item.UniformityConfigurations = uniformityConfigurations;
            param.SincCoefficient = 0d;
        }

        if (isUseSlopeDeltaKs)
        {
            param.SlopeDeltaKConfigurations =
            [
                new GenerateAODWaveformSlopeDeltaKConfiguration { DeltaK = -0.0002d },
                new GenerateAODWaveformSlopeDeltaKConfiguration { DeltaK = -0.0001d },
                new GenerateAODWaveformSlopeDeltaKConfiguration { DeltaK = 0.0001d },
                new GenerateAODWaveformSlopeDeltaKConfiguration { DeltaK = 0.0002d }
            ];
        }

        var resultFilePath = isChirp
            ? $"chirp_" +
              $"{((GenerateChirpAODWaveformParam)param).ProductivityInformation.AdaptTo().Mag.ToString()}_" +
              $"{((GenerateChirpAODWaveformParam)param).SoundPacketLength:0.###}mm_" +
              $"{((GenerateChirpAODWaveformParam)param).AdaptTo().LowFrequency:0.###}Mhz_" +
              $"{((GenerateChirpAODWaveformParam)param).AdaptTo().HighFrequency:0.###}Mhz" +
              $"{AODWaveformGenerator.ChirpAODWaveformFileExtension}"
            : $"prescan_" +
              $"{((GeneratePrescanAODWaveformParam)param).ProductivityInformation.AdaptTo().Mag.ToString()}_" +
              $"{((GeneratePrescanAODWaveformParam)param).FlatnessTime:0.###}ns_" +
              $"{((GeneratePrescanAODWaveformParam)param).AdaptTo().LowFrequency:0.###}Mhz_" +
              $"{((GeneratePrescanAODWaveformParam)param).AdaptTo().HighFrequency:0.###}Mhz" +
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
            .Replace(outputDirectoryPath, string.Empty)
            .Replace(resultFilePath, string.Empty)
            .Trim(Path.DirectorySeparatorChar);
        Assert.NotNull(DateTimeHelper.String2DateTime(isChirp
            ? dateTime.Replace("chirp", string.Empty)
            : dateTime.Replace("prescan", string.Empty), Constants.LongFileDateTimeFormat));

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
                  $"{param.ProductivityInformation.AdaptTo().Mag.ToString()}" +
                  $"${((GenerateChirpAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount:0.###}" +
                  $"$600$03" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt"
                : $"prescan_" +
                  $"{param.ProductivityInformation.AdaptTo().Mag.ToString()}" +
                  $"${((GeneratePrescanAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount:0.###}" +
                  $"$600$02" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

            var matlabProfileFilePath = isChirp
                ? $"chirp_" +
                  $"${((GenerateChirpAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount}" +
                  $"$600$03" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt"
                : $"prescan_" +
                  $"${((GeneratePrescanAODWaveformParam)param).AdaptTo().NumberOfSamples + param.ZeroSampleCount}" +
                  $"${param.ZeroSampleCount}" +
                  $"$600$02" +
                  $"${configuration.OffsetFrequency:0.###}" +
                  $"${configuration.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

            Assert.Equal(profile.FilePath, FileHelper.GetEnsureLongPathSupport(Path.Combine(outputDirectoryPath, dateTime, configuration.OpticsAODElectrodeEnum.ToString(), profileFilePath)));

            Assert.Equal(File.ReadAllText(profile.FilePath), File.ReadAllText(Path.Combine(expectedDirectoryPath, matlabProfileFilePath)));
        }
    }
}