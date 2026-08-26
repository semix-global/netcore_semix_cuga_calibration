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
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    public void Refresh()
    {
        var vectorFields = PlotDataSource.GetOrAddVectorFields(1);

        var vectorFieldList = new List<(Point Point, Vector Vector)>();

        var (rowCount, columnCount) = IdealMatrix.GetRowColCount();

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (IsInWaferMatrix[row][column] == false) continue;

                vectorFieldList.Add((IdealMatrix[row][column], ErrorMatrix[row][column]));
            }
        }

        vectorFields[0].Update(string.Empty, vectorFieldList);
    }

    public void Reset()
    {
        var (rowCount, columnCount) = ErrorMatrix.GetRowColCount();

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                ErrorMatrix[row][column] = Vector.Zero;
                IsMatchMatrix[row][column] = false;
            }
        }
    }

    public StageMap Clone() => new()
    {
        IdealMatrix = JaggedArrayExtensions.Clone(IdealMatrix),
        ErrorMatrix = JaggedArrayExtensions.Clone(ErrorMatrix),
        IsInWaferMatrix = JaggedArrayExtensions.Clone(IsInWaferMatrix),
        IsMatchMatrix = JaggedArrayExtensions.Clone(IsMatchMatrix)
    };
}