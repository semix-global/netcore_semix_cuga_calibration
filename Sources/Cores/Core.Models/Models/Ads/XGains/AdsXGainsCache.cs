using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Ads.XGains;

public sealed partial class AdsXGainsCache : CalibrationCacheBase<AdsXGainsCache>
{
    [ObservableProperty]
    public partial List<double> SpeedXValueList { get; set; } = [50, 65, 85, 110, 143, 186, 242, 315, 400];

    [ObservableProperty]
    public partial bool IsPositive { get; set; }

    [ObservableProperty]
    public partial Point PositiveStartPosition { get; set; }

    [ObservableProperty]
    public partial Point PositiveEndPosition { get; set; }

    [ObservableProperty]
    public partial Point NegativeStartPosition { get; set; }

    [ObservableProperty]
    public partial Point NegativeEndPosition { get; set; }

    [ObservableProperty]
    public partial int FindCount { get; set; }

    [ObservableProperty]
    public partial int FindMaxX { get; set; } = 100;

    [ObservableProperty]
    public partial int FindMinX { get; set; }

    [ObservableProperty]
    public partial int FindInterval1 { get; set; } = 1;

    [ObservableProperty]
    public partial int FindInterval2 { get; set; } = 1;

    [ObservableProperty]
    public partial int RowNumber { get; set; } = 5;

