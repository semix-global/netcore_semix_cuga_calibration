using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(CIBViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CIBViewModel(
    ICalibrationCIBService calibrationCIBService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationCIBService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        var ret = calibrationCIBService.SetMMD(cibInformation, logGainMul128U12Bits, gainS16Bits, maxLogGain);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetLightMatching(CIBInformation cibInformation, double digitalGainPlusMultiplicativeFactors)
    {
        var ret = calibrationCIBService.SetLightMatching(cibInformation, digitalGainPlusMultiplicativeFactors);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public async Task<IReadOnlyList<DarkFieldImageDto>> GetPMTValuesAsync(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        var ret = await calibrationCIBService.GetPMTValuesAsync(
            opticsIlluminationModeEnum,
            productivityInformation,
            stageCoordinateSystemEnum,
            position,
            cibInformations,
            imageWidth,
            isAutoFocus,
            cancellationToken);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }
}