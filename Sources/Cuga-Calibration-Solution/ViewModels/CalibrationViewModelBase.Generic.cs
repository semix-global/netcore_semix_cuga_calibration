using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Serializations;
using Net.Utilities.WPF.Enums;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    public async Task ExportAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (DialogWindowProvider.TryShowSaveFilePathDialog(".json", out var exportPath) != true) return;

                ApplicationCookie.CalibrationViewModelEntries.TryGetValue(GetType(), out var entry);
                Guard.IsNotNull(entry, "The match entry is null.");

                var cache = ObjectHelper.GetPropertyValue(this, nameof(CalibrationViewModelBase<>.Cache));
                Guard.IsNotNull(cache, "The export cache is not exit! Please check the viewmodel.");

                var version = entry.DTOType.GetCustomAttribute<CacheVersionAttribute>();
                Guard.IsNotNull(version, "The CalibrateDTO's version attribute is null.");

                var exportData = new JObject
                {
                    [nameof(ICacheItem.CreatedTime)] = DateTime.Now,
                    [nameof(version.Version)] = version.Version,
                    [nameof(entry.CacheType)] = JObject.FromObject(cache, PrivateSetterContractResolver.Serializer),
                };

                var json = exportData.ToString(Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(exportPath, json);

                DialogWindowProvider.ShowDialog("Export cache success!");
            }, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Export cache failed!");
            DialogWindowProvider.ShowDialog("Export cache failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    public async Task ImportAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (DialogWindowProvider.TryShowSelectFilePathDialog(".json", out var importPath) == false) return;

                ApplicationCookie.CalibrationViewModelEntries.TryGetValue(GetType(), out var entry);
                Guard.IsNotNull(entry, "The match entry is null.");

                var version = entry.DTOType.GetCustomAttribute<CacheVersionAttribute>();
                Guard.IsNotNull(version, "The CalibrateDTO's version attribute is null.");

                var content = File.ReadAllText(importPath);
                var importData = JObject.Parse(content);

                var versionToken = importData[nameof(version.Version)];
                var cacheVersion = GuardExtensions.IsNotNullAndReturn(versionToken, "Version is empty...").ToObject<string>();

                if (cacheVersion is null || new Version(cacheVersion) != new Version(version.Version)) ThrowHelper.ThrowArgumentException("Version mismatch...");

                var cacheToken = importData[nameof(entry.CacheType)];
                if (cacheToken == null) ThrowHelper.ThrowArgumentException<CalibrationCacheBase>("Cache data missing.");

                var cache = cacheToken.ToObject(entry.CacheType, PrivateSetterContractResolver.Serializer);

                ObjectHelper.SetPropertyValue(this, nameof(CalibrationViewModelBase<>.Cache), cache);

                DialogWindowProvider.ShowDialog("Import cache success!");
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import cache failed!");
            DialogWindowProvider.ShowDialog("Import cache failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }
}

public abstract class CalibrationViewModelBase<TCache> : CalibrationViewModelBase where TCache : ObservableCacheBase
{
    public abstract TCache Cache { get; set; }
}