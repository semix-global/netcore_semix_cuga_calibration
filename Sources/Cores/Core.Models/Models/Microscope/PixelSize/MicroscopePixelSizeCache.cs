using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    private double _angleThreshold;

    [ObservableProperty]
    private Size _threshold;

    [ObservableProperty]
    private ConcurrentDictionary<string, MicroscopePixelSizeCacheItem> _microscopePixelSizeCacheItemDic = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopePixelSizeCacheItem CurrentCalibrationCacheItem =>
        MicroscopePixelSizeCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopePixelSizeCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    public void SetFindFocusPosition(Point position)
    {
        CurrentCalibrationCacheItem.FindPosition = position;
    }
}