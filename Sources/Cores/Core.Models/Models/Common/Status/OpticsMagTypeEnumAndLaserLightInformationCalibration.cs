using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;
using Core.Models.Models.Common.Pattern;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsMagTypeEnumAndLaserLightInformationCalibration : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private BindingList<LaserLightInformationStatus> _laserLightInformationStatusList = [];

    public bool IsCalibrated => LaserLightInformationStatusList.All(c => c.IsCalibrated);

    partial void OnLaserLightInformationStatusListChanged(BindingList<LaserLightInformationStatus>? oldValue, BindingList<LaserLightInformationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}

public sealed partial class LaserLightInformationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<LaserLightInformationStatus> CreateList(IReadOnlyList<LaserLightInformation> laserLightInformationList) =>
        laserLightInformationList.Select(t => new LaserLightInformationStatus { LaserLightInformation = t, IsCalibrated = false }).ToList();
}