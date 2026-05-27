using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingDisableCalibrationConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SettingDisableCalibrationConfigViewModel(
    ICacheProvider cacheProvider,
    ICalibrationCacheProvider calibrationCacheProvider,
    ILogger<SettingDisableCalibrationConfigViewModel> logger,
    IDialogWindowProvider dialogWindowProvider)
    : ViewModelBase
{
    [ObservableProperty]
    private SettingDisableCalibrationConfig? _configItem;

    private SettingDisableCalibrationConfig? _originalSnapshot;

    partial void OnConfigItemChanged(SettingDisableCalibrationConfig? value)
    {
        _originalSnapshot = value?.Clone();
    }

    /// <summary>
    /// 保存配置到缓存
    /// </summary>
    public async Task<bool> SavingAsync()
    {
        try
        {
            if (ConfigItem is null) return true;

            if (IsUnchanged()) return true;

            await Task.Run(() => { Save(ConfigItem, CancellationToken.None); });

            _originalSnapshot = ConfigItem.Clone();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving ConfigItem Failed!");
            dialogWindowProvider.ShowDialog("Saving ConfigItem Failed!");
            return false;
        }
    }

    private bool IsUnchanged() => _originalSnapshot?.Equals(ConfigItem) ?? false;

    private void Save(SettingDisableCalibrationConfig cache, CancellationToken cancellationToken) => calibrationCacheProvider.InvokeSave(update =>
    {
        var caches = cacheProvider.GetOrDefaultArray<SettingDisableCalibrationConfig>();

        update(cache);

        caches = [cache, .. caches.Where(t => t.ConfigDescription != cache.ConfigDescription)];

        cacheProvider.SetArray(caches, cancellationToken);

        return true;
    }, nameof(SettingDisableCalibrationConfig), cancellationToken);
}