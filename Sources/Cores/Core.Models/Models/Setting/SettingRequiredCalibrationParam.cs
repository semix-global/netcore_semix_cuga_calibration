using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting;

public sealed partial class SettingRequiredCalibrationParam : ObservableCacheBase, ICloneable<SettingRequiredCalibrationParam>, IAdaptIn<SettingRequiredCalibrationParam, SettingRequiredCalibrationParam>
{
    [ObservableProperty]
    [property: System.ComponentModel.Description(Wcf.Models.WcfConstantHelper.AdsNodeCalibrationName)]
    private ObservableCollection<RequiredCalibrationParam> _adsRequiredCalibrationList = [];

    [ObservableProperty]
    [property: System.ComponentModel.Description(Wcf.Models.WcfConstantHelper.MicroscopeNodeCalibrationName)]
    private ObservableCollection<RequiredCalibrationParam> _microscopeRequiredCalibrationList = [];

    [ObservableProperty]
    [property: System.ComponentModel.Description(Wcf.Models.WcfConstantHelper.ChuckNodeCalibrationName)]
    private ObservableCollection<RequiredCalibrationParam> _chuckRequiredCalibrationList = [];

    [ObservableProperty]
    [property: System.ComponentModel.Description(Wcf.Models.WcfConstantHelper.LaserNodeCalibrationName)]
    private ObservableCollection<RequiredCalibrationParam> _laserRequiredCalibrationList = [];

    #region Mapper

    public SettingRequiredCalibrationParam AdaptIn(SettingRequiredCalibrationParam obj)
    {
        AdsRequiredCalibrationList = [.. obj.AdsRequiredCalibrationList.Select(x => new RequiredCalibrationParam().AdaptIn(x))];
        MicroscopeRequiredCalibrationList = [.. obj.MicroscopeRequiredCalibrationList.Select(x => new RequiredCalibrationParam().AdaptIn(x))];
        ChuckRequiredCalibrationList = [.. obj.ChuckRequiredCalibrationList.Select(x => new RequiredCalibrationParam().AdaptIn(x))];
        LaserRequiredCalibrationList = [.. obj.LaserRequiredCalibrationList.Select(x => new RequiredCalibrationParam().AdaptIn(x))];

        return obj;
    }

    public SettingRequiredCalibrationParam Clone() => new()
    {
        AdsRequiredCalibrationList = [.. AdsRequiredCalibrationList.Select(t => t)],
        MicroscopeRequiredCalibrationList = [.. MicroscopeRequiredCalibrationList.Select(t => t)],
        ChuckRequiredCalibrationList = [.. ChuckRequiredCalibrationList.Select(t => t)],
        LaserRequiredCalibrationList = [.. LaserRequiredCalibrationList.Select(t => t)]
    };

    #endregion Mapper
}

public sealed partial class RequiredCalibrationParam : ObservableCacheBase, IAdaptIn<RequiredCalibrationParam, RequiredCalibrationParam>
{
    [ObservableProperty]
    private bool _isRequired;

    [ObservableProperty]
    private string _calibrationName = string.Empty;

    [ObservableProperty]
    private string _calibrationClassName = string.Empty;

    #region Mapper

    public RequiredCalibrationParam AdaptIn(RequiredCalibrationParam obj)
    {
        IsRequired = obj.IsRequired;
        Id = obj.Id;

        return obj;
    }

    #endregion Mapper
}