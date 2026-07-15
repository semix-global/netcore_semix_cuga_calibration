using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting.CalibrationRelationConfig;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRelationCalibrationDetailViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingRelationCalibrationDetailViewModel : ViewModelBase
{
    private IReadOnlyList<SettingCalibrationRelationConfig> _originalDependencyConfigs = [];

    [ObservableProperty]
    public partial SettingCalibrationRelationParam SelectedParam { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<SettingCalibrationRelationConfig> DependencyRelationConfigs { get; set; } = [];

    partial void OnSelectedParamChanged(SettingCalibrationRelationParam value)
    {
        DependencyRelationConfigs = value.Item.DependencyRelationConfigs;
    }

    [RelayCommand]
    private void Loaded()
    {
        _originalDependencyConfigs = DependencyRelationConfigs.Select(t => t.Clone()).ToList().AsReadOnly();
    }

    [RelayCommand]
    private void Save()
    {
        CloseView(true);
    }

    [RelayCommand]
    private void CloseDetail()
    {
        RestoreConfigs(_originalDependencyConfigs, DependencyRelationConfigs);
        CloseView(false);
    }

    [RelayCommand]
    private void SelectAll()
    {
        SetAll(DependencyRelationConfigs, true);
    }

    [RelayCommand]
    private void ClearAll()
    {
        SetAll(DependencyRelationConfigs, false);
    }

    private static void SetAll(IEnumerable<SettingCalibrationRelationConfig> configs, bool isUsed)
    {
        foreach (var config in configs)
        {
            config.Item.IsUsed = isUsed;
            SetAll(config.Children, isUsed);
        }
    }

    private static void RestoreConfigs(IReadOnlyList<SettingCalibrationRelationConfig> source, IReadOnlyList<SettingCalibrationRelationConfig> target)
    {
        foreach (var targetRoot in target)
        {
            var sourceRoot = source.FirstOrDefault(s => s.SysMenu.Id == targetRoot.SysMenu.Id);
            if (sourceRoot is not null) RestoreNode(sourceRoot, targetRoot);
        }
    }

    private static void RestoreNode(SettingCalibrationRelationConfig source, SettingCalibrationRelationConfig target)
    {
        target.Item.IsUsed = source.Item.IsUsed;
        foreach (var targetChild in target.Children)
        {
            var sourceChild = source.Children.FirstOrDefault(s => s.SysMenu.Id == targetChild.SysMenu.Id);
            if (sourceChild is not null) RestoreNode(sourceChild, targetChild);
        }
    }
}
