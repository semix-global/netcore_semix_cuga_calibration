using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus : ObservableObject
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private BindingList<StageSpeedEnumCalibrationStatus> _stageSpeedEnumCalibrationStatusList = [];

    public bool IsCalibrated => StageSpeedEnumCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnStageSpeedEnumCalibrationStatusListChanged(BindingList<StageSpeedEnumCalibrationStatus>? oldValue, BindingList<StageSpeedEnumCalibrationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? sender, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}