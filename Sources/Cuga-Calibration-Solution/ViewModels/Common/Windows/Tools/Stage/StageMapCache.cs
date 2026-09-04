using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Newtonsoft.Json;

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
    public partial bool IsROIMatchEnabled { get; set; }

    [ObservableProperty]
    public partial double ROIMatchWidthScale { get; set; } = 1d;

    [ObservableProperty]
    public partial double ROIMatchHeightScale { get; set; } = 1d;

    #endregion

    #region Step0

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    #endregion

    #region Step1

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double WaferRadius { get; set; }

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; }

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    #endregion

    #region Step3

    [ObservableProperty]
    public partial int StageMapRepeatTimes { get; set; } = 10;

    [ObservableProperty]
    public partial double AlgorithmStageMapResidualAlpha { get; set; } = 0.4d;

    [ObservableProperty]
    public partial int AlgorithmStageMapMinimumRetryCount { get; set; } = 5;

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial StageMap StageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMap[] RepeatStageMaps { get; set; } = [];

    [ObservableProperty]
    public partial StageMap VerifyStageMap { get; set; } = new();

    #endregion

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        OpticsConfiguration,
        CIBConfiguration,
        AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum,
        IsROIMatchEnabled,
        ROIMatchWidthScale,
        ROIMatchHeightScale,
        IsDarkFieldAlignment,
        ImageWidth,
        WaferRadius,
        DiePitchWidth,
        DiePitchHeight,
        StageMapRepeatTimes,
        AlgorithmStageMapResidualAlpha,
        AlgorithmStageMapMinimumRetryCount
    };
}