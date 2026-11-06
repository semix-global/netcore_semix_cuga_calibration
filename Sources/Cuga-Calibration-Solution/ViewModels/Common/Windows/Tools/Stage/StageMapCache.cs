using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models;
using Core.Models.Models.Chuck.StageMap;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapCache : CalibrationCacheBase
{
    [ObservableProperty]
    public partial ChuckStageMapCache ChuckStageMapCache { get; set; } = new();

    [ObservableProperty]
    public partial ChuckStageMapDTO ChuckStageMap { get; set; } = new();

    partial void OnChuckStageMapCacheChanged(ChuckStageMapCache oldValue, ChuckStageMapCache newValue)
    {
        oldValue.ChuckStageMapCacheDFItem.PropertyChanged -= TemplatesOnPropertyChanged;

        newValue.ChuckStageMapCacheDFItem.PropertyChanged -= TemplatesOnPropertyChanged;
        newValue.ChuckStageMapCacheDFItem.PropertyChanged += TemplatesOnPropertyChanged;

        return;

        void TemplatesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ChuckStageMapCacheDFItem.StageMapTemplates):
                    using (ChuckStageMap.DFStageMapDocument.View.Sync.EnterScope())
                    {
                        ChuckStageMap.DFStageMapDocument.Reset();
                    }

                    using (ChuckStageMap.BFStageMapDocument.View.Sync.EnterScope())
                    {
                        ChuckStageMap.BFStageMapDocument.Reset();
                    }

                    break;
            }
        }
    }

    partial void OnChuckStageMapChanged(ChuckStageMapDTO oldValue, ChuckStageMapDTO newValue)
    {
        oldValue.PropertyChanged -= TemplatesOnPropertyChanged;

        newValue.PropertyChanged -= TemplatesOnPropertyChanged;
        newValue.PropertyChanged += TemplatesOnPropertyChanged;

        return;

        void TemplatesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ChuckStageMap.IsDarkFieldAlignment):
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapTemplates = [];

                    break;
            }
        }
    }

    public object ToHtmlAnonymous(bool isDark)
    {
        if (isDark)
            return new
            {
                ChuckStageMapCache.MicroscopeLensInformation,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.TemplateMatchSetting,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.WaferRadius,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.DiePitchWidth,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.DiePitchHeight,
                ProductivityInformation = new HtmlQuote(ChuckStageMapCache.ChuckStageMapCacheDFItem.ProductivityInformation.ToHtmlAnonymous()),
                ChuckStageMapCache.ChuckStageMapCacheDFItem.LaserLightInformation,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.CIBInformation,
                OpticsConfiguration = new HtmlQuote(ChuckStageMapCache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(ChuckStageMapCache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                ChuckStageMapCache.ChuckStageMapCacheDFItem.ImageWidth,
                ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapRepeatTimes
            };

        return new
        {
            ChuckStageMapCache.MicroscopeLensInformation,
            ChuckStageMapCache.ChuckStageMapCacheBFItem.TemplateMatchSetting,
            ChuckStageMapCache.ChuckStageMapCacheBFItem.WaferRadius,
            ChuckStageMapCache.ChuckStageMapCacheBFItem.DiePitchWidth,
            ChuckStageMapCache.ChuckStageMapCacheBFItem.DiePitchHeight
        };
    }
}