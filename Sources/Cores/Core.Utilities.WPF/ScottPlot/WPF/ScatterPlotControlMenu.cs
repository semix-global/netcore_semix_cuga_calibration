using Core.Utilities.WPF.ScottPlot.Extensions;
using Microsoft.Win32;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Collections.Concurrent;
using System.Windows;

namespace Core.Utilities.WPF.ScottPlot.WPF;

public sealed class ScatterPlotControlMenu(ScatterPlotControl scatterPlotControl) : WpfPlotMenu(scatterPlotControl), IPlotMenu
{
    private static readonly ConcurrentDictionary<Plot, Window> WindowDic = new();

    private const string ShowLegendItem = "Show Legend Item";
    private const string HideLegendItem = "High Legend Item";

    public new ContextMenuItem[] GetDefaultContextMenuItems() =>
    [
        new()
        {
            Label = HideLegendItem,
            OnInvoke = ToggleLegendItem,
        },
        new()
        {
            Label = "Open in New Window",
            OnInvoke = OpenInNewWindow,
        },
        new() { IsSeparator = true },
        new()
        {
            Label = "Save Image",
            OnInvoke = OpenSaveImageDialog
        },
        new()
        {
            Label = "Copy to Clipboard",
            OnInvoke = CopyImageToClipboard
        },
        new()
        {
            Label = "Save Excel",
            OnInvoke = SaveExcel
        },
        new() { IsSeparator = true }
    ];

    public void ToggleLegendItem(Plot plot)
    {
        var index = ContextMenuItems.FindIndex(t => t.Label == (plot.Legend.ShowItemsFromHiddenPlottables ? HideLegendItem : ShowLegendItem));
        plot.Legend.ShowItemsFromHiddenPlottables = !plot.Legend.ShowItemsFromHiddenPlottables;

        var contextMenuItem = ContextMenuItems[index];
        contextMenuItem.Label = plot.Legend.ShowItemsFromHiddenPlottables ? HideLegendItem : ShowLegendItem;
        ContextMenuItems[index] = contextMenuItem;

        scatterPlotControl.Refresh();
    }

    public new void OpenInNewWindow(Plot plot)
    {
        var title = $"Interactive Plot: {plot.GetTitle()}";

        if (WindowDic.TryGetValue(plot, out var win) == false)
        {
            var originalControl = plot.PlotControl;

            var plotControlTemp = new ScatterPlotControl(scatterPlotControl);
            plotControlTemp.ConfigureScatter();

            plotControlTemp.Reset(plot);

            win = new Window
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Width = 600,
                Height = 400,
                Title = title,
                Content = plotControlTemp,
                Topmost = true
            };
            ((IDictionary<Plot, Window>)WindowDic).Add(plot, win);

            win.Closed += (_, _) =>
            {
                plot.PlotControl = originalControl;

                ((IDictionary<Plot, Window>)WindowDic).Remove(plot);
            };
        }

        win.Show();

        scatterPlotControl.Refresh();
    }

    public void SaveExcel(Plot plot)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "Save Excel File",
            FileName = $"Plot{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        var plots = plot.PlottableList.OfType<Scatter>().Select(t =>
        {
            t.Data.MinRenderIndex = 0;
            t.Data.MaxRenderIndex = int.MaxValue;

            return (Name: t.LegendText, Points: t.Data.GetScatterPoints().Select(tt => new Point(tt.X, tt.Y)).ToArray());
        }).ToArray();

        var sheets = new Dictionary<string, object>();
        var values = new List<Dictionary<string, object?>>();

        var maxLength = plots.Max(t => t.Points.Length);
        for (var i = 0; i < maxLength; i++)
        {
            var dic = new Dictionary<string, object?>();
            foreach (var (name, points) in plots)
            {
                var x = string.IsNullOrWhiteSpace(name) ? nameof(Point.X) : $"{name}-{nameof(Point.X)}";
                var y = string.IsNullOrWhiteSpace(name) ? nameof(Point.Y) : $"{name}-{nameof(Point.Y)}";

                dic[x] = i < points.Length ? points[i].X : null;
                dic[y] = i < points.Length ? points[i].Y : null;
            }

            values.Add(dic);
        }

        sheets.Add("ALL", values);

        foreach (var (name, points) in plots)
        {
            sheets[name] = points.Select(t => new
            {
                t.X,
                t.Y
            }).ToList();
        }

        FileHelper.DeleteFileIfExists(dialog.FileName);
        MiniExcel.SaveAs(dialog.FileName, sheets);
    }

    public new void Reset()
    {
        Clear();
        ContextMenuItems.AddRange(GetDefaultContextMenuItems());
    }
}