using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Ads;

public sealed partial class AdsYGainsCalibrationViewModel
{
    private bool LoadDepends()
    {
        if (ApplicationCookie.SysUser.IsAdmin) return true;

        var adsPressureGainsViewModel = HostApplication.GetRequiredService<AdsPressureGainsCalibrationViewModel>();
        if (adsPressureGainsViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {adsPressureGainsViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}