using Microsoft.Xaml.Behaviors;
using MiniExcelLibs;
using Net.Utilities.WPF.Extensions;
using Ookii.Dialogs.Wpf;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Point;

namespace Net.Utilities.WPF.Behaviors;

public sealed class WpfPlotMultiBehavior : Behavior<WpfPlot>
{
    #region 依赖属性

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    #region Plot0

    public string Plot0Title
    {
        get => (string)GetValue(Plot0TitleProperty);
        set => SetValue(Plot0TitleProperty, value);
    }

    public static readonly DependencyProperty Plot0TitleProperty = DependencyProperty.Register(
        nameof(Plot0Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot0Color
    {
        get => (Color)GetValue(Plot0ColorProperty);
        set => SetValue(Plot0ColorProperty, value);
    }

    public static readonly DependencyProperty Plot0ColorProperty = DependencyProperty.Register(
        nameof(Plot0Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[0], PropertyChangedCallback));

    public IEnumerable<Point>? Plot0
    {
        get => (IEnumerable<Point>?)GetValue(Plot0Property);
        set => SetValue(Plot0Property, value);
    }

    public static readonly DependencyProperty Plot0Property = DependencyProperty.Register(
        nameof(Plot0),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape0
    {
        get => (List<Point>?)GetValue(MarkShape0Property);
        set => SetValue(MarkShape0Property, value);
    }

    public static readonly DependencyProperty MarkShape0Property = DependencyProperty.Register(
        nameof(MarkShape0),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot0

    #region Plot1

    public string Plot1Title
    {
        get => (string)GetValue(Plot1TitleProperty);
        set => SetValue(Plot1TitleProperty, value);
    }

    public static readonly DependencyProperty Plot1TitleProperty = DependencyProperty.Register(
        nameof(Plot1Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot1Color
    {
        get => (Color)GetValue(Plot1ColorProperty);
        set => SetValue(Plot1ColorProperty, value);
    }

    public static readonly DependencyProperty Plot1ColorProperty = DependencyProperty.Register(
        nameof(Plot1Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[1], PropertyChangedCallback));

    public IEnumerable<Point>? Plot1
    {
        get => (IEnumerable<Point>?)GetValue(Plot1Property);
        set => SetValue(Plot1Property, value);
    }

    public static readonly DependencyProperty Plot1Property = DependencyProperty.Register(
        nameof(Plot1),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape1
    {
        get => (List<Point>?)GetValue(MarkShape1Property);
        set => SetValue(MarkShape1Property, value);
    }

    public static readonly DependencyProperty MarkShape1Property = DependencyProperty.Register(
        nameof(MarkShape1),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot1

    #region Plot2

    public string Plot2Title
    {
        get => (string)GetValue(Plot2TitleProperty);
        set => SetValue(Plot2TitleProperty, value);
    }

    public static readonly DependencyProperty Plot2TitleProperty = DependencyProperty.Register(
        nameof(Plot2Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot2Color
    {
        get => (Color)GetValue(Plot2ColorProperty);
        set => SetValue(Plot2ColorProperty, value);
    }

    public static readonly DependencyProperty Plot2ColorProperty = DependencyProperty.Register(
        nameof(Plot2Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[2], PropertyChangedCallback));

    public IEnumerable<Point>? Plot2
    {
        get => (IEnumerable<Point>?)GetValue(Plot2Property);
        set => SetValue(Plot2Property, value);
    }

    public static readonly DependencyProperty Plot2Property = DependencyProperty.Register(
        nameof(Plot2),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape2
    {
        get => (List<Point>?)GetValue(MarkShape2Property);
        set => SetValue(MarkShape2Property, value);
    }

    public static readonly DependencyProperty MarkShape2Property = DependencyProperty.Register(
        nameof(MarkShape2),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot2

    #region Plot3

    public string Plot3Title
    {
        get => (string)GetValue(Plot3TitleProperty);
        set => SetValue(Plot3TitleProperty, value);
    }

    public static readonly DependencyProperty Plot3TitleProperty = DependencyProperty.Register(
        nameof(Plot3Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot3Color
    {
        get => (Color)GetValue(Plot3ColorProperty);
        set => SetValue(Plot3ColorProperty, value);
    }

    public static readonly DependencyProperty Plot3ColorProperty = DependencyProperty.Register(
        nameof(Plot3Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[3], PropertyChangedCallback));

    public IEnumerable<Point>? Plot3
    {
        get => (IEnumerable<Point>?)GetValue(Plot3Property);
        set => SetValue(Plot3Property, value);
    }

    public static readonly DependencyProperty Plot3Property = DependencyProperty.Register(
        nameof(Plot3),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape3
    {
        get => (List<Point>?)GetValue(MarkShape3Property);
        set => SetValue(MarkShape3Property, value);
    }

    public static readonly DependencyProperty MarkShape3Property = DependencyProperty.Register(
        nameof(MarkShape3),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot3

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs _)
    {
        if (d is not WpfPlotMultiBehavior behaviors || behaviors.AssociatedObject is null) return;

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

        var resultList = new List<(string title, List<double> Value)>();
        if (Plot0?.Count() > 0)
        {
            resultList.Add(("X", Plot0.Select(t => t.X).ToList()));
            resultList.Add((Plot0Title, Plot0.Select(t => t.Y).ToList()));
        }

        if (Plot1?.Count() > 0)
        {
            resultList.Add(("X", Plot1.Select(t => t.X).ToList()));
            resultList.Add((Plot1Title, Plot1.Select(t => t.Y).ToList()));
        }

        if (Plot2?.Count() > 0)
        {
            resultList.Add(("X", Plot2.Select(t => t.X).ToList()));
            resultList.Add((Plot2Title, Plot2.Select(t => t.Y).ToList()));
        }

        if (Plot3?.Count() > 0)
        {
            resultList.Add(("X", Plot3.Select(t => t.X).ToList()));
            resultList.Add((Plot3Title, Plot3.Select(t => t.Y).ToList()));
        }

        // 获取最大行数
        var maxRows = resultList.Max(r => r.Value.Count);

        // 创建写入Excel的数据结构
        var values = new List<Dictionary<string, object>>();

        // 将每一行的数据作为字典的一个元素
        for (var i = 0; i < maxRows; i++)
        {
            var row = new Dictionary<string, object>();
            foreach (var (title, Value) in resultList.Where(result => i < result.Value.Count))
            {
                row[title] = Value[i]; // 防止索引越界
            }

            values.Add(row);
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // 将数据写入 Excel 文件
        MiniExcel.SaveAs(filePath, values);
    }

    private void UpdatePlotList()
    {
        AssociatedObject.Plot.Title(Title);
        AssociatedObject.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);

        if (Plot0?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot0.Select(t => t.X).ToList(), Plot0.Select(t => t.Y).ToList(), Plot0Color);
            scatterPoints.LegendText = Plot0Title;
            if (MarkShape0 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[])[.. MarkShape0.Select(t => t.X)], [.. MarkShape0.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot0Color;
            }
        }

        if (Plot1?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot1.Select(t => t.X).ToList(), Plot1.Select(t => t.Y).ToList(), Plot1Color);
            scatterPoints.LegendText = Plot1Title;
            if (MarkShape1 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[])[.. MarkShape1.Select(t => t.X)], [.. MarkShape1.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot1Color;
            }
        }

        if (Plot2?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot2.Select(t => t.X).ToList(), Plot2.Select(t => t.Y).ToList(), Plot2Color);
            scatterPoints.LegendText = Plot2Title;
            if (MarkShape2 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[])[.. MarkShape2.Select(t => t.X)], [.. MarkShape2.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot2Color;
            }
        }

        if (Plot3?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot3.Select(t => t.X).ToList(), Plot3.Select(t => t.Y).ToList(), Plot3Color);
            if (MarkShape3 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[])[.. MarkShape3.Select(t => t.X)], [.. MarkShape3.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot3Color;
            }

            scatterPoints.LegendText = Plot3Title;
        }

        AssociatedObject.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        AssociatedObject.Plot.Axes.AutoScale();
        AssociatedObject.Refresh();
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