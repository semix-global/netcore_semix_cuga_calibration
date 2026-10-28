using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapCache : CalibrationCacheBase
{
    #region Common

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    [ObservableProperty]
    public partial bool IsROIMatchEnabled { get; set; }

    [ObservableProperty]
    public partial double ROIMatchWidthScale { get; set; } = 1d;

    [ObservableProperty]
    public partial double ROIMatchHeightScale { get; set; } = 1d;

    #endregion

    #region Step1

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000d;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 5100d;

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; } = 16600d;

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    #endregion

    #region Step3

    [ObservableProperty]
    public partial int StageMapRepeatTimes { get; set; } = 10;

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial StageMap StageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMap[] RepeatStageMaps { get; set; } = [];

    [ObservableProperty]
    public partial StageMap VerifyStageMap { get; set; } = new();

    #endregion

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;

        using var scope = StageMapDocument.View.Sync.EnterScope();
        StageMapDocument.Reset();
    }

    partial void OnRepeatStageMapsChanged(StageMap[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation = new HtmlQuote(ProductivityInformation.ToHtmlAnonymous()),
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
        CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
        TemplateMatchSetting,
        IsROIMatchEnabled,
        ROIMatchWidthScale,
        ROIMatchHeightScale,
        ImageWidth,
        WaferRadius,
        DiePitchWidth,
        DiePitchHeight,
        StageMapRepeatTimes
    };
}