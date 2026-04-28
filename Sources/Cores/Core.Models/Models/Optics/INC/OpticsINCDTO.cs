using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using System.ComponentModel;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;

namespace Core.Models.Models.Optics.INC;

[CacheVersion("1.0.0")]
public sealed partial class OpticsINCDTO : CalibrationDtoBase, ICloneable<OpticsINCDTO>, IAdaptTo<CalibrationOpticsINC>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<OpticsINCDTOItem> _items = [];

    [ObservableProperty]
    private OpticsINCDTOItem? _maxItem;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<OpticsINCDTOItem>? oldValue, IReadOnlyList<OpticsINCDTOItem> newValue)
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

    partial void OnMaxItemChanged(OpticsINCDTOItem? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsINCDTO()
    {
        ScatterPlotControl.SetTitle("INC(Y: PMT Value - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear();

            ScatterPlotControl.GetOrAddScatterLine(
                0,
                $"INC {(MaxItem is not null ? $"{MaxItem.INCMotorAbsoluteValue:0.###}" : "-")}(mm)",
                [.. Items.Select(t => new Point(t.INCMotorAbsoluteValue, t.PMTValue))],
                Constants.Category10.GetColor(0));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public OpticsINCDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        MaxItem = MaxItem?.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsINC AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        INCMotorAbsoluteValue = MaxItem?.INCMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsINCDTOItem : ObservableObject, ICloneable<OpticsINCDTOItem>
{
    [ObservableProperty]
    private double _iNCMotorAbsoluteValue;

    [ObservableProperty]
    private double _pMTValue;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    public OpticsINCDTOItem Clone() => new()
    {
        INCMotorAbsoluteValue = INCMotorAbsoluteValue,
        PMTValue = PMTValue,
        RawImageFilePath = RawImageFilePath,
        ImageFilePath = ImageFilePath
    };
}