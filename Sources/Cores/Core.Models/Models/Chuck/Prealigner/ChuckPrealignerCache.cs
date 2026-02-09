using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Prealigner;

public sealed partial class ChuckPrealignerCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

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
    private string _lowSiteTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highSiteTemplateFilePath = string.Empty;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Wafer radius must be greater than 0.1.")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 5100;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [ObservableProperty]
    private Point _offsetPosition;

    [ObservableProperty]
    private double _degrees;

    [ObservableProperty]
    private Point _efemLoadWaferStagePosition;

    [ObservableProperty]
    private double _efemLoadWaferChuckAngle;

    [ObservableProperty]
    private double _teachingDegreesThreshold = 0.3;

    [ObservableProperty]
    private double _verifyDegreesThreshold = 0.1;

    [ObservableProperty]
    private double _teachingPositionThreshold = 100;

    [ObservableProperty]
    private double _verifyPositionThreshold = 300;

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
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb1 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb2 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb3 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb4 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb5 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb6 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb7 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]

    private byte[] _waferCenterThumb8 = [];

    [ObservableProperty]
    private int _times = 10;
}