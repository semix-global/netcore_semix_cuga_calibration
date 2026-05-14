using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using System.ComponentModel;
using Net.Utilities.Models.Serializations;
using Generate = MathNet.Numerics.Generate;

namespace Core.Models.Models.AOD.Uniformity;

[CacheVersion("1.0.0")]
public sealed partial class AODUniformityDTO : CalibrationDTOBase<AODUniformityDTO>, IAdaptTo<CalibrationLaserAODUniformityItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial WindowItem StartWindowItem { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial WindowItem StopWindowItem { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public bool IsReverse => StartWindowItem.HorizontalProjectMinPixel > StopWindowItem.HorizontalProjectMinPixel;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial WindowItem MappingWindowItem { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Mapping> Mappings { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<int[]> ImageHorizontalProjectMappings { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<int[]> PrescanAODWaveformProfileMappings { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ConcurrentDictionary<CIBInformation, double> TargetPMTValues { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AODUniformityDTOItem InitializeWindowItem { get; set; } = new();

    [ObservableProperty]
    public partial AODUniformityDTOItem Item { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<AODUniformityDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<OpticsPolarizationModeEnum, double>))]
    public partial ConcurrentDictionary<OpticsPolarizationModeEnum, double> OpticsPolarizationModeEnumMeasurePowers { get; set; } = [];

    #region Mapper

    public override AODUniformityDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        StartWindowItem = StartWindowItem.Clone(),
        StopWindowItem = StopWindowItem.Clone(),
        MappingWindowItem = MappingWindowItem.Clone(),
        Mappings = [.. Mappings.Select(t => t.Clone())],
        ImageHorizontalProjectMappings = [.. ImageHorizontalProjectMappings.Select<int[], int[]>(t => [.. t])],
        PrescanAODWaveformProfileMappings = [.. PrescanAODWaveformProfileMappings.Select<int[], int[]>(t => [.. t])],
        TargetPMTValues = new ConcurrentDictionary<CIBInformation, double>(TargetPMTValues.Select(t => new KeyValuePair<CIBInformation, double>(t.Key.Clone(), t.Value))),
        InitializeWindowItem = InitializeWindowItem.Clone(),
        Item = Item.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        OpticsPolarizationModeEnumMeasurePowers = new ConcurrentDictionary<OpticsPolarizationModeEnum, double>(OpticsPolarizationModeEnumMeasurePowers),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAODUniformityItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Coefficient = LaserLightInformation.Coefficient,
        OpticsPolarizationModeEnumMeasurePowers = [.. OpticsPolarizationModeEnumMeasurePowers.Select(t => new KeyValuePair<CgPolarizationTypeEnum, double>(t.Key.ToCgPolarizationTypeEnum(), t.Value))],
        OpticsPolarizationModeEnum = Item.OpticsPolarizationModeEnum.ToCgPolarizationTypeEnum(),
        Uniformities = [.. Item.Window],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public partial class WindowItem : ObservableObject, ICloneable<WindowItem>
    {
        [ObservableProperty]
        [Newtonsoft.Json.JsonIgnore]
        public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

        [ObservableProperty]
        public partial IReadOnlyList<double> Window { get; set; } = [];

        [ObservableProperty]
        public partial IReadOnlyList<double> ImageHorizontalProjects { get; set; } = [];

        [ObservableProperty]
        public partial IReadOnlyList<double> SmoothImageHorizontalProjects { get; set; } = [];

        [ObservableProperty]
        public partial int HorizontalProjectMinPixel { get; set; }

        [ObservableProperty]
        public partial IReadOnlyList<int> HorizontalProjectMinPixels { get; set; } = [];

        [ObservableProperty]
        public partial string RawImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ImageFilePath { get; set; } = string.Empty;

        public void CalculateHorizontalProjectMinPixel(int segmentCount)
        {
            SmoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.Dense([.. ImageHorizontalProjects])).ToArray();

            var vShapeWindowBySegments = Generate.LinearVShapeWindowBySegments(
                1d,
                0d,
                segmentCount,
                Generate.LinearRangeInt32(0, segmentCount - 1),
                ImageHorizontalProjects.Count);
            var startIndex = vShapeWindowBySegments.Regions[0].VMiddleIndex;
            var stopIndex = vShapeWindowBySegments.Regions[^1].VMiddleIndex;

            var (indexes, _) = Extremumor.FindMinima(SmoothImageHorizontalProjects.ToPoints());
            HorizontalProjectMinPixel = indexes
                .Where(t => startIndex <= t && t <= stopIndex)
                .OrderBy(t => SmoothImageHorizontalProjects[t])
                .First();
        }

