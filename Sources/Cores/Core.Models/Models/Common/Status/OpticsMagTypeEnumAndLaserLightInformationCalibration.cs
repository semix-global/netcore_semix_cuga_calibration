using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsMagTypeEnumAndLaserLightInformationCalibration : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private BindingList<LaserLightInformationStatus1> _laserLightInformationStatusList = [];

    public bool IsCalibrated => LaserLightInformationStatusList.All(c => c.IsCalibrated);

    partial void OnLaserLightInformationStatusListChanged(BindingList<LaserLightInformationStatus1>? oldValue, BindingList<LaserLightInformationStatus1> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}

public sealed partial class LaserLightInformationStatus1 : ObservableCacheBase
{
    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<LaserLightInformationStatus1> CreateList(IReadOnlyList<LaserLightInformation> laserLightInformationList) =>
        laserLightInformationList.Select(t => new LaserLightInformationStatus1 { LaserLightInformation = t, IsCalibrated = false }).ToList();
}