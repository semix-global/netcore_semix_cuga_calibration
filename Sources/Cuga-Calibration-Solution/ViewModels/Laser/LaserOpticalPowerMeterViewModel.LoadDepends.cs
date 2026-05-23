using CugaCalibration.ViewModels.Ads;
using CugaCalibration.ViewModels.Chuck;
using CugaCalibration.ViewModels.Microscope;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Laser;

public sealed partial class LaserOpticalPowerMeterViewModel
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

        var microscopeFocusViewModel = HostApplication.GetRequiredService<MicroscopeFocusCalibrationViewModel>();
        if (microscopeFocusViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {microscopeFocusViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var microscopeCalChipViewModel = HostApplication.GetRequiredService<MicroscopeCalChipViewModel>();
        if (microscopeCalChipViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {microscopeCalChipViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var microscopePixelSizeViewModel = HostApplication.GetRequiredService<MicroscopePixelSizeCalibrationViewModel>();
        if (microscopePixelSizeViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {microscopePixelSizeViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var microscopeCentricityViewModel = HostApplication.GetRequiredService<MicroscopeCentricityCalibrationViewModel>();
        if (microscopeCentricityViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {microscopeCentricityViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var chuckGantryViewModel = HostApplication.GetRequiredService<ChuckGantryCalibrationViewModel>();
        if (chuckGantryViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {chuckGantryViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var chuckGlobalScaleErrorViewModel = HostApplication.GetRequiredService<ChuckGlobalScaleErrorCalibrationViewModel>();
        if (chuckGlobalScaleErrorViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {chuckGlobalScaleErrorViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var chuckCenterAndThetaViewModel = HostApplication.GetRequiredService<ChuckCenterAndThetaCalibrationViewModel>();
        if (chuckCenterAndThetaViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {chuckCenterAndThetaViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var chuckPrealignerViewModel = HostApplication.GetRequiredService<ChuckPrealignerCalibrationViewModel>();
        if (chuckPrealignerViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {chuckPrealignerViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var laserBeamStabilizerViewModel = HostApplication.GetRequiredService<LaserBeamStabilizerCalibrationViewModel>();
        if (laserBeamStabilizerViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {laserBeamStabilizerViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}