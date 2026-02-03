using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipDto : CalibrationDtoBase, ICloneable<MicroscopeCalChipDto>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private double _dSWAlignmentDegree;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    public ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDtoItem>> Items { get; private init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem CurrentItem => Items.GetOrAdd(CalChipSiteModelEnum, new MicroscopeCalChipDtoItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem ChuckItem => Items.Get(CalChipSiteModelEnum.ChuckModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem DswItem => Items.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem HazeItem => Items.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem ShinyWaferItem => Items.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem UndefineWaferItem => Items.Get(CalChipSiteModelEnum.UndefinedModel);

    public double DswToChuckAfEcsValue
    {
        get
        {
            if (Items.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) &&
                Items.TryGet(CalChipSiteModelEnum.DswModel, out var dswItem))
            {
                return dswItem.AfEcsValue - chuckItem.AfEcsValue;
            }

            return 0d;
        }
    }

    public double DswToChuckAfMotorValue
    {
        get
        {
            if (Items.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) &&
                Items.TryGet(CalChipSiteModelEnum.DswModel, out var dswItem))
            {
                return dswItem.AfMotorValue - chuckItem.AfMotorValue;
            }

            return 0d;
        }
    }

    public double HazeToChuckAfEcsValue
    {
        get
        {
            if (Items.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) &&
                Items.TryGet(CalChipSiteModelEnum.HazeModel, out var hazeItem))
            {
                return hazeItem.AfEcsValue - chuckItem.AfEcsValue;
            }

            return 0d;
        }
    }

    public double HazeToChuckAfMotorValue
    {
        get
        {
            if (Items.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) &&
                Items.TryGet(CalChipSiteModelEnum.HazeModel, out var hazeItem))
            {
                return hazeItem.AfMotorValue - chuckItem.AfEcsValue;
            }

            return 0d;
        }
    }

    [ObservableProperty]
    private Point _dSWBrightFieldMachineAffinePosition;

    [ObservableProperty]
    private Point _dSWDarkFieldMachineAffinePosition;

    #region Mapper

    public MicroscopeCalChipDto Clone()
    {
        var cloneItems = new ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDtoItem>>();
        foreach (var item in Items)
        {
            cloneItems.GetOrAdd(item.Key, item.Value.Clone());
        }

        return new()
        {
            CalChipSiteModelEnum = CalChipSiteModelEnum,
            MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
            DSWAlignmentDegree = DSWAlignmentDegree,
            DSWBrightFieldMachineAffinePosition = DSWBrightFieldMachineAffinePosition,
            DSWDarkFieldMachineAffinePosition = DSWDarkFieldMachineAffinePosition,
            Items = cloneItems,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationMicroscopeCalChip AdaptTo()
    {
        var chuckItemTemp = Items.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) ? chuckItem : null;
        var dswItemTemp = Items.TryGet(CalChipSiteModelEnum.DswModel, out var dswItem) ? dswItem : null;
        var hazeItemTemp = Items.TryGet(CalChipSiteModelEnum.HazeModel, out var hazeItem) ? hazeItem : null;
        var shinyItemTemp = Items.TryGet(CalChipSiteModelEnum.ShinyWaferModel, out var shinyWaferItem) ? shinyWaferItem : null;
        var undefineItemTemp = Items.TryGet(CalChipSiteModelEnum.UndefinedModel, out var undefinedItem) ? undefinedItem : null;

        return new CalibrationMicroscopeCalChip
        {
            CgMicroscopeLens = MicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(MicroscopeLensInformation),
            DSWAlignmentDegree = DSWAlignmentDegree,
            ChuckAfEcsValue = chuckItemTemp?.AfEcsValue ?? 0d,
            ChuckAfMotorValue = chuckItemTemp?.AfMotorValue ?? 0d,
            DswBrightFieldMachinePosition = dswItemTemp is null ? Point.Origin.ToCgPoint() : DSWBrightFieldMachineAffinePosition.ToCgPoint(),
            DswDarkFieldMachinePosition = dswItemTemp is null ? Point.Origin.ToCgPoint() : DSWDarkFieldMachineAffinePosition.ToCgPoint(),
            DswEcsValue = dswItemTemp?.EcsValue ?? 0d,
            DswAfEcsValue = dswItemTemp?.AfEcsValue ?? 0d,
            DswAfMotorValue = dswItemTemp?.AfMotorValue ?? 0d,
            UndefinedBrightFieldMachinePosition = undefineItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            UndefinedDarkFieldMachinePosition = undefineItemTemp?.DarkFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            UndefinedEcsValue = undefineItemTemp?.EcsValue ?? 0,
            HazeBrightFieldMachinePosition = hazeItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            HazeDarkFieldMachinePosition = hazeItemTemp?.DarkFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            HazeEcsValue = hazeItemTemp?.EcsValue ?? 0d,
            HazeAfEcsValue = hazeItemTemp?.AfEcsValue ?? 0d,
            HazeAfMotorValue = hazeItemTemp?.AfMotorValue ?? 0d,
            ShinyWaferBrightFieldMachinePosition = shinyItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            ShinyWaferDarkFieldMachinePosition = shinyItemTemp?.DarkFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            ShinyWaferEcsValue = shinyItemTemp?.EcsValue ?? 0d,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}

public sealed partial class MicroscopeCalChipDtoItem : ObservableObject, ICloneable<MicroscopeCalChipDtoItem>
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    private Point _brightFieldMachinePosition;

    [ObservableProperty]
    private Point _darkFieldMachinePosition;

    [ObservableProperty]
    private double _ecsValue;

    [ObservableProperty]
    private double _brightFieldQuality;

    [ObservableProperty]
    private double _darkFieldQuality;

    [ObservableProperty]
    private string _brightFieldFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldFilePath = string.Empty;

    [ObservableProperty]
    private double _afEcsValue;

    [ObservableProperty]
    private double _afMotorValue;

    public MicroscopeCalChipDtoItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        BrightFieldMachinePosition = BrightFieldMachinePosition,
        DarkFieldMachinePosition = DarkFieldMachinePosition,
        EcsValue = EcsValue,
        BrightFieldQuality = BrightFieldQuality,
        DarkFieldQuality = DarkFieldQuality,
        BrightFieldFilePath = BrightFieldFilePath,
        DarkFieldFilePath = DarkFieldFilePath,
        AfEcsValue = AfEcsValue,
        AfMotorValue = AfMotorValue
    };
}