using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Ads.XGains;

public sealed partial class AdsXGainsCache : CalibrationCacheBase
{
    [ObservableProperty]
    private List<double> _speedXValueList = [50, 65, 85, 110, 143, 186, 242, 315, 400];

    [ObservableProperty]
    private bool _isPositive;

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
    private int _findMaxX = 100;

    [ObservableProperty]
    private int _findMinX = 0;

    [ObservableProperty]
    private int _findInterval1 = 1;

    [ObservableProperty]
    private int _findInterval2 = 1;

    [ObservableProperty]
    private int _rowNumber = 5;

    [ObservableProperty]
    private int _columnNumber = 5;

    [ObservableProperty]
    private double _threshold = 50;

    [ObservableProperty]
    private double _verifyThreshold = 75;

    [ObservableProperty]
    private double _areaThreshold = 2500;

    [ObservableProperty]
    private double _defaultSpeedXValue = 93.566;

    [ObservableProperty]
    private int _waitTime;

    [ObservableProperty]
    private int _x1;

    [ObservableProperty]
    private int _x2;

    [ObservableProperty]
    private int _x3;

    [ObservableProperty]
    private int _x4;

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsPositiveList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsNegativeList = [];

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

    /// <summary>
    /// 负向集合
    /// </summary>
    public sealed partial class AdsXGainsCacheItem : CalibrationCacheBase
    {
        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private double _speedXValue;

        [ObservableProperty]
        private bool _isPositive;

        [ObservableProperty]
        private double _positiveX1;

        [ObservableProperty]
        private double _positiveX2;

        [ObservableProperty]
        private double _positiveZ1;

        [ObservableProperty]
        private double _positiveZ2;

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
        private List<double> _positivePlotZ1 = [];

        [ObservableProperty]
        private List<double> _positivePlotZ2 = [];

        [ObservableProperty]
        private List<double> _positiveSmoothZ1 = [];

        [ObservableProperty]
        private List<double> _positiveSmoothZ2 = [];

        [ObservableProperty]
        private List<Point> _positivePointListZ1 = [];

        [ObservableProperty]
        private List<Point> _positivePointListZ2 = [];

        [ObservableProperty]
        private List<Point> _positiveSmoothPointListZ1 = [];

        [ObservableProperty]
        private List<Point> _positiveSmoothPointListZ2 = [];

        [ObservableProperty]
        private double _positiveMaxZ1;

        [ObservableProperty]
        private double _positiveMinZ1;

        [ObservableProperty]
        private double _positiveMaxZ2;

        [ObservableProperty]
        private double _positiveMinZ2;

        [ObservableProperty]
        private double _negativeX3;

        [ObservableProperty]
        private double _negativeX4;

        [ObservableProperty]
        private double _negativeZ3;

        [ObservableProperty]
        private double _negativeZ4;

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

        [ObservableProperty]
        private List<double> _negativePlotZ3 = [];

        [ObservableProperty]
        private List<double> _negativePlotZ4 = [];

        [ObservableProperty]
        private List<double> _negativeSmoothZ3 = [];

        [ObservableProperty]
        private List<double> _negativeSmoothZ4 = [];

        [ObservableProperty]
        private List<Point> _negativePointListZ3 = [];

        [ObservableProperty]
        private List<Point> _negativePointListZ4 = [];

        [ObservableProperty]
        private List<Point> _negativeSmoothPointListZ3 = [];

        [ObservableProperty]
        private List<Point> _negativeSmoothPointListZ4 = [];

        [ObservableProperty]
        private double _negativeMaxZ3;

        [ObservableProperty]
        private double _negativeMinZ3;

        [ObservableProperty]
        private double _negativeMaxZ4;

        [ObservableProperty]
        private double _negativeMinZ4;

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

    public sealed partial class AdsXGainsDichotomySpeedCacheItem : CalibrationCacheBase
    {
        [ObservableProperty]
        private double _speedXValue;

        [ObservableProperty]
        private bool _isPositive;

        [ObservableProperty]
        private double _positiveX1;

        [ObservableProperty]
        private double _positiveX2;

        [ObservableProperty]
        private double _negativeX3;

        [ObservableProperty]
        private double _negativeX4;

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
    }
}