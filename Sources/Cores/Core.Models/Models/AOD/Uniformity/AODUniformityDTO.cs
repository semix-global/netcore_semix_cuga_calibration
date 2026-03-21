using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using System.ComponentModel;
using Generate = MathNet.Numerics.Generate;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityDTO : CalibrationDtoBase, ICloneable<AODUniformityDTO>, IAdaptTo<CalibrationLaserAODUniformityItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private WindowItem _startWindowItem = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private WindowItem _stopWindowItem = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public bool IsReverse => StartWindowItem.HorizontalProjectMinPixel > StopWindowItem.HorizontalProjectMinPixel;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private WindowItem _mappingWindowItem = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<Mapping> _mappings = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<int[]> _imageHorizontalProjectMappings = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<int[]> _prescanAODWaveformProfileMappings = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private ConcurrentBag<KeyValuePair<CIBInformation, double>> _targetPMTValues = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private AODUniformityDTOItem _initializeWindowItem = new();

    [ObservableProperty]
    private AODUniformityDTOItem _item = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<AODUniformityDTOItem> _items = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<OpticsPolarizationModeEnum, double>> _opticsPolarizationModeEnumMeasurePowers = [];

    #region Mapper

    public AODUniformityDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        StartWindowItem = StartWindowItem.Clone(),
        StopWindowItem = StopWindowItem.Clone(),
        MappingWindowItem = MappingWindowItem.Clone(),
        Mappings = [.. Mappings.Select(t => t.Clone())],
        ImageHorizontalProjectMappings = [.. ImageHorizontalProjectMappings.Select<int[], int[]>(t => [.. t])],
        PrescanAODWaveformProfileMappings = [.. PrescanAODWaveformProfileMappings.Select<int[], int[]>(t => [.. t])],
        TargetPMTValues = [.. TargetPMTValues],
        InitializeWindowItem = InitializeWindowItem.Clone(),
        Item = Item.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        OpticsPolarizationModeEnumMeasurePowers = [.. OpticsPolarizationModeEnumMeasurePowers],
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
        [property: Newtonsoft.Json.JsonIgnore]
        [property: System.Text.Json.Serialization.JsonIgnore]
        [property: System.Xml.Serialization.XmlIgnore]
        private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

        [ObservableProperty]
        private IReadOnlyList<double> _window = [];

        [ObservableProperty]
        private IReadOnlyList<double> _imageHorizontalProjects = [];

        [ObservableProperty]
        private IReadOnlyList<double> _smoothImageHorizontalProjects = [];

        [ObservableProperty]
        private int _horizontalProjectMinPixel;

        [ObservableProperty]
        private IReadOnlyList<int> _horizontalProjectMinPixels = [];

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

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
        private bool _isNotLinearSpline;

        [ObservableProperty]
        private int _imageHorizontalProjectIndex;

        [ObservableProperty]
        private double _linearSplineMappingIndex;

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]

        public int MappingIndex => (int)Math.Round(LinearSplineMappingIndex, MidpointRounding.AwayFromZero);

        [ObservableProperty]
        private IReadOnlyList<int> _mappingIndices = [];

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
    private OpticsPolarizationModeEnum _opticsPolarizationModeEnum;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private IReadOnlyList<double> _window = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private double _windowLimitMin;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private double _windowLimitMax;

    [ObservableProperty]
    private double _verifyMinRate;

    [ObservableProperty]
    private double _verifyMaxRate;

    [ObservableProperty]
    private IReadOnlyList<double> _verifyImageHorizontalProjects = [];

    [ObservableProperty]
    private IReadOnlyList<Status> _verifyMappingStatuses = [];

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
        private double _minRate;

        [ObservableProperty]
        private double _maxRate;

        [ObservableProperty]
        private IReadOnlyList<Status> _mappingStatuses = [];

        [ObservableProperty]
        private bool _isOk;

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