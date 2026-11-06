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
    public partial ChuckStageMapDTO ChuckStageMapDTO { get; set; } = new();

    partial void OnChuckStageMapCacheChanged(ChuckStageMapCache oldValue, ChuckStageMapCache newValue)
    {
        oldValue.ChuckStageMapCacheDFItem.PropertyChanged -= TemplatesOnPropertyChanged;

        newValue.ChuckStageMapCacheDFItem.PropertyChanged -= TemplatesOnPropertyChanged;
        newValue.ChuckStageMapCacheDFItem.PropertyChanged += TemplatesOnPropertyChanged;

        return;

        void TemplatesOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ChuckStageMapCacheDFItem.StageMapTemplates):
                    using (ChuckStageMapDTO.DFStageMapDocument.View.Sync.EnterScope())
                    {
                        ChuckStageMapDTO.DFStageMapDocument.Reset();
                    }

                    break;
            }
        }
    }

    partial void OnChuckStageMapDTOChanged(ChuckStageMapDTO oldValue, ChuckStageMapDTO newValue)
    {
        oldValue.PropertyChanged -= TemplatesOnPropertyChanged;

        newValue.PropertyChanged -= TemplatesOnPropertyChanged;
        newValue.PropertyChanged += TemplatesOnPropertyChanged;

        return;

        void TemplatesOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ChuckStageMapDTO.IsDarkFieldAlignment):
                    ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapTemplates = [];

                    break;
            }
        }
    }

    public object ToHtmlAnonymous() => new
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
}