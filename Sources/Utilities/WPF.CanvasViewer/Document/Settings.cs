using CanvasViewer.Editor.Enum;
using CanvasViewer.Media.Drawing;
using System.Globalization;

namespace CanvasViewer.Document;

public sealed class Settings
{
    private static Dictionary<string, object> Defaults => new()
    {
        { nameof(DisplayPrecision), 3 },
        { nameof(BackColor), Color.FromArgb(0, 0, 0) },
        { nameof(CursorPromptBackColor), Color.FromArgb(84, 58, 84) },
        { nameof(CursorPromptForeColor), Color.FromArgb(128, Color.White) },
        { nameof(SelectionWindowColor), Color.FromArgb(64, 46, 116, 251) },
        { nameof(SelectionWindowBorderColor), Color.White },
        { nameof(ReverseSelectionWindowColor), Color.FromArgb(64, 46, 251, 116) },
        { nameof(ReverseSelectionWindowBorderColor), Color.White },
        { nameof(SelectionHighlightColor), Color.FromArgb(64, 46, 116, 251) },
        { nameof(ControlPointColor), Color.FromArgb(46, 116, 251) },
        { nameof(ActiveControlPointColor), Color.FromArgb(251, 116, 46) },
        { nameof(MinorGridColor), Color.FromArgb(64, 64, 64) },
        { nameof(MajorGridColor), Color.FromArgb(96, 96, 96) },
        { nameof(AxisColor), Color.FromArgb(128, 128, 64) },
        { nameof(PickBoxSize), 6 },
        { nameof(ControlPointSize), 7 },
        { nameof(PointSize), 6 },
        { nameof(Snap), true },
        { nameof(SnapPointSize), 11 },
        { nameof(SnapDistance), 25 },
        { nameof(SnapMode), SnapPointTypeEnum.All },
        { nameof(SnapPointColor), Color.FromArgb(251, 251, 116) },
        { nameof(JigColor), Color.Orange },
        { nameof(GetterTimeout), TimeSpan.FromSeconds(120) }
    };

    private readonly Dictionary<string, object> _items = [];

    #region 属性

    /// <summary>
    /// 定义和控制数字格式化
    /// </summary>
    public NumberFormatInfo NumberFormat { get; private set; }

    /// <summary>
    /// 显示精度
    /// </summary>
    public int DisplayPrecision
    {
        get => Get<int>(nameof(DisplayPrecision));
        set => Set(nameof(DisplayPrecision), value);
    }

    /// <summary>
    /// 背景色
    /// </summary>
    public Color BackColor
    {
        get => Get<Color>(nameof(BackColor));
        set => Set(nameof(BackColor), value);
    }

    /// <summary>
    /// 光标提示符背景色
    /// </summary>
    public Color CursorPromptBackColor
    {
        get => Get<Color>(nameof(CursorPromptBackColor));
        set => Set(nameof(CursorPromptBackColor), value);
    }

    /// <summary>
    /// 光标提示符前景色
    /// </summary>
    public Color CursorPromptForeColor
    {
        get => Get<Color>(nameof(CursorPromptForeColor));
        set => Set(nameof(CursorPromptForeColor), value);
    }

    /// <summary>
    /// 选择窗口颜色
    /// </summary>
    public Color SelectionWindowColor
    {
        get => Get<Color>(nameof(SelectionWindowColor));
        set => Set(nameof(SelectionWindowColor), value);
    }

    /// <summary>
    /// 选择窗口边框颜色
    /// </summary>
    public Color SelectionWindowBorderColor
    {
        get => Get<Color>(nameof(SelectionWindowBorderColor));
        set => Set(nameof(SelectionWindowBorderColor), value);
    }

    /// <summary>
    /// 反选窗口颜色
    /// </summary>
    public Color ReverseSelectionWindowColor
    {
        get => Get<Color>(nameof(ReverseSelectionWindowColor));
        set => Set(nameof(ReverseSelectionWindowColor), value);
    }

    /// <summary>
    /// 反选窗口边框颜色
    /// </summary>
    public Color ReverseSelectionWindowBorderColor
    {
        get => Get<Color>(nameof(ReverseSelectionWindowBorderColor));
        set => Set(nameof(ReverseSelectionWindowBorderColor), value);
    }

    /// <summary>
    /// 选择高亮颜色
    /// </summary>
    public Color SelectionHighlightColor
    {
        get => Get<Color>(nameof(SelectionHighlightColor));
        set => Set(nameof(SelectionHighlightColor), value);
    }

