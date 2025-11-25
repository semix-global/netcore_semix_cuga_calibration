using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AOD.AODAlignment;

public sealed partial class AODAlignmentDto : CalibrationDtoBase, ICloneable<AODAlignmentDto>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pMTId;

    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemPoints))]
    private IReadOnlyList<AODAlignmentItemDto> _items = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public Point[] ItemPoints => [.. Items.Select(t => new Point(t.PrescanFrequency, t.ImageProjectionYsMaxPixel))];

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private Point[] _itemFitPoints = [];

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    public AODAlignmentDto Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        PMTId = PMTId,
        ChannelId = ChannelId,
        Items = [.. Items.Select(x => x.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        ItemFitPoints = [.. ItemFitPoints],
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AODAlignmentItemDto : ObservableCacheBase, ICloneable<AODAlignmentItemDto>
{
    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<double> _imageProjectionYs = [];

    [ObservableProperty]
    private int _imageProjectionYsMaxPixel;

    public AODAlignmentItemDto Clone() => new()
    {
        PrescanFrequency = PrescanFrequency,
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
        ImageFilePath = ImageFilePath,
        ImageProjectionYs = [.. ImageProjectionYs],
        ImageProjectionYsMaxPixel = ImageProjectionYsMaxPixel
    };
}