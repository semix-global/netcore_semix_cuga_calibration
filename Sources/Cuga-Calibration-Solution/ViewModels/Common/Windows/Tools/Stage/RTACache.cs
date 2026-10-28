using CommunityToolkit.Mvvm.ComponentModel;
using Core.Services.Interfaces;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class RTACache : CalibrationCacheBase
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
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    #endregion

    #region Step1

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000d;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 5100d;

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; } = 16600d;

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    #endregion

    #region Step2

    [ObservableProperty]
    public partial ICalibrationAlgorithmService.StageMapConfigDTO StageMapConfig { get; set; } = new();

    [ObservableProperty]
    public partial string ForwardSimulatorImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReverseSimulatorImageFilePath { get; set; } = string.Empty;

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial RTAScanResult RTAScanResult { get; set; } = new();

    #endregion

    partial void OnCalChipSiteModelEnumChanged(CalChipSiteModelEnum value)
    {
        _ = value;

        StageMapTemplates = [];
    }

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;

        StageMapDocument.Reset();
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation = new HtmlQuote(ProductivityInformation.ToHtmlAnonymous()),
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        CalChipSiteModelEnum,
        OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
        CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
        WaferRadius,
        DiePitchWidth,
        DiePitchHeight,
        StageMapConfig = new HtmlQuote(StageMapConfig.ToHtmlAnonymous()),
        ForwardSimulatorImageFilePath,
        ReverseSimulatorImageFilePath,
    };
}