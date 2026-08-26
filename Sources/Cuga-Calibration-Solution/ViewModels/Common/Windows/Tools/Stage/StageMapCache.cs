using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics;
using Net.Utilities.Models.Geometries;
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

    #endregion

    #region Step0

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    #endregion

    #region Step1

    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; }

    [ObservableProperty]
    public partial double WaferRadius { get; set; }

    [ObservableProperty]
    public partial StageMapTemplatePoint[] StageMapTemplatePoints { get; set; } = [];

    #endregion

    #region Step4

    [ObservableProperty]
    public partial int StageMapRetryCount { get; set; } = 20;

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMap StageMap { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial IReadOnlyList<StageMap> RepeatStageMaps { get; set; } = [];

    [JsonIgnore]
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
        IsDarkFieldAlignment,
        AlgorithmTemplateTypeEnum,
        ImageWidth,
        DiePitchWidth,
        DiePitchHeight,
        WaferRadius,
        StageMapRetryCount
    };
}

public sealed partial class StageMapTemplatePoint : ObservableObject
{
    [ObservableProperty]
    public partial Point DFPosition { get; set; }

    [ObservableProperty]
    public partial Rect ROI { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;
}