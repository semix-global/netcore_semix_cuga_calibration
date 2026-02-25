using Core.Models.Models.Ads.PressureGains;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Ads;

public sealed partial class AdsYGainsCalibrationViewModel
{
    private bool LoadDepends()
    {
        if (ApplicationCookie.SysUser.IsAdmin) return true;

        if (ApplicationCookie.SysUser.IsAdmin) return true;
        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}