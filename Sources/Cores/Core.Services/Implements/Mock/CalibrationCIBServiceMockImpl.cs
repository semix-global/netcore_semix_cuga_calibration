using System.IO;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationCIBServiceMockImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService) : ICalibrationCIBService
{
    private readonly string _mockImageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTValuesAsync(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var result = new DarkFieldImageDto[cibInformations.Count];

        for (var i = 0; i < result.Length; i++)
        {
            var cibInformation = cibInformations[i];

            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
            var size = (SizeI)image.GetSize();
            result[i] = new DarkFieldImageDto { PmtId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image, Matrix = matrix };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDto>>(result));
    }
}