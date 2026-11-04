using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Newtonsoft.Json;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMap : ObservableObject, ICloneable<StageMap>
{
    public Point[][] IdealMatrix { get; set; } = [];

    public Vector[][] ErrorMatrix { get; set; } = [];

    public bool[][] IsInWaferMatrix { get; set; } = [];

    public bool[][] IsMatchMatrix { get; set; } = [];

    [JsonIgnore]
    [ObservableProperty]
    public partial int DisplayIndex { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    public void Refresh()
    {
        try
        {
            var vectorFields = PlotDataSource.GetOrAddVectorFields(1);

            var vectorFieldList = new List<(int XIndex, int YIndex, Point Point, Vector Vector, bool? IsMatch)>();

            var (yLength, xLength) = IdealMatrix.GetYXLength();

            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    vectorFieldList.Add((x, y, IdealMatrix[y][x], ErrorMatrix[y][x], IsInWaferMatrix[y][x] ? IsMatchMatrix[y][x] : null));
                }
            }

            vectorFields[0].Update(string.Empty, vectorFieldList);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public void Reset()
    {
        var (yLength, xLength) = ErrorMatrix.GetYXLength();

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                ErrorMatrix[y][x] = Vector.Zero;
                IsMatchMatrix[y][x] = false;
            }
        }

        Refresh();
    }

    public StageMap Clone() => new()
    {
        IdealMatrix =
        [
            .. IdealMatrix.Select<Point[], Point[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        ErrorMatrix =
        [
            .. ErrorMatrix.Select<Vector[], Vector[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        IsInWaferMatrix =
        [
            .. IsInWaferMatrix.Select<bool[], bool[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ],
        IsMatchMatrix =
        [
            .. IsMatchMatrix.Select<bool[], bool[]>(t =>
            [
                .. t.Select(tt => tt)
            ])
        ]
    };
}