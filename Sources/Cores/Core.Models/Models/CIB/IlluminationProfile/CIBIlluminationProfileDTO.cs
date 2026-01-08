using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Collector;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.IlluminationProfile;

public sealed partial class CIBIlluminationProfileDTO : CalibrationDtoBase, ICloneable<CIBIlluminationProfileDTO>, IAdaptTo<CalibrationLaserCIBIlluminationProfileItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsApodizationModeEnum _opticsApodizationModeEnum;

    [ObservableProperty]
    private OpticsPolarizationModeEnum _opticsPolarizationModeEnum;

    [ObservableProperty]
    private CollectorPolarizationModeEnum _collectorPolarizationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<CIBIlluminationProfileDTOItem> _items = [];

    [ObservableProperty]
    private ConcurrentBag<KeyValuePair<CIBInformation, double>> _targetPMTValues = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private ConcurrentBag<KeyValuePair<CIBInformation, IScatterPlotControl>> _scatterPlotControls = [];

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<CIBIlluminationProfileDTOItem>? oldValue, IReadOnlyList<CIBIlluminationProfileDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    partial void OnTargetPMTValuesChanged(ConcurrentBag<KeyValuePair<CIBInformation, double>> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBIlluminationProfileDTO()
    {
    }

    public CIBIlluminationProfileDTO(IReadOnlyList<CIBInformation> cibInformations) : this()
    {
        ScatterPlotControls = [.. cibInformations.Select(t => new KeyValuePair<CIBInformation, IScatterPlotControl>(t, GetScatterPlotControl()))];
    }

    private void RefreshPlot()
    {
        foreach (var itemItem in Items)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(itemItem.CIBInformation, GetScatterPlotControl());

            scatterPlotControl.Clear(0);
            scatterPlotControl.Clear(1);

            try
            {
                if (TargetPMTValues.TryGetSingle(t => t.Key == itemItem.CIBInformation, out var targetPMTValueKvp) == false) return;
                scatterPlotControl.GetOrAddYLine(0, "Target", targetPMTValueKvp.Value, Colors.Red);

                scatterPlotControl.GetOrAddScatterLine(
                    2,
                    "Result",
                    [.. itemItem.IlluminationProfiles.Index().Select(t => new Point(t.Index, t.Item))],
                    Colors.Red);

                foreach (var (i, itemItemData) in itemItem.Items.Index())
                {
                    scatterPlotControl.GetOrAddScatterLine(
                        0,
                        $"{i + 1} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                        [.. itemItemData.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                        i,
                        new Range(0, itemItem.Items.Count - 1));

                    scatterPlotControl.GetOrAddScatterLine(
                        1,
                        $"{i + 1}",
                        [.. itemItemData.IlluminationProfiles.Index().Select(t => new Point(t.Index, t.Item))],
                        i,
                        new Range(0, itemItem.Items.Count - 1));
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        scatterPlotControl.Configure(new Rows(), 3);

        scatterPlotControl.SetTitle(0, "Horizontal Projects(Y: Log - X: px)");
        scatterPlotControl.SetTitle(1, "Details(Y: Illumination Profile - X: px)");
        scatterPlotControl.SetTitle(2, "Result(Y: Illumination Profile - X: px)");

        return scatterPlotControl;
    }

    #region Mapper

    public CIBIlluminationProfileDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        OpticsApodizationModeEnum = OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        CollectorPolarizationModeEnum = CollectorPolarizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        TargetPMTValues = [.. TargetPMTValues],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBIlluminationProfileItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        OpticsApodizationModeEnum = (int)OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.ToCgPolarizationTypeEnum(),
        CollectorPolarizationModeEnum = CollectorPolarizationModeEnum.ToCgNDFTypeEnum(),
        Items = [.. Items.Select(t => t.AdaptTo())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class CIBIlluminationProfileDTOItem : ObservableObject, ICloneable<CIBIlluminationProfileDTOItem>, IAdaptTo<CalibrationLaserCIBIlluminationProfileItem.Item>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<Item> _items = [];

    [ObservableProperty]
    private IReadOnlyList<double> _illuminationProfiles = [];

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

    public CIBIlluminationProfileDTOItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        IlluminationProfiles = [.. IlluminationProfiles]
    };

    public CalibrationLaserCIBIlluminationProfileItem.Item AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        IlluminationProfiles = [.. IlluminationProfiles]
    };

    #endregion Mapper

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        private IReadOnlyList<double> _imageHorizontalProjects = [];

        [ObservableProperty]
        private double _minRate;

        [ObservableProperty]
        private double _maxRate;

        [ObservableProperty]
        private IReadOnlyList<double> _illuminationProfiles = [];

        [ObservableProperty]
        private bool _isOk;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private string _imageFilePath = string.Empty;

        public Item Clone() => new()
        {
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            MinRate = MinRate,
            MaxRate = MaxRate,
            IlluminationProfiles = [.. IlluminationProfiles],
            IsOk = IsOk,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath
        };
    }
}