using Core.Utilities.WPF.Entities;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Core.Utilities.WPF.UI;

public partial class MarkdownViewer
{
    private bool _isWebView2Initialized;
    private string? _lastLoadedFilePath;
    private Task? _currentLoadTask;
    private bool _needsReloadOnVisible;
    private string? _pendingFilePath; // 在控件不可见时待加载的文件路径
    private int _loadCount; // 用于内存管理的加载计数器
    private const int GcInterval = 10; // 每加载10次触发一次轻量级GC
    private DispatcherTimer? _resizeTimer; // 自适应布局相关字段
    private Size _lastSize;

    public static readonly DependencyProperty IsPopupEnabledProperty = DependencyProperty.Register(
        nameof(IsPopupEnabled), typeof(bool), typeof(MarkdownViewer),
        new PropertyMetadata(true));

    public bool IsPopupEnabled
    {
        get => (bool)GetValue(IsPopupEnabledProperty);
        set => SetValue(IsPopupEnabledProperty, value);
    }

    public static readonly DependencyProperty MarkdownFilePathProperty = DependencyProperty.Register(
        nameof(MarkdownFilePath), typeof(string), typeof(MarkdownViewer),
        new PropertyMetadata(null, OnMarkdownFilePathChanged));

    /// <summary>
    /// HTML 文件路径（预生成的完整 HTML，包含 CSS 和 JS）
    /// </summary>
    public string? MarkdownFilePath
    {
        get => (string?)GetValue(MarkdownFilePathProperty);
        set => SetValue(MarkdownFilePathProperty, value);
    }

