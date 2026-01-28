using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
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
    private WindowItem _startWindowItem = new();

    [ObservableProperty]
    private WindowItem _stopWindowItem = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public bool IsReverse => StartWindowItem.HorizontalProjectMinPixel > StopWindowItem.HorizontalProjectMinPixel;

    [ObservableProperty]
    private WindowItem _mappingWindowItem = new();

    [ObservableProperty]
    private IReadOnlyList<Mapping> _mappings = [];

    [ObservableProperty]
    private IReadOnlyList<int[]> _imageHorizontalProjectMappings = [];

    [ObservableProperty]
    private IReadOnlyList<int[]> _prescanAODWaveformProfileMappings = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<CIBInformation, double>> _targetPMTValues = [];

    [ObservableProperty]
    private AODUniformityDTOItem _initializeWindowItem = new();

    [ObservableProperty]
    private AODUniformityDTOItem _item = new();

    [ObservableProperty]
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
        [property: LiteDB.BsonIgnore]
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

        public void CalculateHorizontalProjectMinPixel(int segmentCount, int segmentIndex)
        {
            var (vYPixelStartIndex, _, vYPixelStopIndex) = Generate.LinearVShapeWindowBySegments(
                1d,
                1d,
                segmentCount + 1,
                segmentIndex,
                ImageHorizontalProjects.Count).Region;

            // 正序
            SmoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(ImageHorizontalProjects)).ToArray();
            var (forwardX, forwardY) = Extremumor.FindMinima(
                Vector<double>.Build.DenseOfArray(Enumerable.Range(0, SmoothImageHorizontalProjects.Count).ToArray()),
                Vector<double>.Build.DenseOfEnumerable(SmoothImageHorizontalProjects));

            var forwardHorizontalProjectMinPixel = forwardX
                .Select(t => (int)t)
                .Index()
                .Where(t => vYPixelStartIndex <= t.Item && t.Item <= vYPixelStopIndex)
                .Select(t => (X: t.Item, Y: forwardY[t.Index]))
                .OrderBy(t => t.Y)
                .FirstOrDefault((-1, 0));

            // 倒序
            var reverseSmoothImageHorizontalProjects = SmoothImageHorizontalProjects.Reverse().ToArray();
            var (reverseX, reverseY) = Extremumor.FindMinima(
                Vector<double>.Build.DenseOfArray(Enumerable.Range(0, reverseSmoothImageHorizontalProjects.Length).ToArray()),
                Vector<double>.Build.DenseOfEnumerable(reverseSmoothImageHorizontalProjects));

            var reverseHorizontalProjectMinPixel = reverseX
                .Select(t => (int)t)
                .Index()
                .Where(t => vYPixelStartIndex <= t.Item && t.Item <= vYPixelStopIndex)
                .Select(t => (X: t.Item, Y: reverseY[t.Index]))
                .OrderBy(t => t.Y)
                .FirstOrDefault((-1, 0));

            HorizontalProjectMinPixel = (forwardHorizontalProjectMinPixel.X, reverseHorizontalProjectMinPixel.X) switch
            {
                (not -1, -1) => forwardHorizontalProjectMinPixel.X,
                (-1, not -1) => ImageHorizontalProjects.Count - reverseHorizontalProjectMinPixel.X - 1,
                (not -1, not -1) => forwardHorizontalProjectMinPixel.Y < reverseHorizontalProjectMinPixel.Y
                    ? forwardHorizontalProjectMinPixel.X
                    : ImageHorizontalProjects.Count - reverseHorizontalProjectMinPixel.X - 1,
                (_, _) => ThrowHelper.ThrowInvalidOperationException<int>("Horizontal Project Min Pixel is not found.")
            };
        }

        public void CalculateHorizontalProjectMinPixels(int segmentCount, int[] segmentIndexes)
        {
            var horizontalProjectMinPixels = new int[segmentCount];

            var regions = Generate.LinearVShapeWindowBySegments(
                1d,
                1d,
                segmentCount + 1,
                segmentIndexes,
                ImageHorizontalProjects.Count).Regions;

            SmoothImageHorizontalProjects = [.. SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(ImageHorizontalProjects))];
            var (x, y) = Extremumor.FindMinima(
                Vector<double>.Build.DenseOfArray(Enumerable.Range(0, SmoothImageHorizontalProjects.Count).ToArray()),
                Vector<double>.Build.DenseOfEnumerable(SmoothImageHorizontalProjects));

            foreach (var (index, (vYPixelStartIndex, _, vYPixelStopIndex)) in regions.Index())
            {
                horizontalProjectMinPixels[index] = x
                    .Select(t => (int)t)
                    .Index()
                    .Where(t => vYPixelStartIndex <= t.Item && t.Item <= vYPixelStopIndex)
                    .Select(t => (X: t.Item, Y: y[t.Index]))
                    .OrderBy(t => t.Y)
                    .First()
                    .X;
            }

            HorizontalProjectMinPixels = horizontalProjectMinPixels;
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
        [LiteDB.BsonIgnore]
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
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private IReadOnlyList<double> _window = [];

    [ObservableProperty]
    private double _windowLimitMin;

    [ObservableProperty]
    private double _windowLimitMax;

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
        WindowLimitMax = WindowLimitMax
    };

    #endregion Mapper

    public sealed partial class Item : AODUniformityDTO.WindowItem, ICloneable<Item>
    {
        [ObservableProperty]
        private double _minRate;

        [ObservableProperty]
        private double _maxRate;

        [ObservableProperty]
        private bool _isOk;

        public new Item Clone()
        {
            var clone = GuardUtils.IsAssignableToType<Item>(base.Clone());
            clone.MinRate = MinRate;
            clone.MaxRate = MaxRate;
            clone.IsOk = IsOk;

            return clone;
        }
    }
}