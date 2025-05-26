using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Extensions;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Net.Utilities.WPF.UI.Nlog;

[INotifyPropertyChanged]
public partial class NLogViewer
{
    private static readonly object LockObj = new();
    private static readonly MethodInfo RenderLogEventMethod = typeof(NLogViewerTarget).GetMethod("RenderLogEvent", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Layout), typeof(LogEventInfo)], null)!;
    private bool _isLoaded;

    #region 属性

    public static readonly DependencyProperty MinLogLevelEnumProperty = DependencyProperty.Register(
        nameof(MinLogLevelEnum),
        typeof(LogLevelEnum),
        typeof(NLogViewer),
        new FrameworkPropertyMetadata(LogLevelEnum.Trace, PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NLogViewer nLogViewer) return;
        if (e.NewValue is not LogLevelEnum logLevelEnum) return;

        nLogViewer._minLogLevelOrdinal = logLevelEnum.ToNLogLevel().Ordinal;
        nLogViewer.ApplyLogLevel(logLevelEnum);
    }

    [Category("NLogViewerColors")]
    public string TraceForeground { get; set; } = Brushes.Gray.ToString();

    [Category("NLogViewerColors")]
    public string DebugForeground { get; set; } = Brushes.Black.ToString();

    [Category("NLogViewerColors")]
    public string InfoForeground { get; set; } = Brushes.Blue.ToString();

    [Category("NLogViewerColors")]
    public string WarnForeground { get; set; } = Brushes.Orange.ToString();

    [Category("NLogViewerColors")]
    public string ErrorForeground { get; set; } = Brushes.Red.ToString();

    [Category("NLogViewerColors")]
    public string FatalForeground { get; set; } = Brushes.DarkRed.ToString();

    [Category("NLogViewer")]
    public string TargetName { get; set; } = string.Empty;

    [Category("NLogViewer")]
    public bool IsPause { get; set; }

    [Category("NLogViewer")]
    public bool IsAutoScroll { get; set; } = true;

    [Category("NLogViewer")]
    public int MaxCount { get; set; } = 200;

    [Category("NLogViewer")]
    public LogLevelEnum MinLogLevelEnum
    {
        get => (LogLevelEnum)GetValue(MinLogLevelEnumProperty);
        set => SetValue(MinLogLevelEnumProperty, value);
    }

    #endregion 属性

    private NLogViewerTarget? _nLogViewerTarget;
    private LoggingRule? _loggingRule;
    private IDisposable? _subscription;
    private int _minLogLevelOrdinal;

    public NLogViewer()
    {
        InitializeComponent();
        NlogUniformSpacingPanel.DataContext = this;

        Loaded += OnLoaded;
        Unloaded += (_, _) => _subscription?.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        var (nLogViewerTarget, loggingRule) = NLogViewerTarget.GetInstance(TargetName);
        _nLogViewerTarget = nLogViewerTarget;
        _loggingRule = loggingRule;

        if (_nLogViewerTarget is null || _loggingRule is null) return;

        ApplyLogLevel(MinLogLevelEnum);
        const string initial = "<Paragraph xmlns =\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\">";
        var stringBuilder = new StringBuilder(initial);

        _subscription = _nLogViewerTarget.Cache // 队列, Subscribe保证串行执行, 所以不会混乱顺序
            .Buffer(TimeSpan.FromMilliseconds(100))
            .Where(x => x.Any())
            .Subscribe(logEventInfos => // 多个订阅者, 保证线程安全
            {
                var isRelease = false;
                try
                {
                    if (IsPause) return;

                    Monitor.TryEnter(LockObj, TimeSpan.FromMilliseconds(1000), ref isRelease); // lock优化防止死锁
                    if (isRelease == false) throw new TimeoutException("Failed to acquire lock");

                    foreach (var logEventInfo in logEventInfos.Where(t => t.Level.Ordinal >= _minLogLevelOrdinal))
                    {
                        stringBuilder.Append("<Run ");
                        stringBuilder.Append("Foreground=\"");
                        stringBuilder.Append(GetForeground(logEventInfo));
                        stringBuilder.Append("\">");
                        stringBuilder.Append(SecurityElement.Escape((string)RenderLogEventMethod.Invoke(_nLogViewerTarget, [_nLogViewerTarget.Layout, logEventInfo])!)); // 转为有效Xml语句
                        stringBuilder.AppendLine("</Run>");
                    }

                    stringBuilder.AppendLine("</Paragraph>");

                    var str = stringBuilder.ToString();
                    stringBuilder.Clear();
                    stringBuilder.Append(initial);
                    _ = RichTextBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, Write, str) /*.GetAwaiter().GetResult()*/; // 不等待会卡主线程
                }
                catch (Exception ex)
                {
                    InternalLogger.Error(ex, "{0}: Failed to append RichTextBox", this);
                }
                finally
                {
                    if (isRelease) Monitor.Exit(LockObj);
                }
            });
    }

    private void ButtonClearOnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // 获取 RichTextBox 的内容
            // var textRange = new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd);
            //// 将内容复制到剪贴板
            //Clipboard.SetText(textRange.Text);
            // 清除 RichTextBox 的内容
            ((Paragraph)RichTextBox.Document.Blocks.LastBlock!).Inlines.Clear();
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, "{0}: Failed to clear RichTextBox", this);
        }
    }

    private void Write(string xamlParagraphText)
    {
        Paragraph parsedParagraph;

        try
        {
            parsedParagraph = (Paragraph)XamlReader.Parse(xamlParagraphText);
        }
        catch (XamlParseException ex)
        {
            InternalLogger.Error(ex, "Error parsing `{0}` to XAML", xamlParagraphText);
            return;
        }

        var inlines = parsedParagraph.Inlines.ToList();
        var flowDocument = RichTextBox.Document ??= new FlowDocument();

        if (flowDocument.Blocks.LastBlock is not Paragraph paragraph)
        {
            paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);
        }

        paragraph.Inlines.AddRange(inlines);

        if (MaxCount >= 0 & paragraph.Inlines.Count - MaxCount / 2 > MaxCount)
        {
            var blocksToRemove = paragraph.Inlines.Count - MaxCount;
            for (var i = 0; i < blocksToRemove; i++) paragraph.Inlines.Remove(paragraph.Inlines.FirstInline);
        }

        if (IsAutoScroll) RichTextBox.ScrollToEnd();
    }

    private string GetForeground(LogEventInfo logEventInfo) =>
        logEventInfo.Level.Name switch
        {
            { } temp when temp == LogLevel.Trace.Name => TraceForeground,
            { } temp when temp == LogLevel.Debug.Name => DebugForeground,
            { } temp when temp == LogLevel.Info.Name => InfoForeground,
            { } temp when temp == LogLevel.Warn.Name => WarnForeground,
            { } temp when temp == LogLevel.Error.Name => ErrorForeground,
            { } temp when temp == LogLevel.Fatal.Name => FatalForeground,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(logEventInfo))
        };

    private void ApplyLogLevel(LogLevelEnum logLevelEnum)
    {
        if (_loggingRule is null) return;

        MinLogLevelEnum = logLevelEnum;
        _loggingRule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
        _loggingRule.EnableLoggingForLevels(logLevelEnum.ToNLogLevel(), LogLevel.Fatal);
        LogManager.ReconfigExistingLoggers(); // 使配置生效
    }
}