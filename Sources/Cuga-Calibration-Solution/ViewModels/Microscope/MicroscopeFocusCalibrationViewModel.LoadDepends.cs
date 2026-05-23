using CugaCalibration.ViewModels.Ads;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Microscope;

public sealed partial class MicroscopeFocusCalibrationViewModel
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

        var adsXGainsViewModel = HostApplication.GetRequiredService<AdsXGainsCalibrationViewModel>();
        if (adsXGainsViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {adsXGainsViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var adsYGainsViewModel = HostApplication.GetRequiredService<AdsYGainsCalibrationViewModel>();
        if (adsYGainsViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {adsYGainsViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}