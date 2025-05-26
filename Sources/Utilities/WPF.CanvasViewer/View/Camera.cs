using CanvasViewer.Geometry;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CanvasViewer.View;

/// <summary>
/// 画布视野在相机上显示
/// </summary>
public sealed class Camera : INotifyPropertyChanged
{
    private Point2D _position;
    private double _zoom;
    private double _width;
    private double _height;

    /// <summary>
    /// 画布视野缩放系数
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            SetField(ref _zoom, value);

            if (double.IsNaN(_zoom) || double.IsNegativeInfinity(_zoom) || double.IsPositiveInfinity(_zoom) ||
                _zoom < double.Epsilon * 1000.0f || _zoom > double.MaxValue / 1000.0f)
            {
                SetField(ref _zoom, 1);
            }
        }
    }

    /// <summary>
    /// 相机确定观看位置<br/>
    /// 相机中心(Canvas控件屏幕中心)世界坐标<br/>
    /// 初始化确定笛卡尔坐标轴原点: 以控件屏幕中心(Width/2, Height/2)建立笛卡尔坐标系设置原点, 比如: 向量A(100,100)表示原点向向量A反方向移动, 最后在控件屏幕中心笛卡尔坐标系(-100, -100)位置<br/>
    /// 其他时候: 当前Canvas控件屏幕中心的世界坐标<br/>
    /// </summary>
    public Point2D Position
    {
        get => _position;
        set
        {
            SetField(ref _position, value);
            var x = _position.X;
            var y = _position.Y;
            if (double.IsNaN(x) || double.IsNegativeInfinity(x) || double.IsPositiveInfinity(x) ||
                x < double.MinValue / 1000.0f || x > double.MaxValue / 1000.0f)
            {
                x = 0;
            }

            if (double.IsNaN(y) || double.IsNegativeInfinity(y) || double.IsPositiveInfinity(y) ||
                y < double.MinValue / 1000.0f || y > double.MaxValue / 1000.0f)
            {
                y = 0;
            }

            SetField(ref _position, new Point2D(x, y));
        }
    }

    /// <summary>
    /// 画布视野宽度
    /// </summary>
    public double Width
    {
        get => _width;
        set => SetField(ref _width, value);
    }

    /// <summary>
    /// 画布视野高度
    /// </summary>
    public double Height
    {
        get => _height;
        set => SetField(ref _height, value);
    }

    public Camera(Point2D position, double zoom, double width, double height, PropertyChangedEventHandler propertyChangedEventHandler)
    {
        Position = position;
        Zoom = zoom;
        Width = width;
        Height = height;
        PropertyChanged -= propertyChangedEventHandler;
        PropertyChanged += propertyChangedEventHandler;
    }

    #region Event

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion Event
}