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

    public bool IsShowLegend
    {
        get => (bool)GetValue(IsShowLegendProperty);
        set => SetValue(IsShowLegendProperty, value);
    }

    public static readonly DependencyProperty IsShowLegendProperty = DependencyProperty.Register(
        nameof(IsShowLegend),
        typeof(bool),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(true, PropertyChangedCallback)
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

    #region Plot4

    public string Plot4Title
    {
        get => (string)GetValue(Plot4TitleProperty);
        set => SetValue(Plot4TitleProperty, value);
    }

    public static readonly DependencyProperty Plot4TitleProperty = DependencyProperty.Register(
        nameof(Plot4Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot4Color
    {
        get => (Color)GetValue(Plot4ColorProperty);
        set => SetValue(Plot4ColorProperty, value);
    }

    public static readonly DependencyProperty Plot4ColorProperty = DependencyProperty.Register(
        nameof(Plot4Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[4], PropertyChangedCallback));

    public IEnumerable<Point>? Plot4
    {
        get => (IEnumerable<Point>?)GetValue(Plot4Property);
        set => SetValue(Plot4Property, value);
    }

    public static readonly DependencyProperty Plot4Property = DependencyProperty.Register(
        nameof(Plot4),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape4
    {
        get => (List<Point>?)GetValue(MarkShape4Property);
        set => SetValue(MarkShape4Property, value);
    }

    public static readonly DependencyProperty MarkShape4Property = DependencyProperty.Register(
        nameof(MarkShape4),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot4

    #region Plot5

    public string Plot5Title
    {
        get => (string)GetValue(Plot5TitleProperty);
        set => SetValue(Plot5TitleProperty, value);
    }

    public static readonly DependencyProperty Plot5TitleProperty = DependencyProperty.Register(
        nameof(Plot5Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot5Color
    {
        get => (Color)GetValue(Plot5ColorProperty);
        set => SetValue(Plot5ColorProperty, value);
    }

    public static readonly DependencyProperty Plot5ColorProperty = DependencyProperty.Register(
        nameof(Plot5Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[5], PropertyChangedCallback));

    public IEnumerable<Point>? Plot5
    {
        get => (IEnumerable<Point>?)GetValue(Plot5Property);
        set => SetValue(Plot5Property, value);
    }

    public static readonly DependencyProperty Plot5Property = DependencyProperty.Register(
        nameof(Plot5),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape5
    {
        get => (List<Point>?)GetValue(MarkShape5Property);
        set => SetValue(MarkShape5Property, value);
    }

    public static readonly DependencyProperty MarkShape5Property = DependencyProperty.Register(
        nameof(MarkShape5),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot5

    #region Plot6

    public string Plot6Title
    {
        get => (string)GetValue(Plot6TitleProperty);
        set => SetValue(Plot6TitleProperty, value);
    }

    public static readonly DependencyProperty Plot6TitleProperty = DependencyProperty.Register(
        nameof(Plot6Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot6Color
    {
        get => (Color)GetValue(Plot6ColorProperty);
        set => SetValue(Plot6ColorProperty, value);
    }

    public static readonly DependencyProperty Plot6ColorProperty = DependencyProperty.Register(
        nameof(Plot6Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[6], PropertyChangedCallback));

    public IEnumerable<Point>? Plot6
    {
        get => (IEnumerable<Point>?)GetValue(Plot6Property);
        set => SetValue(Plot6Property, value);
    }

    public static readonly DependencyProperty Plot6Property = DependencyProperty.Register(
        nameof(Plot6),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape6
    {
        get => (List<Point>?)GetValue(MarkShape6Property);
        set => SetValue(MarkShape6Property, value);
    }

    public static readonly DependencyProperty MarkShape6Property = DependencyProperty.Register(
        nameof(MarkShape6),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot6

    #region Plot7

    public string Plot7Title
    {
        get => (string)GetValue(Plot7TitleProperty);
        set => SetValue(Plot7TitleProperty, value);
    }

    public static readonly DependencyProperty Plot7TitleProperty = DependencyProperty.Register(
        nameof(Plot7Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot7Color
    {
        get => (Color)GetValue(Plot7ColorProperty);
        set => SetValue(Plot7ColorProperty, value);
    }

    public static readonly DependencyProperty Plot7ColorProperty = DependencyProperty.Register(
        nameof(Plot7Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[7], PropertyChangedCallback));

    public IEnumerable<Point>? Plot7
    {
        get => (IEnumerable<Point>?)GetValue(Plot7Property);
        set => SetValue(Plot7Property, value);
    }

    public static readonly DependencyProperty Plot7Property = DependencyProperty.Register(
        nameof(Plot7),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape7
    {
        get => (List<Point>?)GetValue(MarkShape7Property);
        set => SetValue(MarkShape7Property, value);
    }

    public static readonly DependencyProperty MarkShape7Property = DependencyProperty.Register(
        nameof(MarkShape7),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot7

    #region Plot8

    public string Plot8Title
    {
        get => (string)GetValue(Plot8TitleProperty);
        set => SetValue(Plot8TitleProperty, value);
    }

    public static readonly DependencyProperty Plot8TitleProperty = DependencyProperty.Register(
        nameof(Plot8Title),
        typeof(string),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(string.Empty, PropertyChangedCallback)
    );

    public Color Plot8Color
    {
        get => (Color)GetValue(Plot8ColorProperty);
        set => SetValue(Plot8ColorProperty, value);
    }

    public static readonly DependencyProperty Plot8ColorProperty = DependencyProperty.Register(
        nameof(Plot8Color),
        typeof(Color),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(Colors.Category10[8], PropertyChangedCallback));

    public IEnumerable<Point>? Plot8
    {
        get => (IEnumerable<Point>?)GetValue(Plot8Property);
        set => SetValue(Plot8Property, value);
    }

    public static readonly DependencyProperty Plot8Property = DependencyProperty.Register(
        nameof(Plot8),
        typeof(IEnumerable<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    public List<Point>? MarkShape8
    {
        get => (List<Point>?)GetValue(MarkShape8Property);
        set => SetValue(MarkShape8Property, value);
    }

    public static readonly DependencyProperty MarkShape8Property = DependencyProperty.Register(
        nameof(MarkShape8),
        typeof(List<Point>),
        typeof(WpfPlotMultiBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    #endregion Plot8

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

        if (Plot4?.Count() > 0)
        {
            resultList.Add(("X", Plot4.Select(t => t.X).ToList()));
            resultList.Add((Plot4Title, Plot4.Select(t => t.Y).ToList()));
        }

        if (Plot5?.Count() > 0)
        {
            resultList.Add(("X", Plot5.Select(t => t.X).ToList()));
            resultList.Add((Plot5Title, Plot5.Select(t => t.Y).ToList()));
        }

        if (Plot6?.Count() > 0)
        {
            resultList.Add(("X", Plot6.Select(t => t.X).ToList()));
            resultList.Add((Plot6Title, Plot6.Select(t => t.Y).ToList()));
        }

        if (Plot7?.Count() > 0)
        {
            resultList.Add(("X", Plot7.Select(t => t.X).ToList()));
            resultList.Add((Plot7Title, Plot7.Select(t => t.Y).ToList()));
        }

        if (Plot8?.Count() > 0)
        {
            resultList.Add(("X", Plot8.Select(t => t.X).ToList()));
            resultList.Add((Plot8Title, Plot8.Select(t => t.Y).ToList()));
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
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape0.Select(t => t.X)], [.. MarkShape0.Select(t => t.Y)]);
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
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape1.Select(t => t.X)], [.. MarkShape1.Select(t => t.Y)]);
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
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape2.Select(t => t.X)], [.. MarkShape2.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot2Color;
            }
        }

        if (Plot3?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot3.Select(t => t.X).ToList(), Plot3.Select(t => t.Y).ToList(), Plot3Color);
            if (MarkShape3 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape3.Select(t => t.X)], [.. MarkShape3.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot3Color;
            }

            scatterPoints.LegendText = Plot3Title;
        }

        if (Plot4?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot4.Select(t => t.X).ToList(), Plot4.Select(t => t.Y).ToList(), Plot4Color);
            scatterPoints.LegendText = Plot4Title;
            if (MarkShape4 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape4.Select(t => t.X)], [.. MarkShape4.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot4Color;
            }
        }

        if (Plot5?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot5.Select(t => t.X).ToList(), Plot5.Select(t => t.Y).ToList(), Plot5Color);
            scatterPoints.LegendText = Plot5Title;
            if (MarkShape5 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape5.Select(t => t.X)], [.. MarkShape5.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot5Color;
            }
        }

        if (Plot6?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot6.Select(t => t.X).ToList(), Plot6.Select(t => t.Y).ToList(), Plot6Color);
            scatterPoints.LegendText = Plot6Title;
            if (MarkShape6 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape6.Select(t => t.X)], [.. MarkShape6.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot6Color;
            }
        }

        if (Plot7?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot7.Select(t => t.X).ToList(), Plot7.Select(t => t.Y).ToList(), Plot7Color);
            scatterPoints.LegendText = Plot7Title;
            if (MarkShape7 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape7.Select(t => t.X)], [.. MarkShape7.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot7Color;
            }
        }

        if (Plot8?.Count() > 0)
        {
            var scatterPoints = AssociatedObject.Plot.Add.Scatter(Plot8.Select(t => t.X).ToList(), Plot8.Select(t => t.Y).ToList(), Plot8Color);
            scatterPoints.LegendText = Plot8Title;
            if (MarkShape8 is not null)
            {
                var mk = AssociatedObject.Plot.Add.Markers((double[]) [.. MarkShape8.Select(t => t.X)], [.. MarkShape8.Select(t => t.Y)]);
                mk.MarkerShape = MarkerShape.OpenCircle;
                mk.Color = Plot8Color;
            }
        }

        if (IsShowLegend) AssociatedObject.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        else AssociatedObject.Plot.HideLegend();

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