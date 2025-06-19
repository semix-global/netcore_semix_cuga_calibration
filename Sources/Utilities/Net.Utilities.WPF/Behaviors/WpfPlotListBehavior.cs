using Microsoft.Xaml.Behaviors;
using MiniExcelLibs;
using Net.Utilities.WPF.Extensions;
using Ookii.Dialogs.Wpf;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Point;
using Range = ScottPlot.Range;

namespace Net.Utilities.WPF.Behaviors;

public sealed class WpfPlotListBehavior : Behavior<WpfPlot>
{
    private readonly Turbo _turbo = new();

    #region 依赖属性

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(WpfPlotListBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public bool IsShowLegend
    {
        get => (bool)GetValue(IsShowLegendProperty);
        set => SetValue(IsShowLegendProperty, value);
    }

    public static readonly DependencyProperty IsShowLegendProperty = DependencyProperty.Register(
        nameof(IsShowLegend),
        typeof(bool),
        typeof(WpfPlotListBehavior),
        new PropertyMetadata(true, PropertyChangedCallback)
    );

    public List<WpfPlotModel>? PlotList
    {
        get => (List<WpfPlotModel>?)GetValue(PlotListProperty);
        set => SetValue(PlotListProperty, value);
    }

    public static readonly DependencyProperty PlotListProperty = DependencyProperty.Register(
        nameof(PlotList),
        typeof(List<WpfPlotModel>),
        typeof(WpfPlotListBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs _)
    {
        if (d is not WpfPlotListBehavior behaviors || behaviors.AssociatedObject is null) return;

        behaviors.UpdatePlotList();
    }

    #endregion 依赖属性

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ConfigureWpfPlotScatter();

        UpdatePlotList();

        if (AssociatedObject.Menu is not WpfPlotMenu wpfPlotMenu) return;
        wpfPlotMenu?.ContextMenuItems.Add(new ContextMenuItem
        {
            Label = "Output Excel",
            OnInvoke = OutPutXlsx
        });
    }

    public void OutPutXlsx(Plot plot)
    {
        var dialog = TryShowSaveFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;
        var outputPath = filePath;
        if (PlotList is null) return;
        var resultList = new List<(string title, List<double> Value)>
        {
            //插入X轴数据
            ("X轴", PlotList[0].Points.Select(t => t.X).ToList())
        };
        foreach (var (_, (title, pointList, _, _)) in PlotList.Select((t, i) => (i, t)))
        {
            var valueList = pointList.Select(t => t.Y).ToList();
            resultList.Add((title, valueList));
        }

        // 获取最大行数
        var maxRows = resultList.Max(r => r.Value.Count);

        // 创建写入Excel的数据结构
        var values = new List<Dictionary<string, object>>();

        // 将每一行的数据作为字典的一个元素
        for (var i = 0; i < maxRows; i++)
        {
            var row = new Dictionary<string, object>();
            foreach (var (title, Value) in resultList)
            {
                if (i < Value.Count)
                {
                    row[title] = Value[i]; // 防止索引越界
                }
            }

            values.Add(row);
        }

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        // 将数据写入 Excel 文件
        MiniExcel.SaveAs(outputPath, values);
    }

    private void UpdatePlotList()
    {
        AssociatedObject.Plot.Title(Title);

        try
        {
            if (PlotList is null) return;

            AssociatedObject.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);

            foreach (var (i, (title, pointList, turbo, MarkerPoint)) in PlotList.Select((t, i) => (i, t)))
            {
                var color = turbo is null ? Colors.Category10[i % Colors.Category10.Length] : _turbo.GetColor(turbo.Value.Value, new Range(turbo.Value.Min, turbo.Value.Max));
                if (MarkerPoint is not null)
                {
                    var mk = AssociatedObject.Plot.Add.Markers((double[])[.. MarkerPoint.Select(t => t.X)], [.. MarkerPoint.Select(t => t.Y)]);
                    mk.MarkerShape = MarkerShape.OpenCircle;
                    mk.Color = color;
                }

                var scatterPoints = AssociatedObject.Plot.Add.Scatter(pointList.Select(t => t.X).ToList(), pointList.Select(t => t.Y).ToList(), color);
                scatterPoints.LegendText = title;
            }

            if (IsShowLegend) AssociatedObject.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
            else AssociatedObject.Plot.HideLegend();

            AssociatedObject.Plot.Axes.AutoScale();
        }
        finally
        {
            AssociatedObject.Refresh();
        }
    }

    // 弹出保存文件对话框
    private static bool? TryShowSaveFilePathDialog(string filter, out string filePath)
    {
        filePath = string.Empty;

        var saveFileDialog = new VistaSaveFileDialog
        {
            Title = "Save a file",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            OverwritePrompt = true,
            AddExtension = true, // 文档名添加扩展名
            DefaultExt = $"{filter}",
            Filter = $"files (*{filter})|*{filter}",
            FileName = Guid.NewGuid().ToString()
        };
        var result = saveFileDialog.ShowDialog();
        if (result == true) filePath = saveFileDialog.FileName;
        return result;
    }
}

public sealed record WpfPlotModel(string Title, Point[] Points, (double Min, double Max, double Value)? Turbo = null, Point[]? MarkerPoints = null);