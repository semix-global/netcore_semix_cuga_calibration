using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Extensions;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using NLog.Windows.Forms;
using Point = Net.Utilities.Models.Point;

namespace NlogTest;

public partial class MainLogForm : Form
{
    private readonly ILogger<MainLogForm> _logger;
    private readonly Guid _guid = Guid.NewGuid();
    private readonly Random _random = new();

    public MainLogForm()
    {
        InitializeComponent();
        RichTextBoxTarget.ReInitializeAllTextboxes(this); // 将 NLog Target 附加到 RichTextBox
        btnCleanLog.Click += (_, _) => { txtLog.Clear(); };

        var host = Host.CreateDefaultBuilder()
            .UseNLog(new NLogProviderOptions { ReplaceLoggerFactory = true })
            .Build();

        _logger = host.Services.GetRequiredService<ILogger<MainLogForm>>();
    }

    private async void ButtonGenerateLogOnClick(object sender, EventArgs e)
    {
        await Task.Run(() =>
        {
            _logger.LogHtmlInformation("1. Microscope Focus Calibration", HtmlHeaderLevelEnum.Header1, _guid.LoggingHtml());
            _logger.LogHtmlInformation(new HtmlHeader("Step3", HtmlHeaderLevelEnum.Header2), _guid.LoggingHtml());
            _logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                EcsValue = _random.NextDouble(),
                ContentAlignment.BottomCenter,
                FindFocusPosition = new Point(_random.NextDouble() * 10000, _random.NextDouble() * 10000),
                FindFocusLimit = _random.NextDouble(),
                FindFocusInterval = _random.NextDouble(),
                Plot = new HtmlPlot2DLinesChart([
                    ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                    ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                    ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints())
                ], "test"),
                Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", "test", [new HtmlImageCrossOverlay(true)])
            }), _guid.LoggingHtml());

            _logger.LogHtmlInformation("Action Times1", HtmlHeaderLevelEnum.Header3, _guid.LoggingHtml());

            var listAndImage = new List<object>();
            foreach (var i in Enumerable.Range(1, 100))
            {
                var item = new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new PointF((float)_random.NextDouble() * 10000f, (float)_random.NextDouble() * 10000f),
                    Array = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                };
                listAndImage.Add(item);

                _logger.LogHtmlInformation($"{i}. Find Focus time", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(_random.NextDouble() * 10000, _random.NextDouble() * 10000),
                    FindFocusLimit = _random.NextDouble(),
                    FindFocusInterval = _random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("x", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("y", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("z", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(_random.NextDouble() > 0.5)])
                }), _guid.LoggingHtml());
            }

            _logger.LogHtmlInformation("Action Times2", HtmlHeaderLevelEnum.Header3, _guid.LoggingHtml());

            var listAndImage1 = new List<object>();
            foreach (var i in Enumerable.Range(1, 100))
            {
                var item = new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new PointF((float)_random.NextDouble() * 10000f, (float)_random.NextDouble() * 10000f),
                    Array = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                };
                listAndImage1.Add(item);

                _logger.LogHtmlInformation($"{i}. Find Focus time", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(_random.NextDouble() * 10000, _random.NextDouble() * 10000),
                    FindFocusLimit = _random.NextDouble(),
                    FindFocusInterval = _random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("x", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints(),1),
                        ("y", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints(),5),
                        ("z", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints(),10),
                         ("s", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints(),8),
                          ("a", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints(),7)
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(_random.NextDouble() > 0.5)])
                }), _guid.LoggingHtml());
            }

            _logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, _guid.LoggingHtml());
            _logger.LogHtmlInformation(new HtmlTable([.. listAndImage]), _guid.LoggingHtml());

            _logger.LogHtmlCritical(
                new InvalidOperationException(
                    "Do the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do better"),
                "Exception", HtmlHeaderLevelEnum.Header3, _guid.LoggingHtml());

            _logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"), _guid.LoggingHtml());
            _logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer(
            [
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlQuote(new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(_random.NextDouble() * 10000, _random.NextDouble() * 10000),
                    FindFocusLimit = _random.NextDouble(),
                    FindFocusInterval = _random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                new HtmlBullet(new
                {
                    EcsValue = _random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(_random.NextDouble() * 10000, _random.NextDouble() * 10000),
                    FindFocusLimit = _random.NextDouble(),
                    FindFocusInterval = _random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => _random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            ]), _guid.LoggingHtml());

            try
            {
                var i = 1;
                _ = 1 / (i - i);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "test");
                _logger.LogHtmlError(ex, "test", HtmlHeaderLevelEnum.Header3, _guid.LoggingHtml());
            }

            _logger.LogHtmlTrace("test", HtmlHeaderLevelEnum.Header3, _guid.LoggedEndHtml());
        });
    }

    private void ButtonGenerateImageLogOnClick(object sender, EventArgs e)
    {
        _logger.LogHtmlInformation("1. Microscope Focus Calibration", HtmlHeaderLevelEnum.Header1, _guid.LoggingHtml());

        _logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", "test")
        }), _guid.LoggingHtml());

        _logger.LogHtmlInformation("Tab", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Download1 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\NLog.config"), "NLog.config"),
            Download2 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg"), "test.jpg"),
            HtmlTab = new HtmlTab(new
            {
                test0 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test"),
                test1 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", Description: "test"),
                test2 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test3 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test4 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays: []),
                test5 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays: []),
                test6 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test7 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 5d, 257 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test8 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test9 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test10 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100))
                ]),
                test11 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100))
                ]),
                test12 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Size(100, 100))
                ]),
                test13 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 - 1, 257 - 1), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 - 1, 257 - 1), new Net.Utilities.Models.Size(100, 100))
                ]),
                test14 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test15 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test1.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 2d, 257 / 2d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 / 2d, 257 / 2d), new Net.Utilities.Models.Size(100, 100))
                ]),
            })
        }), _guid.LoggingHtml());
        _logger.LogHtmlInformation("Tab", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Download1 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\NLog.config"), "NLog.config"),
            Download2 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg"), "test.jpg"),
            HtmlTab = new HtmlTab(new
            {
                test0 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test"),
                test1 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", Description: "test"),
                test2 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test3 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", Description: "test", HtmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test4 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays: []),
                test5 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays: []),
                test6 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test7 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 5d, 1437 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test8 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test9 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test10 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100))
                ]),
                test11 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Size(100, 100))
                ]),
                test12 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Size(100, 100))
                ]),
                test13 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 - 1, 1437 - 1), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 - 1, 1437 - 1), new Net.Utilities.Models.Size(100, 100))
                ]),
                test14 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test.jpg", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Size(100, 100))
                ]),
                test15 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\test2.png", HtmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 2d, 1437 / 2d), new Net.Utilities.Models.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 2d, 1437 / 2d), new Net.Utilities.Models.Size(100, 100))
                ]),
            })
        }), _guid.LoggingHtml());

        _logger.LogHtmlTrace("test", HtmlHeaderLevelEnum.Header3, _guid.LoggedEndHtml());
    }
}