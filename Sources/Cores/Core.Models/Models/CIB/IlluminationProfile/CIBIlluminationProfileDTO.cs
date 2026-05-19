using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.IlluminationProfile;

[CacheVersion("1.0.0")]
public sealed partial class CIBIlluminationProfileDTO : CalibrationDTOBase<CIBIlluminationProfileDTO>, IAdaptTo<CalibrationLaserCIBIlluminationProfileItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial OpticsApodizationModeEnum OpticsApodizationModeEnum { get; set; }

    [ObservableProperty]
    public partial OpticsPolarizationModeEnum OpticsPolarizationModeEnum { get; set; }

    [ObservableProperty]
    public partial OpticsCollectorPolarizationModeEnum OpticsCollectorPolarizationModeEnum { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CIBIlluminationProfileDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<CIBInformation, double>))]
    public partial ConcurrentDictionary<CIBInformation, double> TargetPMTValues { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ConcurrentDictionary<CIBInformation, IScatterPlotControl> ScatterPlotControls { get; set; } = [];

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

    partial void OnTargetPMTValuesChanged(ConcurrentDictionary<CIBInformation, double> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBIlluminationProfileDTO()
    {
    }

    public CIBIlluminationProfileDTO(IReadOnlyList<CIBInformation> cibInformations) : this()
    {
        ScatterPlotControls = new ConcurrentDictionary<CIBInformation, IScatterPlotControl>(cibInformations.Select(t => new KeyValuePair<CIBInformation, IScatterPlotControl>(t, GetScatterPlotControl())));
    }

    private void RefreshPlot()
    {
        foreach (var itemItem in Items)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(itemItem.CIBInformation, _ => GetScatterPlotControl());

            scatterPlotControl.Clear(0);
            scatterPlotControl.Clear(1);

            try
            {
                if (TargetPMTValues.TryGetSingle(t => t.Key == itemItem.CIBInformation, out var targetPMTValueKvp) == false) return;
                scatterPlotControl.GetOrAddYLine(0, "Target", targetPMTValueKvp.Value, Colors.Red);

                scatterPlotControl.GetOrAddScatterLine(
                    2,
                    "Result",
                    [.. itemItem.Window.Index().Select(t => new Point(t.Index, t.Item))],
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
                        [.. itemItemData.Window.Index().Select(t => new Point(t.Index, t.Item))],
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

        scatterPlotControl.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        scatterPlotControl.SetTitle(1, "Window(Y: Coefficient - X: sa)");
        scatterPlotControl.SetTitle(2, "Result Window(Y: Coefficient - X: sa)");

        return scatterPlotControl;
    }

    #region Mapper

    public override CIBIlluminationProfileDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        OpticsApodizationModeEnum = OpticsApodizationModeEnum,
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        TargetPMTValues = new ConcurrentDictionary<CIBInformation, double>(TargetPMTValues.Select(t => new KeyValuePair<CIBInformation, double>(t.Key.Clone(), t.Value))),
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
        CollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.ToCgNDFTypeEnum(),
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
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Window { get; set; } = [];

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
        Window = [.. Window]
    };

    public CalibrationLaserCIBIlluminationProfileItem.Item AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        IlluminationProfiles = [.. Window]
    };

    #endregion Mapper

    public sealed partial class Item : AODUniformityDTO.WindowItem, ICloneable<Item>
    {
        [ObservableProperty]
        public partial double MinRate { get; set; }

        [ObservableProperty]
        public partial double MaxRate { get; set; }

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public new Item Clone()
        {
            var clone = Guard.IsAssignableToTypeAndReturn<Item>(base.Clone());
            clone.MinRate = MinRate;
            clone.MaxRate = MaxRate;
            clone.IsOk = IsOk;

            return clone;
        }
    }
}