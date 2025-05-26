using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Ads.YGains;

public sealed partial class AdsYGainsCache : CalibrationCacheBase
{
    [ObservableProperty]
    private bool _isPositive;

    [ObservableProperty]
    private List<double> _speedYValueList = [50, 100, 150, 200];

    [ObservableProperty]
    private double _defaultSpeedYValue = 93.566;

    [ObservableProperty]
    private Point _positiveStartPosition;

    [ObservableProperty]
    private Point _positiveEndPosition;

    [ObservableProperty]
    private Point _negativeStartPosition;

    [ObservableProperty]
    private Point _negativeEndPosition;

    [ObservableProperty]
    private int _findCount;

    [ObservableProperty]
    private int _findMaxY = 100;

    [ObservableProperty]
    private int _findMinY = 0;

    [ObservableProperty]
    private int _findInterval1 = 1;

    [ObservableProperty]
    private int _findInterval2 = 1;

    [ObservableProperty]
    private int _findInterval3 = 1;

    [ObservableProperty]
    private int _y1Number = 5;

    [ObservableProperty]
    private int _y2Number = 5;

    [ObservableProperty]
    private int _y3Number = 5;

    [ObservableProperty]
    private double _threshold = 50;

    [ObservableProperty]
    private double _verifyThreshold = 75;

    [ObservableProperty]
    private double _defaultSpeedXValue = 93.566;

    [ObservableProperty]
    private int _waitTime = 10;

    [ObservableProperty]
    private double _y1;

    [ObservableProperty]
    private double _y2;

    [ObservableProperty]
    private double _y3;

    [ObservableProperty]
    private double _y4;

    [ObservableProperty]
    private double _y5;

    [ObservableProperty]
    private double _y6;

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsPositiveList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsNegativeList = [];

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

    public sealed partial class AdsYGainsCacheItem : CalibrationCacheBase
    {
        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private bool _isPositive;

        [ObservableProperty]
        private double _speedYValue;

        [ObservableProperty]
        private double _positiveY1;

        [ObservableProperty]
        private double _positiveY2;

        [ObservableProperty]
        private double _positiveY3;

        [ObservableProperty]
        private List<double> _positivePlotZ1 = [];

        [ObservableProperty]
        private List<double> _positivePlotZ2 = [];

        [ObservableProperty]
        private List<double> _positivePlotZ3 = [];

        [ObservableProperty]
        private List<double> _positiveSmoothZ1 = [];

        [ObservableProperty]
        private List<double> _positiveSmoothZ2 = [];

        [ObservableProperty]
        private List<double> _positiveSmoothZ3 = [];

        [ObservableProperty]
        private List<Point> _positivePointListZ1 = [];

        [ObservableProperty]
        private List<Point> _positivePointListZ2 = [];

        [ObservableProperty]
        private List<Point> _positivePointListZ3 = [];

        [ObservableProperty]
        private List<Point> _positiveSmoothPointListZ1 = [];

        [ObservableProperty]
        private List<Point> _positiveSmoothPointListZ2 = [];

        [ObservableProperty]
        private List<Point> _positiveSmoothPointListZ3 = [];

        [ObservableProperty]
        private double _positiveMaxZ1;

        [ObservableProperty]
        private double _positiveMinZ1;

        [ObservableProperty]
        private double _positiveMaxZ2;

        [ObservableProperty]
        private double _positiveMinZ2;

        [ObservableProperty]
        private double _positiveMaxZ3;

        [ObservableProperty]
        private double _positiveMinZ3;

        [ObservableProperty]
        private double _positiveZ1;

        [ObservableProperty]
        private double _positiveZ2;

        [ObservableProperty]
        private double _positiveZ3;

        [ObservableProperty]
        private double _negativeY4;

        [ObservableProperty]
        private double _negativeY5;

        [ObservableProperty]
        private double _negativeY6;

        [ObservableProperty]
        private List<double> _negativePlotZ4 = [];

        [ObservableProperty]
        private List<double> _negativePlotZ5 = [];

        [ObservableProperty]
        private List<double> _negativePlotZ6 = [];

        [ObservableProperty]
        private List<double> _negativeSmoothZ4 = [];

        [ObservableProperty]
        private List<double> _negativeSmoothZ5 = [];

        [ObservableProperty]
        private List<double> _negativeSmoothZ6 = [];

        [ObservableProperty]
        private List<Point> _negativePointListZ4 = [];

        [ObservableProperty]
        private List<Point> _negativePointListZ5 = [];

        [ObservableProperty]
        private List<Point> _negativePointListZ6 = [];

        [ObservableProperty]
        private List<Point> _negativeSmoothPointListZ4 = [];

        [ObservableProperty]
        private List<Point> _negativeSmoothPointListZ5 = [];

        [ObservableProperty]
        private List<Point> _negativeSmoothPointListZ6 = [];

        [ObservableProperty]
        private double _negativeMaxZ4;

        [ObservableProperty]
        private double _negativeMinZ4;

        [ObservableProperty]
        private double _negativeMaxZ5;

        [ObservableProperty]
        private double _negativeMinZ5;

        [ObservableProperty]
        private double _negativeMaxZ6;

        [ObservableProperty]
        private double _negativeMinZ6;

        [ObservableProperty]
        private double _negativeZ4;

        [ObservableProperty]
        private double _negativeZ5;

        [ObservableProperty]
        private double _negativeZ6;

        [ObservableProperty]
        private double _positiveH;

        [ObservableProperty]
        private double _positiveR;

        [ObservableProperty]
        private double _positiveP;

        [ObservableProperty]
        private List<double> _positivePlotH = [];

        [ObservableProperty]
        private List<double> _positivePlotR = [];

        [ObservableProperty]
        private List<double> _positivePlotP = [];

        [ObservableProperty]
        private double _negativeH;

        [ObservableProperty]
        private double _negativeR;

        [ObservableProperty]
        private double _negativeP;

        [ObservableProperty]
        private List<double> _negativePlotH = [];

        [ObservableProperty]
        private List<double> _negativePlotR = [];

        [ObservableProperty]
        private List<double> _negativePlotP = [];

        [ObservableProperty]
        private double _sumHRP;

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

    public sealed partial class AdsYGainsDichotomySpeedCacheItem : CalibrationCacheBase
    {
        [ObservableProperty]
        private double _speedYValue;

        [ObservableProperty]
        private bool _isPositive;

        [ObservableProperty]
        private double _positiveY1;

        [ObservableProperty]
        private double _positiveY2;

        [ObservableProperty]
        private double _positiveY3;

        [ObservableProperty]
        private double _negativeY4;

        [ObservableProperty]
        private double _negativeY5;

        [ObservableProperty]
        private double _negativeY6;

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
    }
}