    private static readonly DependencyPropertyKey IsBusyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsBusy), typeof(bool), typeof(MarkdownViewer), new PropertyMetadata(false));

    public static readonly DependencyProperty IsBusyProperty = IsBusyPropertyKey.DependencyProperty;
    public bool IsBusy => (bool)GetValue(IsBusyProperty);
    private void SetIsBusy(bool value) => SetValue(IsBusyPropertyKey, value);

    public MarkdownViewer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        _lastSize = new Size(0, 0);
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // 当控件从不可见变为可见时，确保WebView2正确渲染
        if (e.NewValue is not true || !_isWebView2Initialized) return;
        MarkdownWebView.UpdateLayout();
        MarkdownWebView.InvalidateVisual();

        // 只在有标志指示需要重新加载时才重新加载（避免不必要的重复加载）
        if (!_needsReloadOnVisible) return;
        _needsReloadOnVisible = false;
        var pathToLoad = _pendingFilePath; // 使用待加载的路径
        _pendingFilePath = null;
        _lastLoadedFilePath = null; // 清空缓存，允许重新加载
        LoadHtmlFileAsync(pathToLoad);
    }

    private void OpenPopupWindow()
    {
        if (string.IsNullOrEmpty(MarkdownFilePath)) return;

        var window = new Window
        {
            Title = "Instructions",
            Width = 1200,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new MarkdownViewer
            {
                MarkdownFilePath = MarkdownFilePath,
                IsPopupEnabled = false
            }
        };
        window.Show();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await MarkdownWebView.EnsureCoreWebView2Async();
            _isWebView2Initialized = true;

            // 配置 WebView2 支持 JavaScript（Prism.js 代码高亮需要）
            MarkdownWebView.CoreWebView2.Settings.IsScriptEnabled = true;
            MarkdownWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

            MarkdownWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            MarkdownWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            if (!string.IsNullOrEmpty(MarkdownFilePath)) LoadHtmlFileAsync(MarkdownFilePath);
        }
        catch
        {
            _isWebView2Initialized = false;
            MarkdownWebView.NavigateToString(MarkdownHtmlBuilder.BuildWebView2NotInstalledHtml());
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            SetIsBusy(false);
            if (IsPopupEnabled)
            {
                MarkdownWebView.ExecuteScriptAsync("""
                                                   var btn = document.getElementById('popup-btn');
                                                   if (btn) btn.style.display = 'block';
                                                   """);
            }
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "popup")
        {
            Dispatcher.Invoke(OpenPopupWindow);
        }
    }

    private static void OnMarkdownFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownViewer viewer) viewer.LoadHtmlFileAsync(e.NewValue as string);
    }

    private async void LoadHtmlFileAsync(string? filePath)
    {
        // 轻量级内存管理：每加载10次触发一次 Gen0 非阻塞GC（性能损失 < 0.1ms/次）
        if (++_loadCount % GcInterval == 0)
            GC.Collect(0, GCCollectionMode.Optimized, blocking: false);

        // 等待之前的加载任务完成
        if (_currentLoadTask is { IsCompleted: false })
        {
            try
            {
                await _currentLoadTask;
            }
            catch
            {
                /* Ignore */
            }
        }

        // 防止重复加载相同文件
        if (!string.IsNullOrEmpty(filePath) && filePath == _lastLoadedFilePath && _isWebView2Initialized)
        {
            // 如果控件当前不可见，标记为需要在变为可见时重新加载
            if (IsVisible) return;
            _needsReloadOnVisible = true;
            _pendingFilePath = filePath;

            return;
        }

        // 如果控件不可见且WebView2已初始化，延迟加载直到控件变为可见
        if (!IsVisible && _isWebView2Initialized)
        {
            _needsReloadOnVisible = true;
            _pendingFilePath = filePath;
            _lastLoadedFilePath = null; // 清空缓存，确保下次可见时能加载
            return;
        }

        // 清除延迟加载标志和待加载路径（因为我们即将加载）
        _needsReloadOnVisible = false;
        _pendingFilePath = null;
        _currentLoadTask = LoadHtmlFileInternalAsync(filePath);
        await _currentLoadTask;
    }

    private async Task LoadHtmlFileInternalAsync(string? filePath)
    {
        if (!_isWebView2Initialized) return;

        SetIsBusy(true);
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MarkdownWebView.NavigateToString(MarkdownHtmlBuilder.BuildFallbackHtml("Missing markdown document", "Please supplement markdown and set the MarkdownFilePath property to load the document。", "ℹ️"));
                _lastLoadedFilePath = null;
                _needsReloadOnVisible = false;
                _pendingFilePath = null;
                return;
            }

            if (!File.Exists(filePath))
            {
                MarkdownWebView.NavigateToString(MarkdownHtmlBuilder.BuildFallbackHtml("Missing markdown document", $"File does not exist：{filePath}", "⚠️"));
                _lastLoadedFilePath = null;
                _needsReloadOnVisible = false;
                _pendingFilePath = null;
                return;
            }

            // 直接通过 file:// URI 加载本地 HTML 文件
            var fileUri = new Uri(filePath).AbsoluteUri;
            MarkdownWebView.CoreWebView2.Navigate(fileUri);
            _lastLoadedFilePath = filePath;
        }
        catch (Exception ex)
        {
            MarkdownWebView.NavigateToString(MarkdownHtmlBuilder.BuildFallbackHtml("Loading error", $"An error occurred while loading the file：{ex.Message}", "❌"));
            _lastLoadedFilePath = null;
            _needsReloadOnVisible = false;
            _pendingFilePath = null;
            SetIsBusy(false);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isWebView2Initialized) return;

        // 防抖：停止现有定时器并在每次 size 变化时重置
        _resizeTimer?.Stop();

        if (_resizeTimer == null)
        {
            _resizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _resizeTimer.Tick += OnResizeTimerTick;
        }

        _resizeTimer.Start();
    }

    private async void OnResizeTimerTick(object? sender, EventArgs e)
    {
        _resizeTimer?.Stop();

        var currentSize = RenderSize;
        if (currentSize == _lastSize) return;
        _lastSize = currentSize;

        if (!_isWebView2Initialized) return;

        // 等待 WPF 完全空闲，确保所有消息（包括 HWND 的 WM_SIZE）已处理完毕
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        // 强制 WebView2 更新布局和重绘
        MarkdownWebView.UpdateLayout();
        MarkdownWebView.InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // 清理资源，防止内存泄漏
        if (_resizeTimer == null) return;
        _resizeTimer.Stop();
        _resizeTimer.Tick -= OnResizeTimerTick;
        _resizeTimer = null;
    }
}