    /// <summary>
    /// 控制锚点颜色
    /// </summary>
    public Color ControlPointColor
    {
        get => Get<Color>(nameof(ControlPointColor));
        set => Set(nameof(ControlPointColor), value);
    }

    /// <summary>
    /// 激活颜色
    /// </summary>
    public Color ActiveControlPointColor
    {
        get => Get<Color>(nameof(ActiveControlPointColor));
        set => Set(nameof(ActiveControlPointColor), value);
    }

    /// <summary>
    /// 次要网格颜色
    /// </summary>
    public Color MinorGridColor
    {
        get => Get<Color>(nameof(MinorGridColor));
        set => Set(nameof(MinorGridColor), value);
    }

    /// <summary>
    /// 主要网格颜色
    /// </summary>
    public Color MajorGridColor
    {
        get => Get<Color>(nameof(MajorGridColor));
        set => Set(nameof(MajorGridColor), value);
    }

    /// <summary>
    /// 轴颜色
    /// </summary>
    public Color AxisColor
    {
        get => Get<Color>(nameof(AxisColor));
        set => Set(nameof(AxisColor), value);
    }

    /// <summary>
    /// 光标中心空方框大小
    /// </summary>
    public int PickBoxSize
    {
        get => Get<int>(nameof(PickBoxSize));
        set => Set(nameof(PickBoxSize), value);
    }

    /// <summary>
    /// 控制锚点大小
    /// </summary>
    public int ControlPointSize
    {
        get => Get<int>(nameof(ControlPointSize));
        set => Set(nameof(ControlPointSize), value);
    }

    /// <summary>
    /// 点大小
    /// </summary>
    public int PointSize
    {
        get => Get<int>(nameof(PointSize));
        set => Set(nameof(PointSize), value);
    }

    /// <summary>
    /// 是否显示编辑时候捕捉的锚点
    /// </summary>
    public bool Snap
    {
        get => Get<bool>(nameof(Snap));
        set => Set(nameof(Snap), value);
    }

    /// <summary>
    /// 编辑时候捕捉的锚点类型
    /// </summary>
    public SnapPointTypeEnum SnapMode
    {
        get => Get<SnapPointTypeEnum>(nameof(SnapMode));
        set => Set(nameof(SnapMode), value);
    }

    /// <summary>
    /// 多远距离显示编辑时候捕捉的锚点
    /// </summary>
    public int SnapDistance
    {
        get => Get<int>(nameof(SnapDistance));
        set => Set(nameof(SnapDistance), value);
    }

    /// <summary>
    /// 编辑时候捕捉的锚点颜色
    /// </summary>
    public Color SnapPointColor
    {
        get => Get<Color>(nameof(SnapPointColor));
        set => Set(nameof(SnapPointColor), value);
    }

    /// <summary>
    /// 编辑时候捕捉的锚点大小
    /// </summary>
    public int SnapPointSize
    {
        get => Get<int>(nameof(SnapPointSize));
        set => Set(nameof(SnapPointSize), value);
    }

    /// <summary>
    /// 用于显示抖动颜色
    /// </summary>
    public Color JigColor
    {
        get => Get<Color>(nameof(JigColor));
        set => Set(nameof(JigColor), value);
    }

    /// <summary>
    /// Getter超时时间
    /// </summary>
    public TimeSpan GetterTimeout
    {
        get => Get<TimeSpan>(nameof(GetterTimeout));
        set => Set(nameof(GetterTimeout), value);
    }

    #endregion 属性

    public Settings()
    {
        NumberFormat = new NumberFormatInfo
        {
            NumberDecimalDigits = 3,
            NumberDecimalSeparator = "."
        };
        Reset();
    }

    #region 方法

    public void Set(string name, object value)
    {
        _items[name] = value;
    }

    public object Get(string name)
    {
        return _items[name];
    }

    public T Get<T>(string name)
    {
        return (T)Get(name);
    }

    public void Reset()
    {
        _items.Clear();
        foreach (var pair in Defaults)
        {
            _items.Add(pair.Key, pair.Value);
        }

        UpdateSettings();
    }

    private void UpdateSettings()
    {
        var nfi = new NumberFormatInfo
        {
            NumberDecimalDigits = DisplayPrecision,
            NumberDecimalSeparator = "."
        };
        NumberFormat = nfi;
    }

    #endregion 方法
}