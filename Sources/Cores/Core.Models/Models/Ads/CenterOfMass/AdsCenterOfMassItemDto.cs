using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Ads.CenterOfMass;

public sealed partial class AdsCenterOfMassItemDto : CalibrationDtoBase, ICloneable<AdsCenterOfMassItemDto>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private bool _isFindX;

    [ObservableProperty]
    private bool _isPositive;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

    [ObservableProperty]
    private double _xErrorMin;

    [ObservableProperty]
    private double _xErrorMax;

    [ObservableProperty]
    private double _yErrorMin;

    [ObservableProperty]
    private double _yErrorMax;

    /// <summary>
    /// ADS寄存器XY-X0和XY-X1的差值
    /// </summary>
    [ObservableProperty]
    private double _deltaX;

    /// <summary>
    ///  ADS寄存器XY-Y0和XY-Y1的差值
    /// </summary>
    [ObservableProperty]
    private double _deltaY;

    partial void OnXErrorMinChanged(double value)
    {
        UpdateDeltaX();
    }

    partial void OnXErrorMaxChanged(double value)
    {
        UpdateDeltaX();
    }

    private void UpdateDeltaX()
    {
        DeltaX = XErrorMax - XErrorMin;
    }

    partial void OnYErrorMinChanged(double value)
    {
        UpdateDeltaY();
    }

    partial void OnYErrorMaxChanged(double value)
    {
        UpdateDeltaY();
    }

    private void UpdateDeltaY()
    {
        DeltaY = YErrorMax - YErrorMin;
    }

    public void GetIsPositive()
    {
        if (IsFindX)
            IsPositive = StartPosition.Y < EndPosition.Y;
        else
            IsPositive = StartPosition.X < EndPosition.X;
    }

    #region Mapper

    public AdsCenterOfMassItemDto Clone() => new()
    {
        Index = Index,
        IsFindX = IsFindX,
        IsPositive = IsPositive,
        StartPosition = StartPosition,
        EndPosition = EndPosition,
        XErrorMin = XErrorMin,
        XErrorMax = XErrorMax,
        YErrorMin = YErrorMin,
        YErrorMax = YErrorMax,
        DeltaX = DeltaX,
        DeltaY = DeltaY,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}