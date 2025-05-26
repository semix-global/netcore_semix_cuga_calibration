using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.AttachedHelper;

public sealed class GridHelper
{
    #region Rows

    /// <summary>
    /// 自动为 Grid 控件添加 RowDefinitions
    /// </summary>
    /// <remarks>
    /// 支持的写法包括：<br/>
    /// - Star: 1*, *<br/>
    /// - Auto: auto, Auto<br/>
    /// - Pixel: 100, 10.5, 0
    /// </remarks>
    public static readonly DependencyProperty RowsProperty = DependencyProperty.RegisterAttached(
        "Rows",
        typeof(string),
        typeof(GridHelper),
        /*
         *  AffectsArrange：指示属性值的更改将影响 UI 元素的排列。当属性值更改时，系统将触发 UI 元素的重新排列，这通常发生在元素的布局更新时
         *  BindsTwoWayByDefault：指示在数据绑定时，默认情况下属性应该是双向绑定的。这意味着当你绑定到该属性时，默认情况下会启用双向绑定
         *  Inherits：指示属性值应该被子元素继承。当在父元素上设置属性时，子元素将继承该属性值。这对于定义应该由子元素继承的属性很有用
         */
        new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender // 当属性值更改时，系统将触发重新绘制相关的 UI 元素
            | FrameworkPropertyMetadataOptions.AffectsMeasure // 当属性值更改时，系统将触发 UI 元素的重新测量
            | FrameworkPropertyMetadataOptions.NotDataBindable, // 指示此依赖属性不应绑定到数据源
            OnRowsChanged
        )
    );

    public static string? GetRows(DependencyObject obj)
    {
        return (string?)obj.GetValue(RowsProperty);
    }

    public static void SetRows(DependencyObject obj, string value)
    {
        obj.SetValue(RowsProperty, value);
    }

    private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid || e.NewValue is not string rows)
            throw new ArgumentException($"Invalid Rows property value: {e.NewValue}");

        grid.RowDefinitions.Clear();

        foreach (var row in rows.Split(',').Select(r => r.Trim()))
        {
            var gridLength = ParseGridLength(row);
            grid.RowDefinitions.Add(new RowDefinition { Height = gridLength });
        }
    }

    #endregion Rows

    #region Columns

    /// <summary>
    /// 自动为 Grid 控件添加 ColumnDefinitions。详见 <seealso cref="RowsProperty"/>
    /// </summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
        "Columns",
        typeof(string),
        typeof(GridHelper),
        /*
         *  AffectsArrange：指示属性值的更改将影响 UI 元素的排列。当属性值更改时，系统将触发 UI 元素的重新排列，这通常发生在元素的布局更新时
         *  BindsTwoWayByDefault：指示在数据绑定时，默认情况下属性应该是双向绑定的。这意味着当你绑定到该属性时，默认情况下会启用双向绑定
         *  Inherits：指示属性值应该被子元素继承。当在父元素上设置属性时，子元素将继承该属性值。这对于定义应该由子元素继承的属性很有用
         */
        new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender // 当属性值更改时，系统将触发重新绘制相关的 UI 元素
            | FrameworkPropertyMetadataOptions.AffectsMeasure // 当属性值更改时，系统将触发 UI 元素的重新测量
            | FrameworkPropertyMetadataOptions.NotDataBindable, // 指示此依赖属性不应绑定到数据源
            OnColumnsChanged
        )
    );

    public static string? GetColumns(DependencyObject obj)
    {
        return (string?)obj.GetValue(ColumnsProperty);
    }

    public static void SetColumns(DependencyObject obj, string value)
    {
        obj.SetValue(ColumnsProperty, value);
    }

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid || e.NewValue is not string rows)
            throw new ArgumentException($"Invalid Rows property value: {e.NewValue}");

        grid.ColumnDefinitions.Clear();

        foreach (var row in rows.Split(',').Select(r => r.Trim()))
        {
            var gridLength = ParseGridLength(row);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = gridLength });
        }
    }

    #endregion Columns

    private static GridLength ParseGridLength(string length)
    {
        // *, 1*, 2.5*
#if NET
        if (length.EndsWith('*'))
#else
        if (length.EndsWith("*"))
#endif
        {
            double star = 1;
            if (length.Length > 1) star = double.Parse(length[..^1]);

            return new GridLength(star, GridUnitType.Star);
        }
        // a, auto, A, Auto

        if (length.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return GridLength.Auto;
        }

        // 100
        if (double.TryParse(length, out var height))
        {
            return new GridLength(height);
        }

        throw new ArgumentException($"Invalid height value: {length}");
    }
}