using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.AutoFocus;

public sealed partial class ChuckAutoFocusCache : CalibrationCacheBase
{
    private int _rowNumber = 3;
    private int _columnNumber = 3;
    private double _chuckDiameter = 300000;
    private double _waferReduceWidth = 20000;
    private double _waferReduceHeight = 20000;
    private double _waitTime = 5;
    private double _threshold = 5;
    private double _columnCellWidth;
    private double _rowCellHeight;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Number: ")]
    [OddEvenNumber(NumberParityTypeEnum.Odd, ErrorMessage = "Row Number: ")]
    public int RowNumber
    {
        get => _rowNumber;
        set
        {
            if (SetProperty(ref _rowNumber, value, true))
            {
                UpdateRowCellHeight();

                OnPropertyChanged(nameof(RowCellHeight));
                OnPropertyChanged(nameof(ColumnCellWidth));
            }
        }
    }

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Number: ")]
    [OddEvenNumber(NumberParityTypeEnum.Odd, ErrorMessage = "Column Number: ")]
    public int ColumnNumber
    {
        get => _columnNumber;
        set
        {
            if (SetProperty(ref _columnNumber, value, true))
            {
                UpdateColumnCellWidth();

                OnPropertyChanged(nameof(RowCellHeight));
                OnPropertyChanged(nameof(ColumnCellWidth));
            }
        }
    }

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Diameter: ")]
    public double ChuckDiameter
    {
        get => _chuckDiameter;
        set
        {
            if (SetProperty(ref _chuckDiameter, value, true))
            {
                UpdateColumnCellWidth();
                UpdateRowCellHeight();
                OnPropertyChanged(nameof(RowCellHeight));
                OnPropertyChanged(nameof(ColumnCellWidth));
            }
        }
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Reduce Width: ")]
    public double WaferReduceWidth
    {
        get => _waferReduceWidth;
        set
        {
            if (SetProperty(ref _waferReduceWidth, value, true))
            {
                UpdateColumnCellWidth();
                OnPropertyChanged(nameof(ColumnCellWidth));
            }
        }
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Reduce Height: ")]
    public double WaferReduceHeight
    {
        get => _waferReduceHeight;
        set
        {
            if (SetProperty(ref _waferReduceHeight, value, true))
            {
                UpdateRowCellHeight();
                OnPropertyChanged(nameof(RowCellHeight));
            }
        }
    }

    [Comparison(0.001d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wait Time: ")]
    public double WaitTime
    {
        get => _waitTime;
        set => SetProperty(ref _waitTime, value, true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThan)]
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Cell Width must be greater than 0.1.")]
    public double ColumnCellWidth
    {
        get => _columnCellWidth;
        private set => SetProperty(ref _columnCellWidth, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Cell Height must be greater than 0.1.")]
    public double RowCellHeight
    {
        get => _rowCellHeight;
        private set => SetProperty(ref _rowCellHeight, value, true);
    }

    private void UpdateColumnCellWidth()
    {
        ColumnCellWidth = (new Circle(Point.Origin, ChuckDiameter / 2d).GetInscribedRect().Width - WaferReduceWidth) / (ColumnNumber - 1);
    }

    private void UpdateRowCellHeight()
    {
        RowCellHeight = (new Circle(Point.Origin, ChuckDiameter / 2d).GetInscribedRect().Width - WaferReduceHeight) / (RowNumber - 1);
    }
}