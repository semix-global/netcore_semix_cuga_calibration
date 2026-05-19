using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.YGains;

[CacheVersion("1.0.0")]
public sealed partial class AdsYGainsItemDto : CalibrationDTOBase<AdsYGainsItemDto>, IAdaptTo<CalibrationAdsYGainsItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial bool IsPositive { get; set; }

    [ObservableProperty]
    public partial double PositiveY1P1 { get; set; }

    [ObservableProperty]
    public partial double PositiveY1P2 { get; set; }

    [ObservableProperty]
    public partial double PositiveY1P3 { get; set; }

    [ObservableProperty]
    public partial double PositiveY2P1 { get; set; }

    [ObservableProperty]
    public partial double PositiveY2P2 { get; set; }

    [ObservableProperty]
    public partial double PositiveY2P3 { get; set; }

    [ObservableProperty]
    public partial double PositiveY3P1 { get; set; }

    [ObservableProperty]
    public partial double PositiveY3P2 { get; set; }

    [ObservableProperty]
    public partial double PositiveY3P3 { get; set; }

    [ObservableProperty]
    public partial List<Point> PositiveY1Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveY1SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveY2Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveY2SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveY3Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> PositiveY3SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial double NegativeY4P1 { get; set; }

    [ObservableProperty]
    public partial double NegativeY4P2 { get; set; }

    [ObservableProperty]
    public partial double NegativeY4P3 { get; set; }

    [ObservableProperty]
    public partial double NegativeY5P1 { get; set; }

    [ObservableProperty]
    public partial double NegativeY5P2 { get; set; }

    [ObservableProperty]
    public partial double NegativeY5P3 { get; set; }

    [ObservableProperty]
    public partial double NegativeY6P1 { get; set; }

    [ObservableProperty]
    public partial double NegativeY6P2 { get; set; }

    [ObservableProperty]
    public partial double NegativeY6P3 { get; set; }

    [ObservableProperty]
    public partial List<Point> NegativeY4Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeY4SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeY5Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeY5SmoothPlots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeY6Plots { get; set; } = [];

    [ObservableProperty]
    public partial List<Point> NegativeY6SmoothPlots { get; set; } = [];

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

    public override AdsYGainsItemDto Clone() => new()
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
        PositiveY1Plots = [.. PositiveY1Plots],
        PositiveY1SmoothPlots = [.. PositiveY1SmoothPlots],
        PositiveY2Plots = [.. PositiveY2Plots],
        PositiveY2SmoothPlots = [.. PositiveY2SmoothPlots],
        PositiveY3Plots = [.. PositiveY3Plots],
        PositiveY3SmoothPlots = [.. PositiveY3SmoothPlots],
        NegativeY4Plots = [.. NegativeY4Plots],
        NegativeY4SmoothPlots = [.. NegativeY4SmoothPlots],
        NegativeY5Plots = [.. NegativeY5Plots],
        NegativeY5SmoothPlots = [.. NegativeY5SmoothPlots],
        NegativeY6Plots = [.. NegativeY6Plots],
        NegativeY6SmoothPlots = [.. NegativeY6SmoothPlots],
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
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}