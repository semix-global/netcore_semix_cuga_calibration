using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.XGains;

[CacheVersion("1.0.0")]
public sealed partial class AdsXGainsItemDto : CalibrationDTOBase<AdsXGainsItemDto>, IAdaptTo<CalibrationAdsXGainsItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial bool IsPositive { get; set; }

    [ObservableProperty]
    public partial double PositiveX1P1 { get; set; }

    [ObservableProperty]
    public partial double PositiveX1P2 { get; set; }

    [ObservableProperty]
    public partial double PositiveX1P3 { get; set; }

    [ObservableProperty]
    public partial double PositiveX2P1 { get; set; }

    [ObservableProperty]
    public partial double PositiveX2P2 { get; set; }

    [ObservableProperty]
    public partial double PositiveX2P3 { get; set; }

    [ObservableProperty]
    public partial List<Point> PositiveX1Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveX1SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveX2Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveX2SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial double NegativeX3P1 { get; set; }

    [ObservableProperty]
    public partial double NegativeX3P2 { get; set; }

    [ObservableProperty]
    public partial double NegativeX3P3 { get; set; }

    [ObservableProperty]
    public partial double NegativeX4P1 { get; set; }

    [ObservableProperty]
    public partial double NegativeX4P2 { get; set; }

    [ObservableProperty]
    public partial double NegativeX4P3 { get; set; }

    [ObservableProperty]
    public partial List<Point> NegativeX3Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeX3SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeX4Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeX4SmoothPlots { get; set; } = [];

    public double GetX1P1()
    {
        return IsPositive ? PositiveX1P1 : NegativeX3P1;
    }

    public void SetX1P1(double p1)
    {
        if (IsPositive) PositiveX1P1 = p1;
        else NegativeX3P1 = p1;
    }

    public double GetX1P2()
    {
        return IsPositive ? PositiveX1P2 : NegativeX3P2;
    }

    public void SetX1P2(double p2)
    {
        if (IsPositive) PositiveX1P2 = p2;
        else NegativeX3P2 = p2;
    }

    public double GetX1P3()
    {
        return IsPositive ? PositiveX1P3 : NegativeX3P3;
    }

    public void SetX1P3(double p3)
    {
        if (IsPositive) PositiveX1P3 = p3;
        else NegativeX3P3 = p3;
    }

    public double GetX2P1()
    {
        return IsPositive ? PositiveX2P1 : NegativeX4P1;
    }

    public void SetX2P1(double p1)
    {
        if (IsPositive) PositiveX2P1 = p1;
        else NegativeX4P1 = p1;
    }

    public double GetX2P2()
    {
        return IsPositive ? PositiveX2P2 : NegativeX4P2;
    }

    public void SetX2P2(double p2)
    {
        if (IsPositive) PositiveX2P2 = p2;
        else NegativeX4P2 = p2;
    }

    public double GetX2P3()
    {
        return IsPositive ? PositiveX2P3 : NegativeX4P3;
    }

    public void SetX2P3(double p3)
    {
        if (IsPositive) PositiveX2P3 = p3;
        else NegativeX4P3 = p3;
    }

    public List<Point> GetX1Plots()
    {
        return IsPositive ? PositiveX1Plots : NegativeX3Plots;
    }

    public void SetX1Plots(List<Point> x1Plots)
    {
        if (IsPositive) PositiveX1Plots = x1Plots;
        else NegativeX3Plots = x1Plots;
    }

    public List<Point> GetX1SmoothPlots()
    {
        return IsPositive ? PositiveX1SmoothPlots : NegativeX3SmoothPlots;
    }

    public void SetX1SmoothPlots(List<Point> x1SmoothPlots)
    {
        if (IsPositive) PositiveX1SmoothPlots = x1SmoothPlots;
        else NegativeX3SmoothPlots = x1SmoothPlots;
    }

    public List<Point> GetX2Plots()
    {
        return IsPositive ? PositiveX2Plots : NegativeX4Plots;
    }

    public void SetX2Plots(List<Point> x2Plots)
    {
        if (IsPositive) PositiveX2Plots = x2Plots;
        else NegativeX4Plots = x2Plots;
    }

    public List<Point> GetX2SmoothPlots()
    {
        return IsPositive ? PositiveX2SmoothPlots : NegativeX4SmoothPlots;
    }

    public void SetX2SmoothPlots(List<Point> x2SmoothPlots)
    {
        if (IsPositive) PositiveX2SmoothPlots = x2SmoothPlots;
        else NegativeX4SmoothPlots = x2SmoothPlots;
    }

    public void UpdatePositive(AdsXGainsItemDto selectReviewItemDto)
    {
        PositiveX1P1 = selectReviewItemDto.PositiveX1P1;
        PositiveX1P2 = selectReviewItemDto.PositiveX1P2;
        PositiveX1P3 = selectReviewItemDto.PositiveX1P3;
        PositiveX2P1 = selectReviewItemDto.PositiveX2P1;
        PositiveX2P2 = selectReviewItemDto.PositiveX2P2;
        PositiveX2P3 = selectReviewItemDto.PositiveX2P3;
        PositiveX1Plots = selectReviewItemDto.PositiveX1Plots;
        PositiveX1SmoothPlots = selectReviewItemDto.PositiveX1SmoothPlots;
        PositiveX2Plots = selectReviewItemDto.PositiveX2Plots;
        PositiveX2SmoothPlots = selectReviewItemDto.PositiveX2SmoothPlots;
    }

    public void UpdateNegative(AdsXGainsItemDto selectReviewItemDto)
    {
        NegativeX3P1 = selectReviewItemDto.NegativeX3P1;
        NegativeX3P2 = selectReviewItemDto.NegativeX3P2;
        NegativeX3P3 = selectReviewItemDto.NegativeX3P3;
        NegativeX4P1 = selectReviewItemDto.NegativeX4P1;
        NegativeX4P2 = selectReviewItemDto.NegativeX4P2;
        NegativeX4P3 = selectReviewItemDto.NegativeX4P3;
        NegativeX3Plots = selectReviewItemDto.NegativeX3Plots;
        NegativeX3SmoothPlots = selectReviewItemDto.NegativeX3SmoothPlots;
        NegativeX4Plots = selectReviewItemDto.NegativeX4Plots;
        NegativeX4SmoothPlots = selectReviewItemDto.NegativeX4SmoothPlots;
    }

    #region Mapper

    public override AdsXGainsItemDto Clone() => new()
    {
        Index = Index,
        IsPositive = IsPositive,
        PositiveX1P1 = PositiveX1P1,
        PositiveX1P2 = PositiveX1P2,
        PositiveX1P3 = PositiveX1P3,
        PositiveX2P1 = PositiveX2P1,
        PositiveX2P2 = PositiveX2P2,
        PositiveX2P3 = PositiveX2P3,
        PositiveX1Plots = PositiveX1Plots,
        PositiveX1SmoothPlots = PositiveX1SmoothPlots,
        PositiveX2Plots = PositiveX2Plots,
        PositiveX2SmoothPlots = PositiveX2SmoothPlots,
        NegativeX3P1 = NegativeX3P1,
        NegativeX3P2 = NegativeX3P2,
        NegativeX3P3 = NegativeX3P3,
        NegativeX4P1 = NegativeX4P1,
        NegativeX4P2 = NegativeX4P2,
        NegativeX4P3 = NegativeX4P3,
        NegativeX3Plots = NegativeX3Plots,
        NegativeX3SmoothPlots = NegativeX3SmoothPlots,
        NegativeX4Plots = NegativeX4Plots,
        NegativeX4SmoothPlots = NegativeX4SmoothPlots,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAdsXGainsItem AdaptTo() => new()
    {
        PositiveX1P1 = PositiveX1P1,
        PositiveX1P2 = PositiveX1P2,
        PositiveX1P3 = PositiveX1P3,
        PositiveX2P1 = PositiveX2P1,
        PositiveX2P2 = PositiveX2P2,
        PositiveX2P3 = PositiveX2P3,
        NegativeX3P1 = NegativeX3P1,
        NegativeX3P2 = NegativeX3P2,
        NegativeX3P3 = NegativeX3P3,
        NegativeX4P1 = NegativeX4P1,
        NegativeX4P2 = NegativeX4P2,
        NegativeX4P3 = NegativeX4P3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}