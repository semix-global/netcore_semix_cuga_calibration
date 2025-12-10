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
    public MicroscopeCalChipDtoItem? ChuckItem => Items.Get(CalChipSiteModelEnum.ChuckModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem? DswItem => Items.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem? HazeItem => Items.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem? ShinyWaferItem => Items.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipDtoItem? UndefineWaferItem => Items.Get(CalChipSiteModelEnum.UndefinedModel);

    private double _dswToChuckAfEcsValue;

    public double DswToChuckAfEcsValue
    {
        get
        {
            var chuckItem = Items.Get(CalChipSiteModelEnum.ChuckModel);
            var dswItem = Items.Get(CalChipSiteModelEnum.DswModel);
            if (chuckItem == null || dswItem == null)
                return 0d;
            return dswItem.AfEcsValue - chuckItem.AfEcsValue;
        }
        set => _dswToChuckAfEcsValue = value;
    }

    private double _dswToChuckAfMotorValue;

    public double DswToChuckAfMotorValue
    {
        get
        {
            var chuckItem = Items.Get(CalChipSiteModelEnum.ChuckModel);
            var dswItem = Items.Get(CalChipSiteModelEnum.DswModel);
            if (chuckItem == null || dswItem == null)
                return 0d;
            return dswItem.AfMotorValue - chuckItem.AfMotorValue;
        }
        set => _dswToChuckAfMotorValue = value;
    }

    private double _hazeToChuckAfEcsValue;

    public double HazeToChuckAfEcsValue
    {
        get
        {
            var chuckItem = Items.Get(CalChipSiteModelEnum.ChuckModel);
            var hazeItem = Items.Get(CalChipSiteModelEnum.HazeModel);
            if (chuckItem == null || hazeItem == null)
                return 0d;
            return hazeItem.AfEcsValue - chuckItem.AfEcsValue;
        }
        set => _hazeToChuckAfEcsValue = value;
    }

    private double _hazeToChuckAfMotorValue;

    public double HazeToChuckAfMotorValue
    {
        get
        {
            var chuckItem = Items.Get(CalChipSiteModelEnum.ChuckModel);
            var hazeItem = Items.Get(CalChipSiteModelEnum.HazeModel);
            if (chuckItem == null || hazeItem == null)
                return 0d;
            return hazeItem.AfMotorValue - chuckItem.AfMotorValue;
        }
        set => _hazeToChuckAfMotorValue = value;
    }

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
        var chuck = ChuckItem;
        var dsw = DswItem;
        var haze = HazeItem;
        var shiny = ShinyWaferItem;
        var undefine = UndefineWaferItem;
        return new CalibrationMicroscopeCalChip
        {
            CgMicroscopeLens = MicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(MicroscopeLensInformation),
            ChuckAfEcsValue = chuck is null ? 0d : chuck.AfEcsValue,
            ChuckAfMotorValue = chuck is null ? 0d : chuck.AfMotorValue,
            DswBrightFieldMachinePosition = dsw is null ? Point.Origin.ToCgPoint() : dsw.BrightFieldMachinePosition.ToCgPoint(),
            DswDarkFieldMachinePosition = dsw is null ? Point.Origin.ToCgPoint() : dsw.DarkFieldMachinePosition.ToCgPoint(),
            DswEcsValue = dsw is null ? 0d : dsw.EcsValue,
            DswAfEcsValue = dsw is null ? 0d : dsw.AfEcsValue,
            DswAfMotorValue = dsw is null ? 0d : dsw.AfMotorValue,
            UndefinedBrightFieldMachinePosition = undefine is null ? Point.Origin.ToCgPoint() : undefine.BrightFieldMachinePosition.ToCgPoint(),
            UndefinedDarkFieldMachinePosition = undefine is null ? Point.Origin.ToCgPoint() : undefine.DarkFieldMachinePosition.ToCgPoint(),
            UndefinedEcsValue = undefine is null ? 0 : undefine.EcsValue,
            HazeBrightFieldMachinePosition = haze is null ? Point.Origin.ToCgPoint() : haze.BrightFieldMachinePosition.ToCgPoint(),
            HazeDarkFieldMachinePosition = haze is null ? Point.Origin.ToCgPoint() : haze.DarkFieldMachinePosition.ToCgPoint(),
            HazeEcsValue = haze is null ? 0d : haze.EcsValue,
            HazeAfEcsValue = haze is null ? 0d : haze.AfEcsValue,
            HazeAfMotorValue = haze is null ? 0d : haze.AfMotorValue,
            ShinyWaferBrightFieldMachinePosition = shiny is null ? Point.Origin.ToCgPoint() : shiny.BrightFieldMachinePosition.ToCgPoint(),
            ShinyWaferDarkFieldMachinePosition = shiny is null ? Point.Origin.ToCgPoint() : shiny.DarkFieldMachinePosition.ToCgPoint(),
            ShinyWaferEcsValue = shiny is null ? 0d : shiny.EcsValue,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}

public sealed partial class MicroscopeCalChipDtoItem : ObservableCacheBase, ICloneable<MicroscopeCalChipDtoItem>
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