        public void CalculateHorizontalProjectMinPixels(IReadOnlyList<(int VStartIndex, int VMiddleIndex, int VStopIndex)> regions, IReadOnlyList<Point> prescanToImageIndexMappings)
        {
            SmoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.Dense([.. ImageHorizontalProjects])).ToArray();

            var (indexes, _) = Extremumor.FindMinima(SmoothImageHorizontalProjects.ToPoints());

            HorizontalProjectMinPixels = regions
                .Select(t =>
                {
                    var (vStartIndex, vMiddleIndex, vStopIndex) = t;

                    return (VStartIndex: (int)Math.Clamp(Math.Floor(prescanToImageIndexMappings[vStartIndex].Y), 0, ImageHorizontalProjects.Count - 1),
                        VMiddleIndex: (int)Math.Clamp(Math.Round(prescanToImageIndexMappings[vMiddleIndex].Y), 0, ImageHorizontalProjects.Count - 1),
                        VStopIndex: (int)Math.Clamp(Math.Ceiling(prescanToImageIndexMappings[vStopIndex].Y), 0, ImageHorizontalProjects.Count - 1));
                })
                .Select(t =>
                {
                    var (startIndex, _, stopIndex) = t;

                    return indexes
                        .Where(tt => startIndex <= tt && tt <= stopIndex)
                        .OrderBy(tt => SmoothImageHorizontalProjects[tt])
                        .First();
                })
                .ToArray();
        }

        public WindowItem Clone() => new()
        {
            Window = [.. Window],
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            SmoothImageHorizontalProjects = [.. SmoothImageHorizontalProjects],
            HorizontalProjectMinPixel = HorizontalProjectMinPixel,
            HorizontalProjectMinPixels = [.. HorizontalProjectMinPixels],
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }

    public sealed partial class Mapping : ObservableObject, ICloneable<Mapping>
    {
        [ObservableProperty]
        public partial bool IsNotLinearSpline { get; set; }

        [ObservableProperty]
        public partial int ImageHorizontalProjectIndex { get; set; }

        [ObservableProperty]
        public partial double LinearSplineMappingIndex { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public int MappingIndex => (int)Math.Round(LinearSplineMappingIndex, MidpointRounding.AwayFromZero);

        [ObservableProperty]
        public partial IReadOnlyList<int> MappingIndices { get; set; } = [];

        public Mapping Clone() => new()
        {
            IsNotLinearSpline = IsNotLinearSpline,
            ImageHorizontalProjectIndex = ImageHorizontalProjectIndex,
            LinearSplineMappingIndex = LinearSplineMappingIndex,
            MappingIndices = [.. MappingIndices]
        };
    }
}

public sealed partial class AODUniformityDTOItem : ObservableObject, ICloneable<AODUniformityDTOItem>
{
    [ObservableProperty]
    public partial OpticsPolarizationModeEnum OpticsPolarizationModeEnum { get; set; }

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Window { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial double WindowLimitMin { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial double WindowLimitMax { get; set; }

    [ObservableProperty]
    public partial double VerifyMinRate { get; set; }

    [ObservableProperty]
    public partial double VerifyMaxRate { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> VerifyImageHorizontalProjects { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Status> VerifyMappingStatuses { get; set; } = [];

    partial void OnItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(Items));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Items));
    }

    #region Mapper

    public AODUniformityDTOItem Clone() => new()
    {
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        Window = [.. Window],
        WindowLimitMin = WindowLimitMin,
        WindowLimitMax = WindowLimitMax,
        VerifyMinRate = VerifyMinRate,
        VerifyMaxRate = VerifyMaxRate,
        VerifyImageHorizontalProjects = [.. VerifyImageHorizontalProjects],
        VerifyMappingStatuses = [.. VerifyMappingStatuses]
    };

    #endregion Mapper

    public sealed partial class Item : AODUniformityDTO.WindowItem, ICloneable<Item>
    {
        [ObservableProperty]
        public partial double MinRate { get; set; }

        [ObservableProperty]
        public partial double MaxRate { get; set; }

        [ObservableProperty]
        public partial IReadOnlyList<Status> MappingStatuses { get; set; } = [];

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public new Item Clone()
        {
            var clone = Guard.IsAssignableToTypeAndReturn<Item>(base.Clone());
            clone.MinRate = MinRate;
            clone.MaxRate = MaxRate;
            clone.IsOk = IsOk;
            clone.MappingStatuses = [.. MappingStatuses];

            return clone;
        }
    }

    public enum Status
    {
        None,
        GreaterThan,
        LessThan,
        Ok,
        OkWindowLimitMin,
        OkWindowLimitMax
    }
}