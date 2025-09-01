using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Prealigner;

public sealed partial class ChuckPrealignerCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation =  MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _lowSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _highSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    private double _nccTypeTemplateMatchScoreThreshold = 0.8;

    /// <summary>
    /// 低倍率mark点1位置(wafer中间掩模版芯粒左上角)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite1 = new();

    /// <summary>
    /// 低倍率mark点2位置(mark点1的相邻掩模版芯粒左上角)[没有模板, 用低倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite2 = new();

    /// <summary>
    /// 高倍率mark点1位置(低倍率mark点1的精细位置)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite1 = new();

    /// <summary>
    /// 高倍率mark点2位置(低倍率mark点2的精细位置)[没有模板, 用高倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite2 = new();

    [ObservableProperty]
    private Point _offsetPosition;

    [ObservableProperty]
    private double _offsetAngle;

    [ObservableProperty]
    private Point _efemLoadWaferStagePosition;

    [ObservableProperty]
    private double _efemLoadWaferChuckAngle;

    [ObservableProperty]
    private double _angleErrorThreshold = 0.3;

    [ObservableProperty]
    private double _angleThreshold = 0.1;

    [ObservableProperty]
    private double _positionThreshold = 100;

    [ObservableProperty]
    private double _positionErrorThreshold = 300;

    [ObservableProperty]
    private Point _findWaferCenterOffset1;

    [ObservableProperty]
    private Point _findWaferCenterOffset2;

    [ObservableProperty]
    private Point _findWaferCenterOffset3;

    [ObservableProperty]
    private Point _findWaferCenterOffset4;

    [ObservableProperty]
    private Point _findWaferCenterOffset5;

    [ObservableProperty]
    private Point _findWaferCenterOffset6;

    [ObservableProperty]
    private Point _findWaferCenterOffset7;

    [ObservableProperty]
    private Point _findWaferCenterOffset8;

    [ObservableProperty]
    private Point _lowFindPosition1;

    [ObservableProperty]
    private Point _lowFindPosition2;

    [ObservableProperty]
    private Point _highFindPosition1;

    [ObservableProperty]
    private Point _highFindPosition2;

    [ObservableProperty]
    private string _lowTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateImageFilePath = string.Empty;

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb1 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb2 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb3 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb4 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb5 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb6 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb7 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb8 = [];
}