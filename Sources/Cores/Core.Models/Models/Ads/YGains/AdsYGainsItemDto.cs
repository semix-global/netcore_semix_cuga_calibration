using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.YGains;

public sealed partial class AdsYGainsItemDto : CalibrationDtoBase, ICloneable<AdsYGainsItemDto>, IAdaptTo<CalibrationAdsYGainsItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private bool _isPositive;

    [ObservableProperty]
    private double _positiveY1P1;

    [ObservableProperty]
    private double _positiveY1P2;

    [ObservableProperty]
    private double _positiveY1P3;

    [ObservableProperty]
    private double _positiveY2P1;

    [ObservableProperty]
    private double _positiveY2P2;

    [ObservableProperty]
    private double _positiveY2P3;

    [ObservableProperty]
    private double _positiveY3P1;

    [ObservableProperty]
    private double _positiveY3P2;

    [ObservableProperty]
    private double _positiveY3P3;

    [ObservableProperty]
    private List<Point> _positiveY1Plots = [];

    [ObservableProperty]
    private List<Point> _positiveY1SmoothPlots = [];

    [ObservableProperty]
    private List<Point> _positiveY2Plots = [];

    [ObservableProperty]
    private List<Point> _positiveY2SmoothPlots = [];

    [ObservableProperty]
    private List<Point> _positiveY3Plots = [];

    [ObservableProperty]
    private List<Point> _positiveY3SmoothPlots = [];

    [ObservableProperty]
    private double _negativeY4P1;

    [ObservableProperty]
    private double _negativeY4P2;

    [ObservableProperty]
    private double _negativeY4P3;

    [ObservableProperty]
    private double _negativeY5P1;

    [ObservableProperty]
    private double _negativeY5P2;

    [ObservableProperty]
    private double _negativeY5P3;

    [ObservableProperty]
    private double _negativeY6P1;

    [ObservableProperty]
    private double _negativeY6P2;

    [ObservableProperty]
    private double _negativeY6P3;

    [ObservableProperty]
    private List<Point> _negativeY4Plots = [];

    [ObservableProperty]
    private List<Point> _negativeY4SmoothPlots = [];

    [ObservableProperty]
    private List<Point> _negativeY5Plots = [];

    [ObservableProperty]
    private List<Point> _negativeY5SmoothPlots = [];

    [ObservableProperty]
    private List<Point> _negativeY6Plots = [];

    [ObservableProperty]
    private List<Point> _negativeY6SmoothPlots = [];


    public double GetY1P1()
    {
        return IsPositive ? PositiveY1P1 : NegativeY4P1;
    }

    public void SetY1P1(double p1)
    {
        if (IsPositive) PositiveY1P1 = p1;
        else NegativeY4P1 = p1;
    }

    public double GetY1P2()
    {
        return IsPositive ? PositiveY1P2 : NegativeY4P2;
    }

    public void SetY1P2(double p2)
    {
        if (IsPositive) PositiveY1P2 = p2;
        else NegativeY4P2 = p2;
    }

    public double GetY1P3()
    {
        return IsPositive ? PositiveY1P3 : NegativeY4P3;
    }

    public void SetY1P3(double p3)
    {
        if (IsPositive) PositiveY1P3 = p3;
        else NegativeY4P3 = p3;
    }

    public double GetY2P1()
    {
        return IsPositive ? PositiveY2P1 : NegativeY5P1;
    }

    public void SetY2P1(double p1)
    {
        if (IsPositive) PositiveY2P1 = p1;
        else NegativeY5P1 = p1;
    }

    public double GetY2P2()
    {
        return IsPositive ? PositiveY2P2 : NegativeY5P2;
    }

    public void SetY2P2(double p2)
    {
        if (IsPositive) PositiveY2P2 = p2;
        else NegativeY5P2 = p2;
    }

    public double GetY2P3()
    {
        return IsPositive ? PositiveY2P3 : NegativeY5P3;
    }

    public void SetY2P3(double p3)
    {
        if (IsPositive) PositiveY2P3 = p3;
        else NegativeY5P3 = p3;
    }

    public double GetY3P1()
    {
        return IsPositive ? PositiveY3P1 : NegativeY6P1;
    }

    public void SetY3P1(double p1)
    {
        if (IsPositive) PositiveY3P1 = p1;
        else NegativeY6P1 = p1;
    }

    public double GetY3P2()
    {
        return IsPositive ? PositiveY3P2 : NegativeY6P2;
    }

    public void SetY3P2(double p2)
    {
        if (IsPositive) PositiveY3P2 = p2;
        else NegativeY6P2 = p2;
    }

    public double GetY3P3()
    {
        return IsPositive ? PositiveY3P3 : NegativeY6P3;
    }

    public void SetY3P3(double p3)
    {
        if (IsPositive) PositiveY3P3 = p3;
        else NegativeY6P3 = p3;
    }

    public List<Point> GetPositiveY1Plots()
    {
        return IsPositive ? PositiveY1Plots : NegativeY4Plots;
    }

    public List<Point> GetPositiveY1SmoothPlots()
    {
        return IsPositive ? PositiveY1SmoothPlots : NegativeY4SmoothPlots;
    }

    public List<Point> GetPositiveY2Plots()
    {
        return IsPositive ? PositiveY2Plots : NegativeY5Plots;
    }

    public List<Point> GetPositiveY2SmoothPlots()
    {
        return IsPositive ? PositiveY2SmoothPlots : NegativeY5SmoothPlots;
    }

    public List<Point> GetPositiveY3Plots()
    {
        return IsPositive ? PositiveY3Plots : NegativeY6Plots;
    }

    public List<Point> GetPositiveY3SmoothPlots()
    {
        return IsPositive ? PositiveY3SmoothPlots : NegativeY6SmoothPlots;
    }

    public void SetPositiveY1Plots(List<Point> plots)
    {
        if (IsPositive) PositiveY1Plots = plots;
        else NegativeY4Plots = plots;
    }

    public void SetPositiveY1SmoothPlots(List<Point> plots)
    {
        if (IsPositive) PositiveY1SmoothPlots = plots;
        else NegativeY4SmoothPlots = plots;
    }

    public void SetPositiveY2Plots(List<Point> plots)
    {
        if (IsPositive) PositiveY2Plots = plots;
        else NegativeY5Plots = plots;
    }

    public void SetPositiveY2SmoothPlots(List<Point> plots)
    {
        if (IsPositive) PositiveY2SmoothPlots = plots;
        else NegativeY5SmoothPlots = plots;
    }

    public void SetPositiveY3Plots(List<Point> plots)
    {
        if (IsPositive) PositiveY3Plots = plots;
        else NegativeY6Plots = plots;
    }

    public void SetPositiveY3SmoothPlots(List<Point> plots)
    {
        if (IsPositive) PositiveY3SmoothPlots = plots;
        else NegativeY6SmoothPlots = plots;
    }

    public void UpdatePositive(AdsYGainsItemDto selectReviewItemDto)
    {
        PositiveY1P1 = selectReviewItemDto.PositiveY1P1;
        PositiveY1P2 = selectReviewItemDto.PositiveY1P2;
        PositiveY1P3 = selectReviewItemDto.PositiveY1P3;
        PositiveY2P1 = selectReviewItemDto.PositiveY2P1;
        PositiveY2P2 = selectReviewItemDto.PositiveY2P2;
        PositiveY2P3 = selectReviewItemDto.PositiveY2P3;
        PositiveY3P1 = selectReviewItemDto.PositiveY3P1;
        PositiveY3P2 = selectReviewItemDto.PositiveY3P2;
        PositiveY3P3 = selectReviewItemDto.PositiveY3P3;
        PositiveY1Plots = selectReviewItemDto.PositiveY1Plots;
        PositiveY1SmoothPlots = selectReviewItemDto.PositiveY1SmoothPlots;
        PositiveY2Plots = selectReviewItemDto.PositiveY2Plots;
        PositiveY2SmoothPlots = selectReviewItemDto.PositiveY2SmoothPlots;
        PositiveY3Plots = selectReviewItemDto.PositiveY3Plots;
        PositiveY3SmoothPlots = selectReviewItemDto.PositiveY3SmoothPlots;
    }

    public void UpdateNegative(AdsYGainsItemDto selectReviewItemDto)
    {
        NegativeY4P1 = selectReviewItemDto.NegativeY4P1;
        NegativeY4P2 = selectReviewItemDto.NegativeY4P2;
        NegativeY4P3 = selectReviewItemDto.NegativeY4P3;
        NegativeY5P1 = selectReviewItemDto.NegativeY5P1;
        NegativeY5P2 = selectReviewItemDto.NegativeY5P2;
        NegativeY5P3 = selectReviewItemDto.NegativeY5P3;
        NegativeY6P1 = selectReviewItemDto.NegativeY6P1;
        NegativeY6P2 = selectReviewItemDto.NegativeY6P2;
        NegativeY6P3 = selectReviewItemDto.NegativeY6P3;
        NegativeY4Plots = selectReviewItemDto.NegativeY4Plots;
        NegativeY4SmoothPlots = selectReviewItemDto.NegativeY4SmoothPlots;
        NegativeY5Plots = selectReviewItemDto.NegativeY5Plots;
        NegativeY5SmoothPlots = selectReviewItemDto.NegativeY5SmoothPlots;
        NegativeY6Plots = selectReviewItemDto.NegativeY6Plots;
        NegativeY6SmoothPlots = selectReviewItemDto.NegativeY6SmoothPlots;
    }

    #region Mapper

    public AdsYGainsItemDto Clone() => new()
    {
        Index = Index,
        IsPositive = IsPositive,
        PositiveY1P1 = PositiveY1P1,
        PositiveY1P2 = PositiveY1P2,
        PositiveY1P3 = PositiveY1P3,
        PositiveY2P1 = PositiveY2P1,
        PositiveY2P2 = PositiveY2P2,
        PositiveY2P3 = PositiveY2P3,
        PositiveY3P1 = PositiveY3P1,
        PositiveY3P2 = PositiveY3P2,
        PositiveY3P3 = PositiveY3P3,
        NegativeY4P1 = NegativeY4P1,
        NegativeY4P2 = NegativeY4P2,
        NegativeY4P3 = NegativeY4P3,
        NegativeY5P1 = NegativeY5P1,
        NegativeY5P2 = NegativeY5P2,
        NegativeY5P3 = NegativeY5P3,
        NegativeY6P1 = NegativeY6P1,
        NegativeY6P2 = NegativeY6P2,
        NegativeY6P3 = NegativeY6P3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAdsYGainsItem AdaptTo() => new()
    {
        PositiveY1P1 = PositiveY1P1,
        PositiveY1P2 = PositiveY1P2,
        PositiveY1P3 = PositiveY1P3,
        PositiveY2P1 = PositiveY2P1,
        PositiveY2P2 = PositiveY2P2,
        PositiveY2P3 = PositiveY2P3,
        PositiveY3P1 = PositiveY3P1,
        PositiveY3P2 = PositiveY3P2,
        PositiveY3P3 = PositiveY3P3,
        NegativeY4P1 = NegativeY4P1,
        NegativeY4P2 = NegativeY4P2,
        NegativeY4P3 = NegativeY4P3,
        NegativeY5P1 = NegativeY5P1,
        NegativeY5P2 = NegativeY5P2,
        NegativeY5P3 = NegativeY5P3,
        NegativeY6P1 = NegativeY6P1,
        NegativeY6P2 = NegativeY6P2,
        NegativeY6P3 = NegativeY6P3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}