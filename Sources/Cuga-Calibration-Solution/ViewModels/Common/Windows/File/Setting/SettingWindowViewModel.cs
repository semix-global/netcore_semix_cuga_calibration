using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting;

[IOCAppService(ServiceType = typeof(SettingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SettingWindowViewModel : ViewModelBase
{
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly CalibrationSetting _calibrationSetting;
    private readonly ICacheProvider _cacheProvider;

    [ObservableProperty]
    private SettingCommonViewModel _settingCommonViewModel = HostApplication.GetRequiredService<SettingCommonViewModel>();

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _lowMagSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _middleMagSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private SettingDarkFieldGainViewModel _highMagSettingDarkFieldGainViewModel = HostApplication.GetRequiredService<SettingDarkFieldGainViewModel>();

    [ObservableProperty]
    private SettingTemplateMatchViewModel _settingTemplateMatchViewModel = HostApplication.GetRequiredService<SettingTemplateMatchViewModel>();

    [ObservableProperty]
    private SettingCalibrateItemsStatusViewModel _settingCalibrateItemsStatusViewModel = HostApplication.GetRequiredService<SettingCalibrateItemsStatusViewModel>();

    [ObservableProperty]
    private SettingPmtConfigViewModel _settingPmtConfigViewModel = HostApplication.GetRequiredService<SettingPmtConfigViewModel>();

    [ObservableProperty]
    private SettingRequiredCalibrationViewModel _settingRequiredCalibrationViewModel = HostApplication.GetRequiredService<SettingRequiredCalibrationViewModel>();

    public SettingWindowViewModel(
        IDialogWindowProvider dialogWindowProvider,
        CalibrationSetting calibrationSetting,
        ICacheProvider cacheProvider,
        ApplicationCookie applicationCookie)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _calibrationSetting = calibrationSetting;
        _cacheProvider = cacheProvider;

        SettingCommonViewModel.SettingCommonParam = _calibrationSetting.SettingCommonParam;
        SettingCommonViewModel.SettingCommonParam.IsDebugEnvironment = true; // todo:更改为管理员权限
        SettingTemplateMatchViewModel.SettingTemplateMatchParam = _calibrationSetting.SettingTemplateMatchParam;
        LowMagSettingDarkFieldGainViewModel.SettingDarkFieldGainParamList = _calibrationSetting.LowMagSettingDarkFieldGainParam;
        LowMagSettingDarkFieldGainViewModel.OpticsMagTypeEnum = OpticsMagTypeEnum.Low;
        MiddleMagSettingDarkFieldGainViewModel.SettingDarkFieldGainParamList = _calibrationSetting.MiddleMagSettingDarkFieldGainParam;
        MiddleMagSettingDarkFieldGainViewModel.OpticsMagTypeEnum = OpticsMagTypeEnum.Middle;
        HighMagSettingDarkFieldGainViewModel.SettingDarkFieldGainParamList = _calibrationSetting.HighMagSettingDarkFieldGainParam;
        HighMagSettingDarkFieldGainViewModel.OpticsMagTypeEnum = OpticsMagTypeEnum.High;
        foreach (var pmtId in CalibrationConstantsHelper.PmtIds)
        {
            foreach (var channel in CalibrationConstantsHelper.ChannelIds)
            {
                var lowMagSettingDarkField = _calibrationSetting.LowMagSettingDarkFieldGainParam.FirstOrDefault(t => t.PmtId == pmtId && t.ChannelId == channel);
                if (lowMagSettingDarkField is null) _calibrationSetting.LowMagSettingDarkFieldGainParam.Add(new SettingDarkFieldGainParam { PmtId = pmtId, ChannelId = channel });

                var middleMagSettingDarkField = _calibrationSetting.MiddleMagSettingDarkFieldGainParam.FirstOrDefault(t => t.PmtId == pmtId && t.ChannelId == channel);
                if (middleMagSettingDarkField is null) _calibrationSetting.MiddleMagSettingDarkFieldGainParam.Add(new SettingDarkFieldGainParam { PmtId = pmtId, ChannelId = channel });

                var highMagSettingDarkField = _calibrationSetting.HighMagSettingDarkFieldGainParam.FirstOrDefault(t => t.PmtId == pmtId && t.ChannelId == channel);
                if (highMagSettingDarkField is null) _calibrationSetting.HighMagSettingDarkFieldGainParam.Add(new SettingDarkFieldGainParam { PmtId = pmtId, ChannelId = channel });
            }
        }

        foreach (var (_, coefficient) in applicationCookie.LaserLightInformations)
        {
            foreach (var lowItem in _calibrationSetting.LowMagSettingDarkFieldGainParam)
            {
                var gainCoefficientsParam = lowItem.GainOfCoefficientList.SingleOrDefault(t => t.Coefficient - coefficient == 0);
                if (gainCoefficientsParam is null) lowItem.GainOfCoefficientList.Add(new GainOfCoefficientParam { Coefficient = coefficient });
            }

            foreach (var middleItem in _calibrationSetting.MiddleMagSettingDarkFieldGainParam)
            {
                var gainCoefficientsParam = middleItem.GainOfCoefficientList.SingleOrDefault(t => t.Coefficient - coefficient == 0);
                if (gainCoefficientsParam is null) middleItem.GainOfCoefficientList.Add(new GainOfCoefficientParam { Coefficient = coefficient });
            }

            foreach (var highItem in _calibrationSetting.HighMagSettingDarkFieldGainParam)
            {
                var gainCoefficientsParam = highItem.GainOfCoefficientList.SingleOrDefault(t => t.Coefficient - coefficient == 0);
                if (gainCoefficientsParam is null) highItem.GainOfCoefficientList.Add(new GainOfCoefficientParam { Coefficient = coefficient });
            }
        }

        if (_calibrationSetting.SettingPmtConfigParam.PmtConfigList.Count == 0)
        {
            var pmtConfigList = Enumerable.Range(1, 15)
                .Select(i => new PmtConfigParam { Id = i })
                .ToList();
            _calibrationSetting.SettingPmtConfigParam.PmtConfigList = [.. pmtConfigList];
        }

        SettingPmtConfigViewModel.SettingPmtConfigParam = _calibrationSetting.SettingPmtConfigParam;
    }

    [RelayCommand]
    private void Restore()
    {
        _calibrationSetting.AdaptIn(_cacheProvider.GetOrDefault<CalibrationSetting>());
        SettingCommonViewModel.SettingCommonParam = _calibrationSetting.SettingCommonParam;
        SettingTemplateMatchViewModel.SettingTemplateMatchParam = _calibrationSetting.SettingTemplateMatchParam;
        SettingPmtConfigViewModel.SettingPmtConfigParam = _calibrationSetting.SettingPmtConfigParam;
        SettingCommonViewModel.SettingCommonParam.IsDebugEnvironment = true; // todo:更改为管理员权限
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (await SettingCalibrateItemsStatusViewModel.SavingAsync().ConfigureAwait(false) == false)
        {
            _dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        if (await SettingRequiredCalibrationViewModel.SavingAsync().ConfigureAwait(false) == false)
        {
            _dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        if (SaveSetting() == false)
        {
            _dialogWindowProvider.ShowDialog("Save Failed, Please try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        CloseView(true);
    }

    [RelayCommand]
    private void Close()
    {
        SettingCalibrateItemsStatusViewModel.Closing();
        CloseView(true);
    }

    public bool SaveSetting()
    {
        try
        {
            _cacheProvider.Set(_calibrationSetting, CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}