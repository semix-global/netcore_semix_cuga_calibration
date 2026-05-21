using CugaCalibration.ViewModels.Ads;
using CugaCalibration.ViewModels.AOD;
using CugaCalibration.ViewModels.AutoFocus;
using CugaCalibration.ViewModels.Chuck;
using CugaCalibration.ViewModels.Laser;
using CugaCalibration.ViewModels.Microscope;
using CugaCalibration.ViewModels.Optics;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.CIB;

public sealed partial class CIBIlluminationProfileViewModel
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

        var laserOpticalPowerMeterViewModel = HostApplication.GetRequiredService<LaserOpticalPowerMeterViewModel>();
        if (laserOpticalPowerMeterViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {laserOpticalPowerMeterViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var laserAttenuatorViewModel = HostApplication.GetRequiredService<LaserAttenuatorViewModel>();
        if (laserAttenuatorViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {laserAttenuatorViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var opticsRelayViewModel = HostApplication.GetRequiredService<OpticsRelayViewModel>();
        if (opticsRelayViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {opticsRelayViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var autoFocusDarkAutoFocusViewModel = HostApplication.GetRequiredService<AutoFocusDarkAutoFocusViewModel>();
        if (autoFocusDarkAutoFocusViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {autoFocusDarkAutoFocusViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var opticsINCViewModel = HostApplication.GetRequiredService<OpticsINCViewModel>();
        if (opticsINCViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {opticsINCViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var cibMMDViewModel = HostApplication.GetRequiredService<CIBMMDViewModel>();
        if (cibMMDViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {cibMMDViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var aodAlignmentViewModel = HostApplication.GetRequiredService<AODAlignmentViewModel>();
        if (aodAlignmentViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {aodAlignmentViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var aodDelayViewModel = HostApplication.GetRequiredService<AODDelayViewModel>();
        if (aodDelayViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {aodDelayViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var opticsGlobalFieldTiltViewModel = HostApplication.GetRequiredService<OpticsGlobalFieldTiltViewModel>();
        if (opticsGlobalFieldTiltViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {opticsGlobalFieldTiltViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var cIBXTCViewModel = HostApplication.GetRequiredService<CIBXTCViewModel>();
        if (cIBXTCViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {cIBXTCViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var aodUniformityViewModel = HostApplication.GetRequiredService<AODUniformityViewModel>();
        if (aodUniformityViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {aodUniformityViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var aodBestFocusAndAstigmatismViewModel = HostApplication.GetRequiredService<AODBestFocusAndAstigmatismViewModel>();
        if (aodBestFocusAndAstigmatismViewModel.Entry.Status.IsOk == false)
        {
            DialogWindowProvider.ShowDialog($"The {aodBestFocusAndAstigmatismViewModel.Name} precondition is Not Ok", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return true;
    }
}