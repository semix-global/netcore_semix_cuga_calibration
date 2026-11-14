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
        oldValue.ChuckStageMapCacheDFItem.PropertyChanged -= ChuckStageMapCacheOnPropertyChanged;

        newValue.ChuckStageMapCacheDFItem.PropertyChanged -= ChuckStageMapCacheOnPropertyChanged;
        newValue.ChuckStageMapCacheDFItem.PropertyChanged += ChuckStageMapCacheOnPropertyChanged;

        return;

        void ChuckStageMapCacheOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ChuckStageMapCacheBFItem.StageMapTemplates):
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
        oldValue.PropertyChanged -= ChuckStageMapDTOOnPropertyChanged;

        newValue.PropertyChanged -= ChuckStageMapDTOOnPropertyChanged;
        newValue.PropertyChanged += ChuckStageMapDTOOnPropertyChanged;

        return;

        void ChuckStageMapDTOOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                DFParam = new HtmlQuote(new
                {
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.CIBInformation,
                    OpticsConfiguration = new HtmlQuote(ChuckStageMapCache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(ChuckStageMapCache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.ImageWidth,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapRepeatTimes,
                    ChuckStageMapCache.ChuckStageMapCacheBFItem.TemplateMatchSetting,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.WaferRadius,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.DiePitchWidth,
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                                TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        })
                    ])
                })
            };

        return new
        {
            ChuckStageMapCache.MicroscopeLensInformation,
            ChuckStageMapCache.Threshold,
            BFParam = new HtmlQuote(new
            {
                ChuckStageMapCache.ChuckStageMapCacheBFItem.TemplateMatchSetting,
                ChuckStageMapCache.ChuckStageMapCacheBFItem.WaferRadius,
                ChuckStageMapCache.ChuckStageMapCacheBFItem.DiePitchWidth,
                ChuckStageMapCache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. ChuckStageMapCache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            })
        };
    }
}