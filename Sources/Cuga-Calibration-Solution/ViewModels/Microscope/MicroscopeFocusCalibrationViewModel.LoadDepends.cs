using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Microscope;

public sealed partial class MicroscopeFocusCalibrationViewModel
{
    private bool LoadDepends()
    {
        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AdsXGainsItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AdsYGainsItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}