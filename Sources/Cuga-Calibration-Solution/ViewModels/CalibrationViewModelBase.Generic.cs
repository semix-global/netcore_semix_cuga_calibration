using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Serializations;
using Net.Utilities.WPF.Enums;
using Newtonsoft.Json.Linq;
using System.IO;
using Local.SQL.Cache.Providers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;

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

                var version = SQLiteHelper.GetTableInfo(Entry.CacheType);

                var exportData = new JObject
                {
                    [nameof(ICacheItem.CreatedTime)] = DateTime.Now,
                    [nameof(version.Version)] = version.Version,
                    [nameof(CalibrationViewModelBase<>.Cache)] = JObject.FromObject(ObjectHelper.GetPropertyValue<CalibrationCacheBase>(this, nameof(CalibrationViewModelBase<>.Cache)), PrivateSetterContractResolver.Serializer),
                };

                File.WriteAllText(exportPath, exportData.ToString(Newtonsoft.Json.Formatting.Indented));

                DialogWindowProvider.ShowDialog("Export cache success!");
            });
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

                var version = SQLiteHelper.GetTableInfo(Entry.CacheType);

                var importData = JObject.Parse(File.ReadAllText(importPath));

                var versionToken = importData[nameof(version.Version)]?.ToObject<string>();
                Guard.IsNotNull(versionToken);
                Guard.IsTrue(versionToken == version.Version);

                var cacheToken = importData[nameof(CalibrationViewModelBase<>.Cache)];
                Guard.IsNotNull(cacheToken);

                var cache = cacheToken.ToObject(Entry.CacheType, PrivateSetterContractResolver.Serializer);

                ObjectHelper.SetPropertyValue(this, nameof(CalibrationViewModelBase<>.Cache), cache);

                DialogWindowProvider.ShowDialog("Import cache success!");
            }).ConfigureAwait(false);
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