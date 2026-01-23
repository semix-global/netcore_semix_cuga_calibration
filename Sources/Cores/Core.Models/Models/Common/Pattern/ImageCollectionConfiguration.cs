using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Pattern;

public partial class ImageCollectionConfiguration : ObservableCacheBase, ICloneable<ImageCollectionConfiguration>
{
    [ObservableProperty]
    private bool _isAutoFocus = true;

    [ObservableProperty]
    private bool _isForward = true;

    #region X Axis

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZEcsPerXWidthUm))]
    private Point _startPoint = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZEcsPerXWidthUm))]
    private Point _endPoint = Point.Origin;

    [ObservableProperty]
    private double _xSpeedValue;

    public double XUniformTime => Math.Abs(StartPoint.X - EndPoint.X) / XSpeedValue;

    private double _accelerateExtensionTime;

    public double AccelerateExtensionTime
    {
        get => IsCustomEcs ? 0 : _accelerateExtensionTime;
        set => _accelerateExtensionTime = value;
    }

    private double _uniformExtensionTime;

    public double UniformExtensionTime
    {
        get => IsCustomEcs ? 0 : _uniformExtensionTime;
        set => _uniformExtensionTime = value;
    }

    public double AccelerateExtensionWidth => 0.5 * AccelerateExtensionTime * AccelerateExtensionTime * XSpeedValue;

    public double UniformExtensionWidth => UniformExtensionTime * XSpeedValue;

    public Point ExtensionStartPoint => StartPoint - (Vector)new Point((AccelerateExtensionWidth + UniformExtensionWidth), 0);

    public Point ExtensionEndPoint => EndPoint + (Vector)new Point((AccelerateExtensionWidth + UniformExtensionWidth), 0);

    #endregion

    #region Z Axis

    private bool _isCustomEcs;

    public bool IsCustomEcs
    {
        get => !IsAutoFocus && _isCustomEcs;
        set => _isCustomEcs = value;
    }

    private double _zStartEcs;

    public double ZStartEcs
    {
        get => IsCustomEcs ? 0 : _zStartEcs;
        set
        {
            _zStartEcs = value;
            OnPropertyChanged(nameof(ZEcsPerXWidthUm));
        }
    }

    private double _zEndEcs;

    public double ZEndEcs
    {
        get => IsCustomEcs ? 0 : _zEndEcs;
        set
        {
            _zEndEcs = value;
            OnPropertyChanged(nameof(ZEcsPerXWidthUm));
        }
    }

    public double ZSpeedValue => (Math.Abs(ZEndEcs - ZStartEcs) / XUniformTime) * 1.097912;

    public double AccelerateExtensionEcs => 0.5 * AccelerateExtensionTime * AccelerateExtensionTime * ZSpeedValue;

    public double UniformExtensionEcs => UniformExtensionTime * ZSpeedValue;

    public double ExtensionStartEcs => ZStartEcs - AccelerateExtensionEcs - UniformExtensionEcs;

    public double ExtensionEndEcs => ZEndEcs + AccelerateExtensionEcs + UniformExtensionEcs;

    public double ZEcsPerXWidthUm => Math.Abs((ZEndEcs - ZStartEcs) / (EndPoint - StartPoint).X);

    #endregion

    public object ToHtmlAnonymous() => new
    {
        IsAutoFocus,
        IsForward,
        StartPoint,
        EndPoint,
        XSpeedValue,
        XUniformTime,
        AccelerateExtensionTime,
        UniformExtensionTime,
        AccelerateExtensionWidth,
        UniformExtensionWidth,
        ExtensionStartPoint,
        ExtensionEndPoint,
        IsCustomEcs,
        ZStartEcs,
        ZEndEcs,
        ZSpeedValue,
        AccelerateExtensionEcs,
        UniformExtensionEcs,
        ExtensionStartEcs,
        ExtensionEndEcs,
        ZEcsPerXWidthUm
    };

    public ImageCollectionConfiguration Clone() => new()
    {
        IsAutoFocus = IsAutoFocus,
        IsForward = IsForward,
        StartPoint = StartPoint,
        EndPoint = EndPoint,
        XSpeedValue = XSpeedValue,
        AccelerateExtensionTime = AccelerateExtensionTime,
        UniformExtensionTime = UniformExtensionTime,
        IsCustomEcs = IsCustomEcs,
        ZStartEcs = ZStartEcs,
        ZEndEcs = ZEndEcs,
    };
}