    [ObservableProperty]
    public partial int ColumnNumber { get; set; } = 5;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 50;

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; } = 75;

    [ObservableProperty]
    public partial double AreaThreshold { get; set; } = 2500;

    [ObservableProperty]
    public partial double DefaultSpeedXValue { get; set; } = 93.566;

    [ObservableProperty]
    public partial int WaitTime { get; set; }

    [ObservableProperty]
    public partial int X1 { get; set; }

    [ObservableProperty]
    public partial int X2 { get; set; }

    [ObservableProperty]
    public partial int X3 { get; set; }

    [ObservableProperty]
    public partial int X4 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AdsXGainsCacheItem> AdsXGainsPositiveList { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<AdsXGainsCacheItem> AdsXGainsNegativeList { get; set; } = [];

    public override AdsXGainsCache Clone() => new()
    {
        SpeedXValueList = [.. SpeedXValueList],
        IsPositive = IsPositive,
        PositiveStartPosition = PositiveStartPosition,
        PositiveEndPosition = PositiveEndPosition,
        NegativeStartPosition = NegativeStartPosition,
        NegativeEndPosition = NegativeEndPosition,
        FindCount = FindCount,
        FindMaxX = FindMaxX,
        FindMinX = FindMinX,
        FindInterval1 = FindInterval1,
        FindInterval2 = FindInterval2,
        RowNumber = RowNumber,
        ColumnNumber = ColumnNumber,
        Threshold = Threshold,
        VerifyThreshold = VerifyThreshold,
        AreaThreshold = AreaThreshold,
        DefaultSpeedXValue = DefaultSpeedXValue,
        WaitTime = WaitTime,
        X1 = X1,
        X2 = X2,
        X3 = X3,
        X4 = X4,
        AdsXGainsPositiveList = new ObservableCollection<AdsXGainsCacheItem>(AdsXGainsPositiveList.Select(x => x.Clone())),
        AdsXGainsNegativeList = new ObservableCollection<AdsXGainsCacheItem>(AdsXGainsNegativeList.Select(x => x.Clone())),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };

    public Point GetStartPosition()
    {
        return IsPositive ? PositiveStartPosition : NegativeStartPosition;
    }

    public Point GetEndPosition()
    {
        return IsPositive ? PositiveEndPosition : NegativeEndPosition;
    }

    public void SetStartPosition(Point position)
    {
        if (IsPositive) PositiveStartPosition = position;
        else NegativeStartPosition = position;
    }

    public void SetEndPosition(Point position)
    {
        if (IsPositive) PositiveEndPosition = position;
        else NegativeEndPosition = position;
    }

    public ObservableCollection<AdsXGainsCacheItem> GetAdsXGainsCacheItem()
    {
        return IsPositive ? AdsXGainsPositiveList : AdsXGainsNegativeList;
    }

    public void SetAdsXGainsCacheItem(ObservableCollection<AdsXGainsCacheItem> adsXGainsCacheItemList)
    {
        if (IsPositive) AdsXGainsPositiveList = adsXGainsCacheItemList;
        else AdsXGainsNegativeList = adsXGainsCacheItemList;
    }

    public double GetX1()
    {
        return IsPositive ? X1 : X3;
    }

    public double GetX2()
    {
        return IsPositive ? X2 : X4;
    }

    /// <summary>
    /// 负向集合
    /// </summary>
    public sealed partial class AdsXGainsCacheItem : CalibrationCacheBase<AdsXGainsCacheItem>
    {
        [ObservableProperty]
        public partial int Index { get; set; }

        [ObservableProperty]
        public partial double SpeedXValue { get; set; }

        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double PositiveX1 { get; set; }

        [ObservableProperty]
        public partial double PositiveX2 { get; set; }

        [ObservableProperty]
        public partial double PositiveZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveZ2 { get; set; }

        [ObservableProperty]
        public partial double PositiveH { get; set; }

        [ObservableProperty]
        public partial double PositiveR { get; set; }

        [ObservableProperty]
        public partial double PositiveP { get; set; }

        [ObservableProperty]
        public partial List<double> PositivePlotH { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotR { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotP { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositiveSmoothZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositiveSmoothZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositivePointListZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositivePointListZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositiveSmoothPointListZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositiveSmoothPointListZ2 { get; set; } = [];

        [ObservableProperty]
        public partial double PositiveMaxZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveMinZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveMaxZ2 { get; set; }

        [ObservableProperty]
        public partial double PositiveMinZ2 { get; set; }

        [ObservableProperty]
        public partial double NegativeX3 { get; set; }

        [ObservableProperty]
        public partial double NegativeX4 { get; set; }

        [ObservableProperty]
        public partial double NegativeZ3 { get; set; }

        [ObservableProperty]
        public partial double NegativeZ4 { get; set; }

        [ObservableProperty]
        public partial double NegativeH { get; set; }

        [ObservableProperty]
        public partial double NegativeR { get; set; }

        [ObservableProperty]
        public partial double NegativeP { get; set; }

        [ObservableProperty]
        public partial List<double> NegativePlotH { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativePlotR { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativePlotP { get; set; } = [];

        [ObservableProperty]
        public partial double SumHRP { get; set; }

        [ObservableProperty]
        public partial List<double> NegativePlotZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativePlotZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativeSmoothZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativeSmoothZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativePointListZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativePointListZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativeSmoothPointListZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativeSmoothPointListZ4 { get; set; } = [];

        [ObservableProperty]
        public partial double NegativeMaxZ3 { get; set; }

        [ObservableProperty]
        public partial double NegativeMinZ3 { get; set; }

        [ObservableProperty]
        public partial double NegativeMaxZ4 { get; set; }

        [ObservableProperty]
        public partial double NegativeMinZ4 { get; set; }

        public override AdsXGainsCacheItem Clone() => new()
        {
            Index = Index,
            SpeedXValue = SpeedXValue,
            IsPositive = IsPositive,
            PositiveX1 = PositiveX1,
            PositiveX2 = PositiveX2,
            PositiveZ1 = PositiveZ1,
            PositiveZ2 = PositiveZ2,
            PositiveH = PositiveH,
            PositiveR = PositiveR,
            PositiveP = PositiveP,
            PositivePlotH = [.. PositivePlotH],
            PositivePlotR = [.. PositivePlotR],
            PositivePlotP = [.. PositivePlotP],
            PositivePlotZ1 = [.. PositivePlotZ1],
            PositivePlotZ2 = [.. PositivePlotZ2],
            PositiveSmoothZ1 = [.. PositiveSmoothZ1],
            PositiveSmoothZ2 = [.. PositiveSmoothZ2],
            PositivePointListZ1 = [.. PositivePointListZ1],
            PositivePointListZ2 = [.. PositivePointListZ2],
            PositiveSmoothPointListZ1 = [.. PositiveSmoothPointListZ1],
            PositiveSmoothPointListZ2 = [.. PositiveSmoothPointListZ2],
            PositiveMaxZ1 = PositiveMaxZ1,
            PositiveMinZ1 = PositiveMinZ1,
            PositiveMaxZ2 = PositiveMaxZ2,
            PositiveMinZ2 = PositiveMinZ2,
            NegativeX3 = NegativeX3,
            NegativeX4 = NegativeX4,
            NegativeZ3 = NegativeZ3,
            NegativeZ4 = NegativeZ4,
            NegativeH = NegativeH,
            NegativeR = NegativeR,
            NegativeP = NegativeP,
            NegativePlotH = [.. NegativePlotH],
            NegativePlotR = [.. NegativePlotR],
            NegativePlotP = [.. NegativePlotP],
            SumHRP = SumHRP,
            NegativePlotZ3 = [.. NegativePlotZ3],
            NegativePlotZ4 = [.. NegativePlotZ4],
            NegativeSmoothZ3 = [.. NegativeSmoothZ3],
            NegativeSmoothZ4 = [.. NegativeSmoothZ4],
            NegativePointListZ3 = [.. NegativePointListZ3],
            NegativePointListZ4 = [.. NegativePointListZ4],
            NegativeSmoothPointListZ3 = [.. NegativeSmoothPointListZ3],
            NegativeSmoothPointListZ4 = [.. NegativeSmoothPointListZ4],
            NegativeMaxZ3 = NegativeMaxZ3,
            NegativeMinZ3 = NegativeMinZ3,
            NegativeMaxZ4 = NegativeMaxZ4,
            NegativeMinZ4 = NegativeMinZ4,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
            Id = Id,
            Expiration = Expiration,
        };

        public double GetX1()
        {
            return IsPositive ? PositiveX1 : NegativeX3;
        }

        public double GetX2()
        {
            return IsPositive ? PositiveX2 : NegativeX4;
        }

        public void SetX1(double x1)
        {
            if (IsPositive) PositiveX1 = x1;
            else NegativeX3 = x1;
        }

        public void SetX2(double x2)
        {
            if (IsPositive) PositiveX2 = x2;
            else NegativeX4 = x2;
        }

        public double GetZ1()
        {
            return IsPositive ? PositiveZ1 : NegativeZ3;
        }

        public double GetZ2()
        {
            return IsPositive ? PositiveZ2 : NegativeZ4;
        }

        public void SetZ1(double z1)
        {
            if (IsPositive) PositiveZ1 = z1;
            else NegativeZ3 = z1;
        }

        public void SetZ2(double z2)
        {
            if (IsPositive) PositiveZ2 = z2;
            else NegativeZ4 = z2;
        }

        public List<double> GetPlotZ1()
        {
            return IsPositive ? PositivePlotZ1 : NegativePlotZ3;
        }

        public List<double> GetPlotZ2()
        {
            return IsPositive ? PositivePlotZ2 : NegativePlotZ4;
        }

        public void SetPlotZ1(List<double> plotZ1)
        {
            if (IsPositive) PositivePlotZ1 = plotZ1;
            else NegativePlotZ3 = plotZ1;
        }

        public void SetPlotZ2(List<double> plotZ2)
        {
            if (IsPositive) PositivePlotZ2 = plotZ2;
            else NegativePlotZ4 = plotZ2;
        }

        public List<Point> GetPointZ1()
        {
            return IsPositive ? PositivePointListZ1 : NegativePointListZ3;
        }

        public List<Point> GetPointZ2()
        {
            return IsPositive ? PositivePointListZ2 : NegativePointListZ4;
        }

        public void SetPointZ1(List<Point> pointZ1)
        {
            if (IsPositive) PositivePointListZ1 = pointZ1;
            else NegativePointListZ3 = pointZ1;
        }

        public void SetPointZ2(List<Point> pointZ2)
        {
            if (IsPositive) PositivePointListZ2 = pointZ2;
            else NegativePointListZ4 = pointZ2;
        }

        public List<Point> GetSmoothPointZ1()
        {
            return IsPositive ? PositiveSmoothPointListZ1 : NegativeSmoothPointListZ3;
        }

        public List<Point> GetSmoothPointZ2()
        {
            return IsPositive ? PositiveSmoothPointListZ2 : NegativeSmoothPointListZ4;
        }

        public void SetSmoothPointZ1(List<Point> pointSmoothZ1)
        {
            if (IsPositive) PositiveSmoothPointListZ1 = pointSmoothZ1;
            else NegativeSmoothPointListZ3 = pointSmoothZ1;
        }

        public void SetSmoothPointZ2(List<Point> pointSmoothZ2)
        {
            if (IsPositive) PositiveSmoothPointListZ2 = pointSmoothZ2;
            else NegativeSmoothPointListZ4 = pointSmoothZ2;
        }

        public List<double> GetSmoothPlotZ1()
        {
            return IsPositive ? PositiveSmoothZ1 : NegativeSmoothZ3;
        }

        public List<double> GetSmoothPlotZ2()
        {
            return IsPositive ? PositiveSmoothZ2 : NegativeSmoothZ4;
        }

        public void SetSmoothPlotZ1(List<double> plotSmoothZ1)
        {
            if (IsPositive) PositiveSmoothZ1 = plotSmoothZ1;
            else NegativeSmoothZ3 = plotSmoothZ1;
        }

        public void SetSmoothPlotZ2(List<double> plotSmoothZ2)
        {
            if (IsPositive) PositiveSmoothZ2 = plotSmoothZ2;
            else NegativeSmoothZ4 = plotSmoothZ2;
        }

        public double GetMaxZ1()
        {
            return IsPositive ? PositiveMaxZ1 : NegativeMaxZ3;
        }

        public double GetMaxZ2()
        {
            return IsPositive ? PositiveMaxZ2 : NegativeMaxZ4;
        }

        public void SetMaxZ1(double maxZ1)
        {
            if (IsPositive) PositiveMaxZ1 = maxZ1;
            else NegativeMaxZ3 = maxZ1;
        }

        public void SetMaxZ2(double maxZ2)
        {
            if (IsPositive) PositiveMaxZ2 = maxZ2;
            else NegativeMaxZ4 = maxZ2;
        }

        public double GetMinZ1()
        {
            return IsPositive ? PositiveMinZ1 : NegativeMinZ3;
        }

        public double GetMinZ2()
        {
            return IsPositive ? PositiveMinZ2 : NegativeMinZ4;
        }

        public void SetMinZ1(double minZ1)
        {
            if (IsPositive) PositiveMinZ1 = minZ1;
            else NegativeMinZ3 = minZ1;
        }

        public void SetMinZ2(double minZ2)
        {
            if (IsPositive) PositiveMinZ2 = minZ2;
            else NegativeMinZ4 = minZ2;
        }

        public double GetH()
        {
            return IsPositive ? PositiveH : NegativeH;
        }

        public double GetR()
        {
            return IsPositive ? PositiveR : NegativeR;
        }

        public double GetP()
        {
            return IsPositive ? PositiveP : NegativeP;
        }

        public void SetH(double h)
        {
            if (IsPositive) PositiveH = h;
            else NegativeH = h;
        }

        public void SetR(double r)
        {
            if (IsPositive) PositiveR = r;
            else NegativeR = r;
        }

        public void SetP(double p)
        {
            if (IsPositive) PositiveP = p;
            else NegativeP = p;
        }

        public List<double> GetPlotH()
        {
            return IsPositive ? PositivePlotH : NegativePlotH;
        }

        public List<double> GetPlotR()
        {
            return IsPositive ? PositivePlotR : NegativePlotR;
        }

        public List<double> GetPlotP()
        {
            return IsPositive ? PositivePlotP : NegativePlotP;
        }

        public void SetPlotH(List<double> hList)
        {
            if (IsPositive) PositivePlotH = hList;
            else NegativePlotH = hList;
        }

        public void SetPlotR(List<double> rList)
        {
            if (IsPositive) PositivePlotR = rList;
            else NegativePlotR = rList;
        }

        public void SetPlotP(List<double> pList)
        {
            if (IsPositive) PositivePlotP = pList;
            else NegativePlotP = pList;
        }
    }

    public sealed partial class AdsXGainsDichotomySpeedCacheItem : CalibrationCacheBase<AdsXGainsDichotomySpeedCacheItem>
    {
        [ObservableProperty]
        public partial double SpeedXValue { get; set; }

        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double PositiveX1 { get; set; }

        [ObservableProperty]
        public partial double PositiveX2 { get; set; }

        [ObservableProperty]
        public partial double NegativeX3 { get; set; }

        [ObservableProperty]
        public partial double NegativeX4 { get; set; }

        public double GetX1()
        {
            return IsPositive ? PositiveX1 : NegativeX3;
        }

        public double GetX2()
        {
            return IsPositive ? PositiveX2 : NegativeX4;
        }

        public void SetX1(double x1)
        {
            if (IsPositive) PositiveX1 = x1;
            else NegativeX3 = x1;
        }

        public void SetX2(double x2)
        {
            if (IsPositive) PositiveX2 = x2;
            else NegativeX4 = x2;
        }

        public override AdsXGainsDichotomySpeedCacheItem Clone() => new()
        {
            SpeedXValue = SpeedXValue,
            IsPositive = IsPositive,
            PositiveX1 = PositiveX1,
            PositiveX2 = PositiveX2,
            NegativeX3 = NegativeX3,
            NegativeX4 = NegativeX4,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
            Id = Id,
            Expiration = Expiration,
        };
    }
}