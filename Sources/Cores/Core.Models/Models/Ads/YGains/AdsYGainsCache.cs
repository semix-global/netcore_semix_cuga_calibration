using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Ads.YGains;

public sealed partial class AdsYGainsCache : CalibrationCacheBase<AdsYGainsCache>
{
    [ObservableProperty]
    public partial bool IsPositive { get; set; }

    [ObservableProperty]
    public partial List<double> SpeedYValueList { get; set; } = [50, 100, 150, 200];

    [ObservableProperty]
    public partial double DefaultSpeedYValue { get; set; } = 93.566;

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
    public partial int FindMaxY { get; set; } = 100;

    [ObservableProperty]
    public partial int FindMinY { get; set; }

    [ObservableProperty]
    public partial int FindInterval1 { get; set; } = 1;

    [ObservableProperty]
    public partial int FindInterval2 { get; set; } = 1;

    [ObservableProperty]
    public partial int FindInterval3 { get; set; } = 1;

    [ObservableProperty]
    public partial int Y1Number { get; set; } = 5;

    [ObservableProperty]
    public partial int Y2Number { get; set; } = 5;

    [ObservableProperty]
    public partial int Y3Number { get; set; } = 5;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 50;

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; } = 75;

    [ObservableProperty]
    public partial double DefaultSpeedXValue { get; set; } = 93.566;

    [ObservableProperty]
    public partial int WaitTime { get; set; } = 10;

    [ObservableProperty]
    public partial double Y1 { get; set; }

    [ObservableProperty]
    public partial double Y2 { get; set; }

    [ObservableProperty]
    public partial double Y3 { get; set; }

    [ObservableProperty]
    public partial double Y4 { get; set; }

    [ObservableProperty]
    public partial double Y5 { get; set; }

    [ObservableProperty]
    public partial double Y6 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AdsYGainsCacheItem> AdsYGainsPositiveList { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<AdsYGainsCacheItem> AdsYGainsNegativeList { get; set; } = [];

    public override AdsYGainsCache Clone() => new()
    {
        IsPositive = IsPositive,
        SpeedYValueList = [.. SpeedYValueList],
        DefaultSpeedYValue = DefaultSpeedYValue,
        PositiveStartPosition = PositiveStartPosition,
        PositiveEndPosition = PositiveEndPosition,
        NegativeStartPosition = NegativeStartPosition,
        NegativeEndPosition = NegativeEndPosition,
        FindCount = FindCount,
        FindMaxY = FindMaxY,
        FindMinY = FindMinY,
        FindInterval1 = FindInterval1,
        FindInterval2 = FindInterval2,
        FindInterval3 = FindInterval3,
        Y1Number = Y1Number,
        Y2Number = Y2Number,
        Y3Number = Y3Number,
        Threshold = Threshold,
        VerifyThreshold = VerifyThreshold,
        DefaultSpeedXValue = DefaultSpeedXValue,
        WaitTime = WaitTime,
        Y1 = Y1,
        Y2 = Y2,
        Y3 = Y3,
        Y4 = Y4,
        Y5 = Y5,
        Y6 = Y6,
        AdsYGainsPositiveList = new ObservableCollection<AdsYGainsCacheItem>(AdsYGainsPositiveList.Select(x => x.Clone())),
        AdsYGainsNegativeList = new ObservableCollection<AdsYGainsCacheItem>(AdsYGainsNegativeList.Select(x => x.Clone())),
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

    public double GetY1()
    {
        return IsPositive ? Y1 : Y4;
    }

    public double GetY2()
    {
        return IsPositive ? Y2 : Y5;
    }

    public double GetY3()
    {
        return IsPositive ? Y3 : Y6;
    }

    public sealed partial class AdsYGainsCacheItem : CalibrationCacheBase<AdsYGainsCacheItem>
    {
        [ObservableProperty]
        public partial int Index { get; set; }

        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double SpeedYValue { get; set; }

        [ObservableProperty]
        public partial double PositiveY1 { get; set; }

        [ObservableProperty]
        public partial double PositiveY2 { get; set; }

        [ObservableProperty]
        public partial double PositiveY3 { get; set; }

        [ObservableProperty]
        public partial List<double> PositivePlotZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositivePlotZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositiveSmoothZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositiveSmoothZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> PositiveSmoothZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositivePointListZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositivePointListZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositivePointListZ3 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositiveSmoothPointListZ1 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositiveSmoothPointListZ2 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> PositiveSmoothPointListZ3 { get; set; } = [];

        [ObservableProperty]
        public partial double PositiveMaxZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveMinZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveMaxZ2 { get; set; }

        [ObservableProperty]
        public partial double PositiveMinZ2 { get; set; }

        [ObservableProperty]
        public partial double PositiveMaxZ3 { get; set; }

        [ObservableProperty]
        public partial double PositiveMinZ3 { get; set; }

        [ObservableProperty]
        public partial double PositiveZ1 { get; set; }

        [ObservableProperty]
        public partial double PositiveZ2 { get; set; }

        [ObservableProperty]
        public partial double PositiveZ3 { get; set; }

        [ObservableProperty]
        public partial double NegativeY4 { get; set; }

        [ObservableProperty]
        public partial double NegativeY5 { get; set; }

        [ObservableProperty]
        public partial double NegativeY6 { get; set; }

        [ObservableProperty]
        public partial List<double> NegativePlotZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativePlotZ5 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativePlotZ6 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativeSmoothZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativeSmoothZ5 { get; set; } = [];

        [ObservableProperty]
        public partial List<double> NegativeSmoothZ6 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativePointListZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativePointListZ5 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativePointListZ6 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativeSmoothPointListZ4 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativeSmoothPointListZ5 { get; set; } = [];

        [ObservableProperty]
        public partial List<Point> NegativeSmoothPointListZ6 { get; set; } = [];

        [ObservableProperty]
        public partial double NegativeMaxZ4 { get; set; }

        [ObservableProperty]
        public partial double NegativeMinZ4 { get; set; }

        [ObservableProperty]
        public partial double NegativeMaxZ5 { get; set; }

        [ObservableProperty]
        public partial double NegativeMinZ5 { get; set; }

        [ObservableProperty]
        public partial double NegativeMaxZ6 { get; set; }

        [ObservableProperty]
        public partial double NegativeMinZ6 { get; set; }

        [ObservableProperty]
        public partial double NegativeZ4 { get; set; }

        [ObservableProperty]
        public partial double NegativeZ5 { get; set; }

        [ObservableProperty]
        public partial double NegativeZ6 { get; set; }

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

        public override AdsYGainsCacheItem Clone() => new()
        {
            Index = Index,
            IsPositive = IsPositive,
            SpeedYValue = SpeedYValue,
            PositiveY1 = PositiveY1,
            PositiveY2 = PositiveY2,
            PositiveY3 = PositiveY3,
            PositivePlotZ1 = [.. PositivePlotZ1],
            PositivePlotZ2 = [.. PositivePlotZ2],
            PositivePlotZ3 = [.. PositivePlotZ3],
            PositiveSmoothZ1 = [.. PositiveSmoothZ1],
            PositiveSmoothZ2 = [.. PositiveSmoothZ2],
            PositiveSmoothZ3 = [.. PositiveSmoothZ3],
            PositivePointListZ1 = [.. PositivePointListZ1],
            PositivePointListZ2 = [.. PositivePointListZ2],
            PositivePointListZ3 = [.. PositivePointListZ3],
            PositiveSmoothPointListZ1 = [.. PositiveSmoothPointListZ1],
            PositiveSmoothPointListZ2 = [.. PositiveSmoothPointListZ2],
            PositiveSmoothPointListZ3 = [.. PositiveSmoothPointListZ3],
            PositiveMaxZ1 = PositiveMaxZ1,
            PositiveMinZ1 = PositiveMinZ1,
            PositiveMaxZ2 = PositiveMaxZ2,
            PositiveMinZ2 = PositiveMinZ2,
            PositiveMaxZ3 = PositiveMaxZ3,
            PositiveMinZ3 = PositiveMinZ3,
            PositiveZ1 = PositiveZ1,
            PositiveZ2 = PositiveZ2,
            PositiveZ3 = PositiveZ3,
            NegativeY4 = NegativeY4,
            NegativeY5 = NegativeY5,
            NegativeY6 = NegativeY6,
            NegativePlotZ4 = [.. NegativePlotZ4],
            NegativePlotZ5 = [.. NegativePlotZ5],
            NegativePlotZ6 = [.. NegativePlotZ6],
            NegativeSmoothZ4 = [.. NegativeSmoothZ4],
            NegativeSmoothZ5 = [.. NegativeSmoothZ5],
            NegativeSmoothZ6 = [.. NegativeSmoothZ6],
            NegativePointListZ4 = [.. NegativePointListZ4],
            NegativePointListZ5 = [.. NegativePointListZ5],
            NegativePointListZ6 = [.. NegativePointListZ6],
            NegativeSmoothPointListZ4 = [.. NegativeSmoothPointListZ4],
            NegativeSmoothPointListZ5 = [.. NegativeSmoothPointListZ5],
            NegativeSmoothPointListZ6 = [.. NegativeSmoothPointListZ6],
            NegativeMaxZ4 = NegativeMaxZ4,
            NegativeMinZ4 = NegativeMinZ4,
            NegativeMaxZ5 = NegativeMaxZ5,
            NegativeMinZ5 = NegativeMinZ5,
            NegativeMaxZ6 = NegativeMaxZ6,
            NegativeMinZ6 = NegativeMinZ6,
            NegativeZ4 = NegativeZ4,
            NegativeZ5 = NegativeZ5,
            NegativeZ6 = NegativeZ6,
            PositiveH = PositiveH,
            PositiveR = PositiveR,
            PositiveP = PositiveP,
            PositivePlotH = [.. PositivePlotH],
            PositivePlotR = [.. PositivePlotR],
            PositivePlotP = [.. PositivePlotP],
            NegativeH = NegativeH,
            NegativeR = NegativeR,
            NegativeP = NegativeP,
            NegativePlotH = [.. NegativePlotH],
            NegativePlotR = [.. NegativePlotR],
            NegativePlotP = [.. NegativePlotP],
            SumHRP = SumHRP,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
            Id = Id,
            Expiration = Expiration,
        };

        public void SetAdsY1(double y1)
        {
            if (IsPositive) PositiveY1 = y1;
            else NegativeY4 = y1;
        }

        public void SetAdsY2(double y2)
        {
            if (IsPositive) PositiveY2 = y2;
            else NegativeY5 = y2;
        }

        public void SetAdsY3(double y3)
        {
            if (IsPositive) PositiveY3 = y3;
            else NegativeY6 = y3;
        }

        public double GetAdsY1()
        {
            return IsPositive ? PositiveY1 : NegativeY4;
        }

        public double GetAdsY2()
        {
            return IsPositive ? PositiveY2 : NegativeY5;
        }

        public double GetAdsY3()
        {
            return IsPositive ? PositiveY3 : NegativeY6;
        }

        public double GetZ1()
        {
            return IsPositive ? PositiveZ1 : NegativeZ4;
        }

        public double GetZ2()
        {
            return IsPositive ? PositiveZ2 : NegativeZ5;
        }

        public double GetZ3()
        {
            return IsPositive ? PositiveZ3 : NegativeZ6;
        }

        public void SetZ1(double z1)
        {
            if (IsPositive) PositiveZ1 = z1;
            else NegativeZ4 = z1;
        }

        public void SetZ2(double z2)
        {
            if (IsPositive) PositiveZ2 = z2;
            else NegativeZ5 = z2;
        }

        public void SetZ3(double z3)
        {
            if (IsPositive) PositiveZ3 = z3;
            else NegativeZ6 = z3;
        }

        public List<double> GetPlotZ1()
        {
            return IsPositive ? PositivePlotZ1 : NegativePlotZ4;
        }

        public List<double> GetPlotZ2()
        {
            return IsPositive ? PositivePlotZ2 : NegativePlotZ5;
        }

        public List<double> GetPlotZ3()
        {
            return IsPositive ? PositivePlotZ3 : NegativePlotZ6;
        }

        public void SetPlotZ1(List<double> plotZ1)
        {
            if (IsPositive) PositivePlotZ1 = plotZ1;
            else NegativePlotZ4 = plotZ1;
        }

        public void SetPlotZ2(List<double> plotZ2)
        {
            if (IsPositive) PositivePlotZ2 = plotZ2;
            else NegativePlotZ5 = plotZ2;
        }

        public void SetPlotZ3(List<double> plotZ3)
        {
            if (IsPositive) PositivePlotZ3 = plotZ3;
            else NegativePlotZ6 = plotZ3;
        }

        public List<Point> GetPointZ1()
        {
            return IsPositive ? PositivePointListZ1 : NegativePointListZ4;
        }

        public List<Point> GetPointZ2()
        {
            return IsPositive ? PositivePointListZ2 : NegativePointListZ5;
        }

        public List<Point> GetPointZ3()
        {
            return IsPositive ? PositivePointListZ3 : NegativePointListZ6;
        }

        public void SetPointZ1(List<Point> pointZ1)
        {
            if (IsPositive) PositivePointListZ1 = pointZ1;
            else NegativePointListZ4 = pointZ1;
        }

        public void SetPointZ2(List<Point> pointZ2)
        {
            if (IsPositive) PositivePointListZ2 = pointZ2;
            else NegativePointListZ5 = pointZ2;
        }

        public void SetPointZ3(List<Point> pointZ3)
        {
            if (IsPositive) PositivePointListZ3 = pointZ3;
            else NegativePointListZ6 = pointZ3;
        }

        public List<Point> GetSmoothPointZ1()
        {
            return IsPositive ? PositiveSmoothPointListZ1 : NegativeSmoothPointListZ4;
        }

        public List<Point> GetSmoothPointZ2()
        {
            return IsPositive ? PositiveSmoothPointListZ2 : NegativeSmoothPointListZ5;
        }

        public List<Point> GetSmoothPointZ3()
        {
            return IsPositive ? PositiveSmoothPointListZ3 : NegativeSmoothPointListZ6;
        }

        public void SetSmoothPointZ1(List<Point> pointSmoothZ1)
        {
            if (IsPositive) PositiveSmoothPointListZ1 = pointSmoothZ1;
            else NegativeSmoothPointListZ4 = pointSmoothZ1;
        }

        public void SetSmoothPointZ2(List<Point> pointSmoothZ2)
        {
            if (IsPositive) PositiveSmoothPointListZ2 = pointSmoothZ2;
            else NegativeSmoothPointListZ5 = pointSmoothZ2;
        }

        public void SetSmoothPointZ3(List<Point> pointSmoothZ3)
        {
            if (IsPositive) PositiveSmoothPointListZ3 = pointSmoothZ3;
            else NegativeSmoothPointListZ6 = pointSmoothZ3;
        }

        public List<double> GetSmoothPlotZ1()
        {
            return IsPositive ? PositiveSmoothZ1 : NegativeSmoothZ4;
        }

        public List<double> GetSmoothPlotZ2()
        {
            return IsPositive ? PositiveSmoothZ2 : NegativeSmoothZ5;
        }

        public List<double> GetSmoothPlotZ3()
        {
            return IsPositive ? PositiveSmoothZ3 : NegativeSmoothZ6;
        }

        public void SetSmoothPlotZ1(List<double> plotSmoothZ1)
        {
            if (IsPositive) PositiveSmoothZ1 = plotSmoothZ1;
            else NegativeSmoothZ4 = plotSmoothZ1;
        }

        public void SetSmoothPlotZ2(List<double> plotSmoothZ2)
        {
            if (IsPositive) PositiveSmoothZ2 = plotSmoothZ2;
            else NegativeSmoothZ5 = plotSmoothZ2;
        }

        public void SetSmoothPlotZ3(List<double> plotSmoothZ3)
        {
            if (IsPositive) PositiveSmoothZ3 = plotSmoothZ3;
            else NegativeSmoothZ6 = plotSmoothZ3;
        }

        public double GetMaxZ1()
        {
            return IsPositive ? PositiveMaxZ1 : NegativeMaxZ4;
        }

        public double GetMaxZ2()
        {
            return IsPositive ? PositiveMaxZ2 : NegativeMaxZ5;
        }

        public double GetMaxZ3()
        {
            return IsPositive ? PositiveMaxZ3 : NegativeMaxZ6;
        }

        public void SetMaxZ1(double maxZ1)
        {
            if (IsPositive) PositiveMaxZ1 = maxZ1;
            else NegativeMaxZ4 = maxZ1;
        }

        public void SetMaxZ2(double maxZ2)
        {
            if (IsPositive) PositiveMaxZ2 = maxZ2;
            else NegativeMaxZ5 = maxZ2;
        }

        public void SetMaxZ3(double maxZ3)
        {
            if (IsPositive) PositiveMaxZ3 = maxZ3;
            else NegativeMaxZ6 = maxZ3;
        }

        public double GetMinZ1()
        {
            return IsPositive ? PositiveMinZ1 : NegativeMinZ4;
        }

        public double GetMinZ2()
        {
            return IsPositive ? PositiveMinZ2 : NegativeMinZ5;
        }

        public double GetMinZ3()
        {
            return IsPositive ? PositiveMinZ3 : NegativeMinZ6;
        }

        public void SetMinZ1(double minZ1)
        {
            if (IsPositive) PositiveMinZ1 = minZ1;
            else NegativeMinZ4 = minZ1;
        }

        public void SetMinZ2(double minZ2)
        {
            if (IsPositive) PositiveMinZ2 = minZ2;
            else NegativeMinZ5 = minZ2;
        }

        public void SetMinZ3(double minZ3)
        {
            if (IsPositive) PositiveMinZ3 = minZ3;
            else NegativeMinZ6 = minZ3;
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

    public sealed partial class AdsYGainsDichotomySpeedCacheItem : CalibrationCacheBase<AdsYGainsDichotomySpeedCacheItem>
    {
        [ObservableProperty]
        public partial double SpeedYValue { get; set; }

        [ObservableProperty]
        public partial bool IsPositive { get; set; }

        [ObservableProperty]
        public partial double PositiveY1 { get; set; }

        [ObservableProperty]
        public partial double PositiveY2 { get; set; }

        [ObservableProperty]
        public partial double PositiveY3 { get; set; }

        [ObservableProperty]
        public partial double NegativeY4 { get; set; }

        [ObservableProperty]
        public partial double NegativeY5 { get; set; }

        [ObservableProperty]
        public partial double NegativeY6 { get; set; }

        public double GetY1()
        {
            return IsPositive ? PositiveY1 : NegativeY4;
        }

        public double GetY2()
        {
            return IsPositive ? PositiveY2 : NegativeY5;
        }

        public double GetY3()
        {
            return IsPositive ? PositiveY3 : NegativeY6;
        }

        public void SetY1(double y1)
        {
            if (IsPositive) PositiveY1 = y1;
            else NegativeY4 = y1;
        }

        public void SetY2(double y2)
        {
            if (IsPositive) PositiveY2 = y2;
            else NegativeY5 = y2;
        }

        public void SetY3(double y3)
        {
            if (IsPositive) PositiveY3 = y3;
            else NegativeY6 = y3;
        }

        public override AdsYGainsDichotomySpeedCacheItem Clone() => new()
        {
            SpeedYValue = SpeedYValue,
            IsPositive = IsPositive,
            PositiveY1 = PositiveY1,
            PositiveY2 = PositiveY2,
            PositiveY3 = PositiveY3,
            NegativeY4 = NegativeY4,
            NegativeY5 = NegativeY5,
            NegativeY6 = NegativeY6,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
            Id = Id,
            Expiration = Expiration,
        };
    }
}