using Core.Models.Models.Ads.PressureGains;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Ads;

public sealed partial class AdsXGainsCalibrationViewModel
{
    protected async Task<bool> LoadDependsAsync(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}