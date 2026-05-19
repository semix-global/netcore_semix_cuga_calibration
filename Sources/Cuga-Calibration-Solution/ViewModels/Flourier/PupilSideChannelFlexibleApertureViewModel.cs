using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Fourier.CameraAlignment;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilSideChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilSideChannelFlexibleApertureViewModel : CalibrationViewModelBase
{
    #region 界面相关

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private int _rodNum;

    [ObservableProperty]
    private int _rodBegin = 1;

    [ObservableProperty]
    private int _rodEnd;

    [ObservableProperty]
    private int _rodWidth = 50;

    [ObservableProperty]
    private int _rodHight = 350;

    [ObservableProperty]
    private int _xStartPixel = 100;

    [ObservableProperty]
    private double _ch12Percentage = 0.5;

    [ObservableProperty]
    private double _ch12Percentage1 = 0.5;

    [ObservableProperty]
    private double _ch12Percentage2 = 0.8;

    [ObservableProperty]
    private bool _isSyncingFromDrag;

    [ObservableProperty]
    private string _heightRelationPercentCh1Text = "";

    [ObservableProperty]
    private string _heightRelationPercentCh2Text = "";

    [ObservableProperty]
    private string _heightBeginPercentCh1Text = "";

    [ObservableProperty]
    private string _heightBeginPercentCh2Text = "";

    // 当 RodNum/ RodWidth/ XStartPixel 改变时自动重建矩形集合
    partial void OnRodNumChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    partial void OnRodWidthChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    partial void OnRodHightChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    [ObservableProperty]
    private Point _sxPos;

    [ObservableProperty]
    private int _rodNumber;

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Find Begin And End Rods of Ch1" },
        new() { StepName = "Pupil Side Channel Flexible Aperture Calibration Ch1" },
        new() { StepName = "Find Begin And End Rods of Ch2" },
        new() { StepName = "Pupil Side Channel Flexible Aperture Calibration Ch2" }
    ];

    [ObservableProperty]
    private ObservableCollection<RodInformation> _setAllRods =
    [
        new("Rod1", 0), new("Rod2", 0), new("Rod3", 0), new("Rod4", 0), new("Rod5", 0), new("Rod6", 0),
        new("Rod7", 0), new("Rod8", 0), new("Rod9", 0), new("Rod10", 0), new("Rod11", 0), new("Rod12", 0),
        new("Rod13", 0), new("Rod14", 0), new("Rod15", 0), new("Rod16", 0), new("Rod17", 0), new("Rod18", 0),
        new("Rod19", 0), new("Rod20", 0), new("Rod21", 0), new("Rod22", 0), new("Rod23", 0), new("Rod24", 0),
        new("Rod25", 0), new("Rod26", 0), new("Rod27", 0), new("Rod28", 0), new("Rod29", 0), new("Rod30", 0),
        new("Rod31", 0), new("Rod32", 0), new("Rod33", 0), new("Rod34", 0), new("Rod35", 0), new("Rod36", 0),
        new("Rod37", 0), new("Rod38", 0), new("Rod39", 0), new("Rod40", 0), new("Rod41", 0), new("Rod42", 0),
        new("Rod43", 0), new("Rod44", 0), new("Rod45", 0), new("Rod46", 0)
    ];

    // 所有电线杆集合（绑定到 ListBox）
    [ObservableProperty]
    private ObservableCollection<Pole> _allOddRodsCh1Percent1 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allOddRodsCh1Percent2 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allEvenRodsCh1Percent1 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allEvenRodsCh1Percent2 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allOddRodsCh2Percent1 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allOddRodsCh2Percent2 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allEvenRodsCh2Percent1 = [];

    [ObservableProperty]
    private ObservableCollection<Pole> _allEvenRodsCh2Percent2 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageAxis = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectOddFirstCh1 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectOddSecondCh1 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectEvenFirstCh1 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectEvenSecondCh1 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectOddFirstCh2 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectOddSecondCh2 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectEvenFirstCh2 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectEvenSecondCh2 = [];

    [ObservableProperty]
    private ObservableCollection<double> _allRodsBeginPercentCh1 = [];

    [ObservableProperty]
    private ObservableCollection<double> _allRodsBeginPercentCh2 = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private PupilSideChannelFlexibleApertureDTO _resultDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private PupilCameraAlignmentDTO _pupilCameraAlignmentValue = new();

    [RecipeCache]
    [ObservableProperty]
    private PupilSideChannelFlexibleApertureCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private PupilSideChannelFlexibleApertureDTO _calibration = new();

    [ObservableProperty]
    private PupilSideChannelFlexibleApertureDTO _review = new();

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        PupilCameraAlignmentValue = ApplicationCookieService.GetCalibration<PupilCameraAlignmentDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<PupilSideChannelFlexibleApertureCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<PupilSideChannelFlexibleApertureDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        Cache.OriginImageFilePathList1 = new ObservableCollection<string>(Enumerable.Repeat("123", 4));
        Cache.OriginImageFilePathList2 = new ObservableCollection<string>(Enumerable.Repeat("123", 4));

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:
                Cache.RectROIDrawableList.Clear();
                InitRodState();
                Application.Current.Dispatcher.Invoke(() => { RebuildRectRoiDrawableList(); });

                return true;

            case 2:
                SelectedTabIndex = 1;
                InitRodState();
                Application.Current.Dispatcher.Invoke(() => { RebuildRectRoiDrawableList(); });

                return true;

            case 4:
                SelectedTabIndex = 0;
                Cache.BitmapImageDrawableCh1.BitmapImage = null;
                Cache.BitmapImageDrawableCh2.BitmapImage = null;

                ResultDto.CgFFBoxBeginPositionCh1 = Cache.CgFFBoxBeginPositionCh1;
                ResultDto.CgFFBoxEndPositionCh1 = Cache.CgFFBoxEndPositionCh1;
                ResultDto.CgFFBoxBeginNumber1Ch1 = Cache.CgFFBoxBeginNumber1Ch1;
                ResultDto.CgFFBoxBeginNumber2Ch1 = Cache.CgFFBoxBeginNumber2Ch1;
                ResultDto.CgFFBoxEndNumber1Ch1 = Cache.CgFFBoxEndNumber1Ch1;
                ResultDto.CgFFBoxEndNumber2Ch1 = Cache.CgFFBoxEndNumber2Ch1;
                ResultDto.CgFFBoxRodWidthListCh1 = Cache.CgFFBoxRodWidthListCh1;
                ResultDto.CgFFBoxHeightRelationPercentListCh1 = Cache.CgFFBoxHeightRelationPercentListCh1;
                ResultDto.CurrentImageRectListFirstCh1 = Cache.CurrentImageRectListFirstCh1;
                ResultDto.CgFFBoxAllRodsBeginPercentCh1 = Cache.CgFFBoxAllRodsBeginPercentCh1;

                ResultDto.CgFFBoxBeginPositionCh2 = Cache.CgFFBoxBeginPositionCh2;
                ResultDto.CgFFBoxEndPositionCh2 = Cache.CgFFBoxEndPositionCh2;
                ResultDto.CgFFBoxBeginNumber1Ch2 = Cache.CgFFBoxBeginNumber1Ch2;
                ResultDto.CgFFBoxBeginNumber2Ch2 = Cache.CgFFBoxBeginNumber2Ch2;
                ResultDto.CgFFBoxEndNumber1Ch2 = Cache.CgFFBoxEndNumber1Ch2;
                ResultDto.CgFFBoxEndNumber2Ch2 = Cache.CgFFBoxEndNumber2Ch2;
                ResultDto.CgFFBoxRodWidthListCh2 = Cache.CgFFBoxRodWidthListCh2;
                ResultDto.CgFFBoxHeightRelationPercentListCh2 = Cache.CgFFBoxHeightRelationPercentListCh2;
                ResultDto.CurrentImageRectListFirstCh2 = Cache.CurrentImageRectListFirstCh2;
                ResultDto.CgFFBoxAllRodsBeginPercentCh2 = Cache.CgFFBoxAllRodsBeginPercentCh2;

                ResultDto.IsCalibrated = true;
                Save(ResultDto, cancellationToken);
                return true;

            default:
                InitRodState();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //ImageFilePath = filePath;
                    //BitmapImageDrawable.BitmapImage = bitmap;
                    RebuildRectRoiDrawableList(); // ✅ 统一入口
                });
                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        FourierViewModel.SetFFHome(FFCH.Ch1);
        FourierViewModel.SetFFHome(FFCH.Ch2);
        FourierViewModel.SetFFHome(FFCH.Ch3_X);
        FourierViewModel.SetFFHome(FFCH.Ch3_Y);

        //Cache.HazeWaferPosition = StageViewModel.GetBrightFieldStagePosition();
        Cache.HazeWaferPosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;
        Cache.HazeWaferPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition);

        AfViewModel.ToggleDarkFieldEnable(true);
        SxPos = new Point(Cache.HazeWaferPosition.X, Cache.HazeWaferPosition.Y);

        Cache.CgFFBoxHeightRelationPercentListCh1.Add(12);
        Cache.CgFFBoxHeightRelationPercentListCh1.Add(13);

        Cache.CgFFBoxHeightRelationPercentListCh2.Add(12);
        Cache.CgFFBoxHeightRelationPercentListCh2.Add(13);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HazeWaferPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CgFFBoxBeginNumber1Ch1,
                Cache.CgFFBoxEndNumber1Ch1,
                Cache.CgFFBoxBeginNumber2Ch1,
                Cache.CgFFBoxEndNumber2Ch1
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch1Image != null)
        {
            {
                Cache.CgFFBoxAllRodsBeginPercentCh1 = Ch12Percentage1 * 100;
                // 合并奇数和偶数杆（使用 Percent1 的数据，因为宽度不变）
                var allRods = AllOddRodsCh1Percent1.Concat(AllEvenRodsCh1Percent1)
                    .OrderBy(r => r.Id) // 按杆号升序
                    .ToList();
                Cache.CgFFBoxRodWidthListCh1.Clear();
                foreach (var rod in allRods)
                {
                    Cache.CgFFBoxRodWidthListCh1.Add(rod.Width);
                }

                {
                    // 构建字典：Id -> (Height1, Height2)
                    var oddDict1 = AllOddRodsCh1Percent1.ToDictionary(r => r.Id, r => r.Height);
                    var oddDict2 = AllOddRodsCh1Percent2.ToDictionary(r => r.Id, r => r.Height);
                    var evenDict1 = AllEvenRodsCh1Percent1.ToDictionary(r => r.Id, r => r.Height);
                    var evenDict2 = AllEvenRodsCh1Percent2.ToDictionary(r => r.Id, r => r.Height);

                    // 合并所有 Id（去重 + 排序）
                    var allIds = oddDict1.Keys.Concat(evenDict1.Keys)
                        .OrderBy(id => id)
                        .ToList();

                    Cache.CgFFBoxHeightRelationPercentListCh1.Clear();

                    foreach (int id in allIds)
                    {
                        // 获取 Height1 和 Height2
                        bool hasOdd = oddDict1.ContainsKey(id);
                        bool hasEven = evenDict1.ContainsKey(id);

                        int h1, h2;

                        if (hasOdd)
                        {
                            if (!oddDict2.TryGetValue(id, out h2))
                                throw new InvalidOperationException($"奇数杆 {id} 在 Percent2 中缺失");
                            h1 = oddDict1[id];
                        }
                        else if (hasEven)
                        {
                            if (!evenDict2.TryGetValue(id, out h2))
                                throw new InvalidOperationException($"偶数杆 {id} 在 Percent2 中缺失");
                            h1 = evenDict1[id];
                        }
                        else
                            continue;

                        // 计算变化率
                        double rate = (h2 - h1) / ((Ch12Percentage2 - Ch12Percentage1) / 0.1);
                        Cache.CgFFBoxHeightRelationPercentListCh1.Add(rate * 0.1);
                    }
                }
            }

            ComputeAllRodsCh1();

            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImageCh1", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.CgFFBoxBeginPositionCh1,
                    Cache.CgFFBoxEndPositionCh1,
                    //CgFFBoxRodWidthListCh1 = Cache.CgFFBoxRodWidthListCh1,             
                    //CgFFBoxHeightRelationPercentListCh1 = Cache.CgFFBoxHeightRelationPercentListCh1,
                    CgFFBoxRodWidthListCh1 = Environment.NewLine + string.Join(Environment.NewLine,
                        Cache.CgFFBoxRodWidthListCh1.Select((width, index) => $"Rod{Cache.CgFFBoxBeginNumber2Ch1 + index}: {width}")),
                    CgFFBoxHeightRelationPercentListCh1 = Environment.NewLine + string.Join(Environment.NewLine,
                        Cache.CgFFBoxHeightRelationPercentListCh1.Select((relation, index) => $"Rod{Cache.CgFFBoxBeginNumber2Ch1 + index}: {relation}")),

                    RectOddFirstsCh1 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectOddFirstCh1.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectOddSecondsCh1 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectOddSecondCh1.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectEvenFirstsCh1 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectEvenFirstCh1.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectEvenSecondsCh1 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectEvenSecondCh1.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectAllRodsCh1 = Environment.NewLine + string.Join(Environment.NewLine, Cache.CurrentImageRectListFirstCh1.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(1, 46),
                            (46 - 1) + 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    }))

                    //RectOddSecondsCh1 = string.Join(Environment.NewLine, CurrentImageRectOddSecondCh1),
                    //RectEvenFirstsCh1 = string.Join(Environment.NewLine, CurrentImageRectEvenFirstCh1),
                    //RectEvenSecondsCh1 = string.Join(Environment.NewLine, CurrentImageRectEvenSecondCh1)
                    //OddRodNumbersCh1 = string.Join(Environment.NewLine, Enumerable.Range(Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1), Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList()),
                    //EvenRodNumbersCh1 = string.Join(Environment.NewLine, Enumerable.Range(Math.Min(Cache.CgFFBoxBeginNumber2Ch1, Cache.CgFFBoxEndNumber2Ch1), Math.Abs(Cache.CgFFBoxEndNumber2Ch1 - Cache.CgFFBoxBeginNumber2Ch1) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList())                    
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch1Image__" + "AllRods:", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Ch1ImageFilePath = Cache.OriginImageFilePathList1[0],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch1Image = new HtmlImage(Cache.OriginImageFilePathList1[0], htmlImageOverlays:
                            [
                                .. Cache.CurrentImageRectListFirstCh1
                                    .Skip(Cache.CgFFBoxBeginNumber1Ch1)
                                    .Take(Cache.CgFFBoxEndNumber1Ch1 - Cache.CgFFBoxBeginNumber1Ch1 + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. Cache.CurrentImageRectListFirstCh1
                                    .Index()
                                    .Skip(Cache.CgFFBoxBeginNumber1Ch1)
                                    .Take(Cache.CgFFBoxEndNumber1Ch1 - Cache.CgFFBoxBeginNumber1Ch1 + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        )
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                var OddFirstCh1 = Cache.CgFFBoxBeginNumber2Ch1 % 2 == 1 ? Cache.CgFFBoxBeginNumber2Ch1 : Cache.CgFFBoxBeginNumber2Ch1 + 1;
                var OddLastCh1 = Cache.CgFFBoxEndNumber2Ch1 % 2 == 1 ? Cache.CgFFBoxEndNumber2Ch1 : Cache.CgFFBoxEndNumber2Ch1 - 1;

                var EvenFirstCh1 = Cache.CgFFBoxBeginNumber2Ch1 % 2 == 0 ? Cache.CgFFBoxBeginNumber2Ch1 : Cache.CgFFBoxBeginNumber2Ch1 + 1;
                var EvenLastCh1 = Cache.CgFFBoxEndNumber2Ch1 % 2 == 0 ? Cache.CgFFBoxEndNumber2Ch1 : Cache.CgFFBoxEndNumber2Ch1 - 1;

                for (int j = 0; j < Cache.OriginImageFilePathList1.Count; j++)
                {
                    if (j == 0)
                    {
                        Logger.LogHtmlInformation("Ch1Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch1ImageFilePath = Cache.OriginImageFilePathList1[j],
                            MovePercent = Ch12Percentage1 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch1Image = new HtmlImage(Cache.OriginImageFilePathList1[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectOddFirstCh1
                                            .Skip(OddFirstCh1)
                                            .Take((OddLastCh1 - OddFirstCh1 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectOddFirstCh1
                                            .Index()
                                            .Skip(OddFirstCh1)
                                            .Take((OddLastCh1 - OddFirstCh1 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 1)
                    {
                        Logger.LogHtmlInformation("Ch1Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch1ImageFilePath = Cache.OriginImageFilePathList1[j],
                            MovePercent = Ch12Percentage2 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch1Image = new HtmlImage(Cache.OriginImageFilePathList1[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectOddSecondCh1
                                            .Skip(OddFirstCh1)
                                            .Take((OddLastCh1 - OddFirstCh1 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectOddSecondCh1
                                            .Index()
                                            .Skip(OddFirstCh1)
                                            .Take((OddLastCh1 - OddFirstCh1 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 2)
                    {
                        Logger.LogHtmlInformation("Ch1Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch1ImageFilePath = Cache.OriginImageFilePathList1[j],
                            MovePercent = Ch12Percentage1 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch1Image = new HtmlImage(Cache.OriginImageFilePathList1[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectEvenFirstCh1
                                            .Skip(EvenFirstCh1)
                                            .Take((EvenLastCh1 - EvenFirstCh1 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectEvenFirstCh1
                                            .Index()
                                            .Skip(EvenFirstCh1)
                                            .Take((EvenLastCh1 - EvenFirstCh1 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 3)
                    {
                        Logger.LogHtmlInformation("Ch1Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch1ImageFilePath = Cache.OriginImageFilePathList1[j],
                            MovePercent = Ch12Percentage2 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch1Image = new HtmlImage(Cache.OriginImageFilePathList1[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectEvenSecondCh1
                                            .Skip(EvenFirstCh1)
                                            .Take((EvenLastCh1 - EvenFirstCh1 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectEvenSecondCh1
                                            .Index()
                                            .Skip(EvenFirstCh1)
                                            .Take((EvenLastCh1 - EvenFirstCh1 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                return true;
            });
        }

        DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        return Task.FromResult(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CgFFBoxBeginNumber1Ch2,
                Cache.CgFFBoxEndNumber1Ch2,
                Cache.CgFFBoxBeginNumber2Ch2,
                Cache.CgFFBoxEndNumber2Ch2
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch2Image != null)
        {
            {
                Cache.CgFFBoxAllRodsBeginPercentCh2 = Ch12Percentage1 * 100;
                // 合并奇数和偶数杆（使用 Percent1 的数据，因为宽度不变）
                var allRods = AllOddRodsCh2Percent1.Concat(AllEvenRodsCh2Percent1)
                    .OrderBy(r => r.Id) // 按杆号升序
                    .ToList();
                Cache.CgFFBoxRodWidthListCh2.Clear();
                foreach (var rod in allRods)
                {
                    Cache.CgFFBoxRodWidthListCh2.Add(rod.Width);
                }

                {
                    // 构建字典：Id -> (Height1, Height2)
                    var oddDict1 = AllOddRodsCh2Percent1.ToDictionary(r => r.Id, r => r.Height);
                    var oddDict2 = AllOddRodsCh2Percent2.ToDictionary(r => r.Id, r => r.Height);
                    var evenDict1 = AllEvenRodsCh2Percent1.ToDictionary(r => r.Id, r => r.Height);
                    var evenDict2 = AllEvenRodsCh2Percent2.ToDictionary(r => r.Id, r => r.Height);

                    // 合并所有 Id（去重 + 排序）
                    var allIds = oddDict1.Keys.Concat(evenDict1.Keys)
                        .OrderBy(id => id)
                        .ToList();

                    Cache.CgFFBoxHeightRelationPercentListCh2.Clear();

                    foreach (int id in allIds)
                    {
                        // 获取 Height1 和 Height2
                        bool hasOdd = oddDict1.ContainsKey(id);
                        bool hasEven = evenDict1.ContainsKey(id);

                        int h1, h2;
                        if (hasOdd)
                        {
                            if (!oddDict2.TryGetValue(id, out h2))
                                throw new InvalidOperationException($"奇数杆 {id} 在 Percent2 中缺失");
                            h1 = oddDict1[id];
                        }
                        else if (hasEven)
                        {
                            if (!evenDict2.TryGetValue(id, out h2))
                                throw new InvalidOperationException($"偶数杆 {id} 在 Percent2 中缺失");
                            h1 = evenDict1[id];
                        }
                        else
                            continue;

                        // 计算变化率
                        double rate = (h2 - h1) / ((Ch12Percentage2 - Ch12Percentage1) / 0.1);
                        Cache.CgFFBoxHeightRelationPercentListCh2.Add(rate * 0.1);
                    }
                }
            }

            ComputeAllRodsCh2();

            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImageCh2", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.CgFFBoxBeginPositionCh2,
                    Cache.CgFFBoxEndPositionCh2,
                    //CgFFBoxRodWidthListCh2 = Cache.CgFFBoxRodWidthListCh2,   
                    //CgFFBoxHeightRelationPercentListCh2 = Cache.CgFFBoxHeightRelationPercentListCh2,
                    CgFFBoxRodWidthListCh2 = Environment.NewLine + string.Join(Environment.NewLine,
                        Cache.CgFFBoxRodWidthListCh2.Select((width, index) => $"Rod{Cache.CgFFBoxBeginNumber2Ch2 + index}: {width}")),
                    CgFFBoxHeightRelationPercentListCh2 = Environment.NewLine + string.Join(Environment.NewLine,
                        Cache.CgFFBoxHeightRelationPercentListCh2.Select((relation, index) => $"Rod{Cache.CgFFBoxBeginNumber2Ch2 + index}: {relation}")),

                    RectOddFirstsCh2 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectOddFirstCh2.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectOddSecondsCh2 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectOddSecondCh2.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectEvenFirstsCh2 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectEvenFirstCh2.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectEvenSecondsCh2 = Environment.NewLine + string.Join(Environment.NewLine, CurrentImageRectEvenSecondCh2.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2),
                            Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    })),
                    RectAllRodsCh2 = Environment.NewLine + string.Join(Environment.NewLine, Cache.CurrentImageRectListFirstCh2.Select((rect, i) =>
                    {
                        var oddNums = Enumerable.Range(
                            Math.Min(1, 46),
                            46 - 1 + 1).OrderBy(x => x).ToList();
                        int rodId = i < oddNums.Count ? oddNums[i] : -1;
                        return $"Rod{rodId}: {rect}";
                    }))

                    //RectOddFirstsCh2 = string.Join(Environment.NewLine, CurrentImageRectOddFirstCh2),
                    //RectOddSecondsCh2 = string.Join(Environment.NewLine, CurrentImageRectOddSecondCh2),
                    //RectEvenFirstsCh2 = string.Join(Environment.NewLine, CurrentImageRectEvenFirstCh2),
                    //RectEvenSecondsCh2 = string.Join(Environment.NewLine, CurrentImageRectEvenSecondCh2),
                    //OddRodNumbersCh2 = string.Join(Environment.NewLine, Enumerable.Range(Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2), Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 1).OrderBy(x => x).ToList()),
                    //EvenRodNumbersCh2 = string.Join(Environment.NewLine, Enumerable.Range(Math.Min(Cache.CgFFBoxBeginNumber2Ch2, Cache.CgFFBoxEndNumber2Ch2), Math.Abs(Cache.CgFFBoxEndNumber2Ch2 - Cache.CgFFBoxBeginNumber2Ch2) + 1).Where(x => x % 2 == 0).OrderBy(x => x).ToList())               
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch2Image__" + "AllRods:", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Ch2ImageFilePath = Cache.OriginImageFilePathList2[0],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch2Image = new HtmlImage(Cache.OriginImageFilePathList2[0], htmlImageOverlays:
                            [
                                ..Cache.CurrentImageRectListFirstCh2
                                    .Skip(Cache.CgFFBoxBeginNumber1Ch2)
                                    .Take(Cache.CgFFBoxEndNumber1Ch2 - Cache.CgFFBoxBeginNumber1Ch2 + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. Cache.CurrentImageRectListFirstCh2
                                    .Index()
                                    .Skip(Cache.CgFFBoxBeginNumber1Ch2)
                                    .Take(Cache.CgFFBoxEndNumber1Ch2 - Cache.CgFFBoxBeginNumber1Ch2 + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        )
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                var OddFirstCh2 = Cache.CgFFBoxBeginNumber2Ch2 % 2 == 1 ? Cache.CgFFBoxBeginNumber2Ch2 : Cache.CgFFBoxBeginNumber2Ch2 + 1;
                var OddLastCh2 = Cache.CgFFBoxEndNumber2Ch2 % 2 == 1 ? Cache.CgFFBoxEndNumber2Ch2 : Cache.CgFFBoxEndNumber2Ch2 - 1;

                var EvenFirstCh2 = Cache.CgFFBoxBeginNumber2Ch2 % 2 == 0 ? Cache.CgFFBoxBeginNumber2Ch2 : Cache.CgFFBoxBeginNumber2Ch2 + 1;
                var EvenLastCh2 = Cache.CgFFBoxEndNumber2Ch2 % 2 == 0 ? Cache.CgFFBoxEndNumber2Ch2 : Cache.CgFFBoxEndNumber2Ch2 - 1;

                for (int j = 0; j < Cache.OriginImageFilePathList2.Count; j++)
                {
                    if (j == 0)
                    {
                        Logger.LogHtmlInformation("Ch2Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch2ImageFilePath = Cache.OriginImageFilePathList2[j],
                            MovePercent = Ch12Percentage1 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch2Image = new HtmlImage(Cache.OriginImageFilePathList2[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectOddFirstCh2
                                            .Skip(OddFirstCh2)
                                            .Take((OddLastCh2 - OddFirstCh2 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectOddFirstCh2
                                            .Index()
                                            .Skip(OddFirstCh2)
                                            .Take((OddLastCh2 - OddFirstCh2 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 1)
                    {
                        Logger.LogHtmlInformation("Ch2Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch2ImageFilePath = Cache.OriginImageFilePathList2[j],
                            MovePercent = Ch12Percentage2 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch2Image = new HtmlImage(Cache.OriginImageFilePathList2[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectOddSecondCh2
                                            .Skip(OddFirstCh2)
                                            .Take((OddLastCh2 - OddFirstCh2 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectOddSecondCh2
                                            .Index()
                                            .Skip(OddFirstCh2)
                                            .Take((OddLastCh2 - OddFirstCh2 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 2)
                    {
                        Logger.LogHtmlInformation("Ch2Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch2ImageFilePath = Cache.OriginImageFilePathList2[j],
                            MovePercent = Ch12Percentage1 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch2Image = new HtmlImage(Cache.OriginImageFilePathList2[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectEvenFirstCh2
                                            .Skip(EvenFirstCh2)
                                            .Take((EvenLastCh2 - EvenFirstCh2 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectEvenFirstCh2
                                            .Index()
                                            .Skip(EvenFirstCh2)
                                            .Take((EvenLastCh2 - EvenFirstCh2 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    if (j == 3)
                    {
                        Logger.LogHtmlInformation("Ch2Image:" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            Ch2ImageFilePath = Cache.OriginImageFilePathList2[j],
                            MovePercent = Ch12Percentage2 * 100 + "%",
                            HtmlTab = new HtmlTab(new
                            {
                                Ch2Image = new HtmlImage(Cache.OriginImageFilePathList2[j], htmlImageOverlays:
                                    [
                                        .. CurrentImageRectEvenSecondCh2
                                            .Skip(EvenFirstCh2)
                                            .Take((EvenLastCh2 - EvenFirstCh2 + 1) / 2).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                        .. CurrentImageRectEvenSecondCh2
                                            .Index()
                                            .Skip(EvenFirstCh2)
                                            .Take((EvenLastCh2 - EvenFirstCh2 + 1) / 2).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                                    ]
                                )
                            })
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                return true;
            });
        }

        DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        return Task.FromResult(false);
    }

    // 关键：当第一个矩形被拖动时，更新 XStartPixel（但不重建！）
    private void OnFirstRectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RectROIDrawable.Rect) && sender is RectROIDrawable firstRect && Cache.BitmapImageDrawableCh1.BitmapImage != null)
        {
            var imageRect = Cache.BitmapImageDrawableCh1.CartesianCoordinateToImageCoordinate(firstRect.Rect);
            var newStartX = (int)Math.Round(imageRect.X);

            if (newStartX != XStartPixel)
            {
                IsSyncingFromDrag = true;
                try
                {
                    XStartPixel = newStartX; // 这会触发 UI 更新，但不会重建（见下）
                }
                finally
                {
                    IsSyncingFromDrag = false;
                }
            }
        }
    }

    // 阻止因 XStartPixel 变化而重建（仅当非拖动时才重建）
    partial void OnXStartPixelChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        if (!IsSyncingFromDrag)
        {
            RebuildRectRoiDrawableList();
        }
    }

    // 在 RebuildRectROIDrawableList() 创建新列表后，订阅第一个矩形
    private void RebuildRectRoiDrawableList()
    {
        if (Cache.BitmapImageDrawableCh1.BitmapImage is null)
        {
            // 取消旧订阅（可选）
            if (Cache.RectROIDrawableList.FirstOrDefault() is { } previousFirst)
                previousFirst.PropertyChanged -= OnFirstRectPropertyChanged;
            return;
        }

        if (SelectedTabIndex == 0)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            for (int i = RodBegin; i <= RodEnd; i += 2)
            {
                var imageRect = new Rect(new Point(XStartPixel + i * RodWidth, 0), new Size(RodWidth, RodHight));
                var cartesianRect = Cache.BitmapImageDrawableCh1.ImageCoordinateToCartesianCoordinate(imageRect);

                newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = i.ToString() });
            }

            // 取消旧订阅
            if (Cache.RectROIDrawableList.FirstOrDefault() is { } oldFirst)
                oldFirst.PropertyChanged -= OnFirstRectPropertyChanged;

            Cache.RectROIDrawableList = newList;
        }

        if (SelectedTabIndex == 1)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            for (int i = RodBegin; i <= RodEnd; i += 2)
            {
                var imageRect = new Rect(new Point(XStartPixel + i * RodWidth, 0), new Size(RodWidth, RodHight));
                var cartesianRect = Cache.BitmapImageDrawableCh2.ImageCoordinateToCartesianCoordinate(imageRect);

                newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = i.ToString() });
            }

            // 取消旧订阅
            if (Cache.RectROIDrawableList.FirstOrDefault() is { } oldFirst)
                oldFirst.PropertyChanged -= OnFirstRectPropertyChanged;

            Cache.RectROIDrawableList = newList;
        }

        // 订阅新的第一个矩形
        if (Cache.RectROIDrawableList.FirstOrDefault() is { } newFirst)
            newFirst.PropertyChanged += OnFirstRectPropertyChanged;
    }

    [RelayCommand]
    private async Task GetImageOddFirstCh1Async()
    {
        await Task.Run(() =>
        {
            AllOddRodsCh1Percent1 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 1)
                    AllOddRodsCh1Percent1 = [.. AllOddRodsCh1Percent1, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch1 % 2 == 1)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1 + 1;

            if (Cache.CgFFBoxEndNumber2Ch1 % 2 == 1)
                RodEnd = Cache.CgFFBoxEndNumber2Ch1;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch1 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 1)
                    ch12List[j - 1] = (j, Ch12Percentage1);
            }

            if (SelectedTabIndex == 0)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                        Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage1 + "__" + "OddFirst" + Cache.CgFFBoxBeginNumber2Ch1 + "__" + Cache.CgFFBoxEndNumber2Ch1 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch1Image = Cache.BitmapImageDrawableCh1.BitmapImage;
                        Cache.BitmapImageDrawableCh1.BitmapImage.Save(Cache.OriginImageFilePath1);
                        Cache.OriginImageFilePathList1[0] = Cache.OriginImageFilePath1;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageEvenFirstCh1Async()
    {
        await Task.Run(() =>
        {
            AllEvenRodsCh1Percent1 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 0)
                    AllEvenRodsCh1Percent1 = [.. AllEvenRodsCh1Percent1, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch1 % 2 == 0)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1 + 1;

            if (Cache.CgFFBoxEndNumber2Ch1 % 2 == 0)
                RodEnd = Cache.CgFFBoxEndNumber2Ch1;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch1 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 0)
                    ch12List[j - 1] = (j, Ch12Percentage1);
            }

            if (SelectedTabIndex == 0)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                        Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage1 + "__" + "EvenFirst" + Cache.CgFFBoxBeginNumber2Ch1 + "__" + Cache.CgFFBoxEndNumber2Ch1 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch1Image = Cache.BitmapImageDrawableCh1.BitmapImage;
                        Cache.BitmapImageDrawableCh1.BitmapImage.Save(Cache.OriginImageFilePath1);
                        Cache.OriginImageFilePathList1[2] = Cache.OriginImageFilePath1;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageOddSecondCh1Async()
    {
        await Task.Run(() =>
        {
            AllOddRodsCh1Percent2 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 1)
                    AllOddRodsCh1Percent2 = [.. AllOddRodsCh1Percent2, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch1 % 2 == 1)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1 + 1;

            if (Cache.CgFFBoxEndNumber2Ch1 % 2 == 1)
                RodEnd = Cache.CgFFBoxEndNumber2Ch1;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch1 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 1)
                    ch12List[j - 1] = (j, Ch12Percentage2);
            }

            if (SelectedTabIndex == 0)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                        Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage2 + "__" + "OddSecond" + Cache.CgFFBoxBeginNumber2Ch1 + "__" + Cache.CgFFBoxEndNumber2Ch1 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch1Image = Cache.BitmapImageDrawableCh1.BitmapImage;
                        Cache.BitmapImageDrawableCh1.BitmapImage.Save(Cache.OriginImageFilePath1);
                        Cache.OriginImageFilePathList1[1] = Cache.OriginImageFilePath1;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageEvenSecondCh1Async()
    {
        await Task.Run(() =>
        {
            AllEvenRodsCh1Percent2 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 0)
                    AllEvenRodsCh1Percent2 = [.. AllEvenRodsCh1Percent2, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch1 % 2 == 0)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch1 + 1;

            if (Cache.CgFFBoxEndNumber2Ch1 % 2 == 0)
                RodEnd = Cache.CgFFBoxEndNumber2Ch1;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch1 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= Cache.CgFFBoxEndNumber2Ch1; j++)
            {
                if (j % 2 == 0)
                    ch12List[j - 1] = (j, Ch12Percentage2);
            }

            if (SelectedTabIndex == 0)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                        Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage2 + "__" + "EvenSecond" + Cache.CgFFBoxBeginNumber2Ch1 + "__" + Cache.CgFFBoxEndNumber2Ch1 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch1Image = Cache.BitmapImageDrawableCh1.BitmapImage;
                        Cache.BitmapImageDrawableCh1.BitmapImage.Save(Cache.OriginImageFilePath1);
                        Cache.OriginImageFilePathList1[3] = Cache.OriginImageFilePath1;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageOddFirstCh2Async()
    {
        await Task.Run(() =>
        {
            AllOddRodsCh2Percent1 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 1)
                    AllOddRodsCh2Percent1 = [.. AllOddRodsCh2Percent1, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch2 % 2 == 1)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2 + 1;

            if (Cache.CgFFBoxEndNumber2Ch2 % 2 == 1)
                RodEnd = Cache.CgFFBoxEndNumber2Ch2;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch2 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 1)
                    ch12List[j - 1] = (j, Ch12Percentage1);
            }

            if (SelectedTabIndex == 1)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                        Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage1 + "__" + "OddFirst" + Cache.CgFFBoxBeginNumber2Ch2 + "__" + Cache.CgFFBoxEndNumber2Ch2 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch2Image = Cache.BitmapImageDrawableCh2.BitmapImage;
                        Cache.BitmapImageDrawableCh2.BitmapImage.Save(Cache.OriginImageFilePath2);
                        Cache.OriginImageFilePathList2[0] = Cache.OriginImageFilePath2;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageEvenFirstCh2Async()
    {
        await Task.Run(() =>
        {
            AllEvenRodsCh2Percent1 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 0)
                    AllEvenRodsCh2Percent1 = [.. AllEvenRodsCh2Percent1, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch2 % 2 == 0)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2 + 1;

            if (Cache.CgFFBoxEndNumber2Ch2 % 2 == 0)
                RodEnd = Cache.CgFFBoxEndNumber2Ch2;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch2 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 0)
                    ch12List[j - 1] = (j, Ch12Percentage1);
            }

            if (SelectedTabIndex == 1)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                        Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage1 + "__" + "EvenFirst" + Cache.CgFFBoxBeginNumber2Ch2 + "__" + Cache.CgFFBoxEndNumber2Ch2 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch2Image = Cache.BitmapImageDrawableCh2.BitmapImage;
                        Cache.BitmapImageDrawableCh2.BitmapImage.Save(Cache.OriginImageFilePath2);
                        Cache.OriginImageFilePathList2[2] = Cache.OriginImageFilePath2;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageOddSecondCh2Async()
    {
        await Task.Run(() =>
        {
            AllOddRodsCh2Percent2 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 1)
                    AllOddRodsCh2Percent2 = [.. AllOddRodsCh2Percent2, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch2 % 2 == 1)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2 + 1;

            if (Cache.CgFFBoxEndNumber2Ch2 % 2 == 1)
                RodEnd = Cache.CgFFBoxEndNumber2Ch2;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch2 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 1)
                    ch12List[j - 1] = (j, Ch12Percentage2);
            }

            if (SelectedTabIndex == 1)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                        Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage2 + "__" + "OddSecond" + Cache.CgFFBoxBeginNumber2Ch2 + "__" + Cache.CgFFBoxEndNumber2Ch2 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch2Image = Cache.BitmapImageDrawableCh2.BitmapImage;
                        Cache.BitmapImageDrawableCh2.BitmapImage.Save(Cache.OriginImageFilePath2);
                        Cache.OriginImageFilePathList2[1] = Cache.OriginImageFilePath2;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageEvenSecondCh2Async()
    {
        await Task.Run(() =>
        {
            AllEvenRodsCh2Percent2 = [];
            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 0)
                    AllEvenRodsCh2Percent2 = [.. AllEvenRodsCh2Percent2, new Pole { Id = j }];
            }

            var ch12List = new List<(int rodnumber, double rodpos)>();
            var rodNum = 46;

            if (Cache.CgFFBoxBeginNumber2Ch2 % 2 == 0)
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2;
            else
                RodBegin = Cache.CgFFBoxBeginNumber2Ch2 + 1;

            if (Cache.CgFFBoxEndNumber2Ch2 % 2 == 0)
                RodEnd = Cache.CgFFBoxEndNumber2Ch2;
            else
                RodEnd = Cache.CgFFBoxEndNumber2Ch2 - 1;

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= Cache.CgFFBoxEndNumber2Ch2; j++)
            {
                if (j % 2 == 0)
                    ch12List[j - 1] = (j, Ch12Percentage2);
            }

            if (SelectedTabIndex == 1)
            {
                var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                if (result)
                {
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                    {
                        using var bitmap = BytesToBitmapImage(ret);

                        var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                        Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();

                        Cache.OriginImageFilePath2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage2 + "__" + "EvenSecond" + Cache.CgFFBoxBeginNumber2Ch2 + "__" + Cache.CgFFBoxEndNumber2Ch2 + "__" + $"{Guid.NewGuid():N}.jpg");
                        Cache.Ch2Image = Cache.BitmapImageDrawableCh2.BitmapImage;
                        Cache.BitmapImageDrawableCh2.BitmapImage.Save(Cache.OriginImageFilePath2);
                        Cache.OriginImageFilePathList2[3] = Cache.OriginImageFilePath2;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh1BeginNumber1Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber1Ch1; j <= rodNum; j++)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j - 1; k >= 1; k--)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                            Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is begin number? No: We will move the next rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxBeginNumber1Ch1 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh1EndNumber1Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxEndNumber1Ch1; j >= 1; j--)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j + 1; k <= rodNum; k++)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                            Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is end number? No: We will move the previous rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxEndNumber1Ch1 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh1BeginNumber2Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch1; j <= rodNum; j++)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j - 1; k >= 1; k--)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                            Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is begin number? No: We will move the next rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxBeginNumber2Ch1 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh1EndNumber2Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxEndNumber2Ch1; j >= 1; j--)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j + 1; k <= rodNum; k++)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                            Cache.BitmapImageDrawableCh1.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is end number? No: We will move the previous rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxEndNumber2Ch1 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh2BeginNumber1Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber1Ch2; j <= rodNum; j++)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j - 1; k >= 1; k--)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                            Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is begin number? No: We will move the next rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxBeginNumber1Ch2 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh2EndNumber1Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxEndNumber1Ch2; j >= 1; j--)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j + 1; k <= rodNum; k++)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                            Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is end number? No: We will move the previous rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxEndNumber1Ch2 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh2BeginNumber2Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxBeginNumber2Ch2; j <= rodNum; j++)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j - 1; k >= 1; k--)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                            Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is begin number? No: We will move the next rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxBeginNumber2Ch2 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetCh2EndNumber2Async()
    {
        await Task.Run(() =>
        {
            InitRodState();
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            {
                var result = FourierViewModel.GetFourierConfig();
                rodNum = result.RodNum;
            }

            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0.0));
            }

            for (int j = Cache.CgFFBoxEndNumber2Ch2; j >= 1; j--)
            {
                ch12List[j - 1] = (j, 0.99);
                for (int k = j + 1; k <= rodNum; k++)
                {
                    ch12List[k - 1] = (k, 0.0);
                }

                {
                    var result = FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    if (result)
                    {
                        //await Task.Delay(2000).ConfigureAwait(false);
                        var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
                        {
                            using var bitmap = BytesToBitmapImage(ret);

                            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                            Cache.BitmapImageDrawableCh2.BitmapImage = croppedImage.ToBitmapImage();
                        }
                    }
                }

                DialogWindowProvider.TryShowDialog("Yes: This rod is end number? No: We will move the previous rod", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                if (dialogResult == DialogResultEnum.Yes)
                {
                    Cache.CgFFBoxEndNumber2Ch2 = j;
                    break;
                }
            }
        }).ConfigureAwait(false);
    }

    private HImage GetPictureRegion(HImage originImage, int startX, int startY, int width, int height)
    {
        // 3. 【关键】使用 CropPart 进行真实裁剪 // 参数: 原图, 起始列(Column), 起始行(Row), 宽度, 高度     
        using var hImage = originImage;
        using var cropHImage = hImage.CropPart(startY, startX, width, height);
        return cropHImage;
    }

    private void Save(PupilSideChannelFlexibleApertureDTO itemDto, CancellationToken cancellationToken)
    {
        InvokeSave(update =>
        {
            update(itemDto);
            update(Cache);

            Calibration = itemDto.Clone();

            ApplicationCookieService.SetCalibration(Calibration, cancellationToken);
            ApplicationCookieService.SetCache(Cache, cancellationToken);
        });
    }

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<PupilSideChannelFlexibleApertureDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    public static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        if (bytes.Length == 0)
            return null!;
        // Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage
        // 使用接受字节数组的构造函数（反编译源码显示有此构造函数）
        return new BitmapImage(bytes, isCopy: true);
    }

    // 3. 解析方法：从字符串提取所有 double

    private void InitRodState()
    {
        RodBegin = 1;
        RodEnd = 0;
    }

    [RelayCommand]
    private void RefreshOddFirstCh1()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllOddRodsCh1Percent1[j].Width = (int)rect.Width;
            AllOddRodsCh1Percent1[j].Height = (int)rect.Height;

            CurrentImageRectOddFirstCh1.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshOddSecondCh1()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllOddRodsCh1Percent2[j].Width = (int)rect.Width;
            AllOddRodsCh1Percent2[j].Height = (int)rect.Height;

            CurrentImageRectOddSecondCh1.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshEvenFirstCh1()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllEvenRodsCh1Percent1[j].Width = (int)rect.Width;
            AllEvenRodsCh1Percent1[j].Height = (int)rect.Height;

            CurrentImageRectEvenFirstCh1.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshEvenSecondCh1()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllEvenRodsCh1Percent2[j].Width = (int)rect.Width;
            AllEvenRodsCh1Percent2[j].Height = (int)rect.Height;

            CurrentImageRectEvenSecondCh1.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshOddFirstCh2()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllOddRodsCh2Percent1[j].Width = (int)rect.Width;
            AllOddRodsCh2Percent1[j].Height = (int)rect.Height;

            CurrentImageRectOddFirstCh2.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshOddSecondCh2()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllOddRodsCh2Percent2[j].Width = (int)rect.Width;
            AllOddRodsCh2Percent2[j].Height = (int)rect.Height;

            CurrentImageRectOddSecondCh2.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshEvenFirstCh2()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllEvenRodsCh2Percent1[j].Width = (int)rect.Width;
            AllEvenRodsCh2Percent1[j].Height = (int)rect.Height;

            CurrentImageRectEvenFirstCh2.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    [RelayCommand]
    private void RefreshEvenSecondCh2()
    {
        CurrentImageAxis.Clear();
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var rectOne = Cache.BitmapImageDrawableCh2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            CurrentImageAxis.Add(rectOne);
        }

        for (int j = 0; j < CurrentImageAxis.Count; j++)
        {
            var rect = CurrentImageAxis[j];
            AllEvenRodsCh2Percent2[j].Width = (int)rect.Width;
            AllEvenRodsCh2Percent2[j].Height = (int)rect.Height;

            CurrentImageRectEvenSecondCh2.Add(rect);
        }

        DialogWindowProvider.ShowDialog("Set Data Success!");
    }

    private void ComputeAllRodsCh1()
    {
        // ===== 1. 计算奇数平均值 =====
        double avgOddS = CurrentImageRectOddFirstCh1.Any() ? CurrentImageRectOddFirstCh1.Zip(CurrentImageRectOddFirstCh1.Skip(1), (r1, r2) => Math.Abs(r2.X - r1.X)).Average() : 20.0;
        double avgOddW = CurrentImageRectOddFirstCh1.Any() ? CurrentImageRectOddFirstCh1.Average(r => r.Width) : 80.0;
        double avgOddH = CurrentImageRectOddFirstCh1.Any() ? CurrentImageRectOddFirstCh1.Average(r => r.Height) : 80.0;
        double avgOddY = CurrentImageRectOddFirstCh1.Any() ? CurrentImageRectOddFirstCh1.Average(r => r.Y) : 100.0;

        // ===== 2. 计算偶数平均值 =====
        double avgEvenS = CurrentImageRectEvenFirstCh1.Any() ? CurrentImageRectEvenFirstCh1.Zip(CurrentImageRectEvenFirstCh1.Skip(1), (r1, r2) => Math.Abs(r2.X - r1.X)).Average() : 20.0;
        double avgEvenW = CurrentImageRectEvenFirstCh1.Any() ? CurrentImageRectEvenFirstCh1.Average(r => r.Width) : 80.0;
        double avgEvenH = CurrentImageRectEvenFirstCh1.Any() ? CurrentImageRectEvenFirstCh1.Average(r => r.Height) : 80.0;
        double avgEvenY = CurrentImageRectEvenFirstCh1.Any() ? CurrentImageRectEvenFirstCh1.Average(r => r.Y) : 200.0;

        // ===== 3. 获取 [begin, end] 范围内的奇/偶编号 =====
        int begin = Cache.CgFFBoxBeginNumber2Ch1;
        int end = Cache.CgFFBoxEndNumber2Ch1;

        var oddNumbersInRange = Enumerable.Range(begin, end - begin + 1)
            .Where(x => x % 2 == 1)
            .OrderBy(x => x)
            .ToList();
        var evenNumbersInRange = Enumerable.Range(begin, end - begin + 1)
            .Where(x => x % 2 == 0)
            .OrderBy(x => x)
            .ToList();

        // ===== 4. 构建真实数据映射（用于保留原始 X/Y/W/H）=====
        var realRectMap = new Dictionary<int, Rect>();
        for (int i = 0; i < Math.Min(oddNumbersInRange.Count, CurrentImageRectOddFirstCh1.Count); i++)
            realRectMap[oddNumbersInRange[i]] = CurrentImageRectOddFirstCh1[i];
        for (int i = 0; i < Math.Min(evenNumbersInRange.Count, CurrentImageRectEvenFirstCh1.Count); i++)
            realRectMap[evenNumbersInRange[i]] = CurrentImageRectEvenFirstCh1[i];

        // ===== 5. 生成 1~46 的奇数、偶数完整列表 =====
        var allOddIds = Enumerable.Range(1, 46).Where(x => x % 2 == 1).ToList(); // [1,3,...,45] 共23个
        var allEvenIds = Enumerable.Range(1, 46).Where(x => x % 2 == 0).ToList(); // [2,4,...,46] 共23个

        // ===== 6. 为奇数生成完整 X 坐标 =====
        var oddXList = new double[allOddIds.Count];
        bool hasOddData = oddNumbersInRange.Count > 0 && CurrentImageRectOddFirstCh1.Count > 0;

        if (hasOddData)
        {
            // 找到第一个已有奇数在 allOddIds 中的位置
            int firstKnownIndex = allOddIds.IndexOf(oddNumbersInRange.First());
            int lastKnownIndex = allOddIds.IndexOf(oddNumbersInRange.Last());

            // 填充已知部分（保留原始 X）
            for (int i = 0; i < oddNumbersInRange.Count && i < CurrentImageRectOddFirstCh1.Count; i++)
            {
                int globalIndex = allOddIds.IndexOf(oddNumbersInRange[i]);
                if (globalIndex >= 0)
                    oddXList[globalIndex] = CurrentImageRectOddFirstCh1[i].X;
            }

            // 向左填充（编号更小的奇数）
            for (int i = firstKnownIndex - 1; i >= 0; i--)
            {
                oddXList[i] = oddXList[i + 1] - (avgOddS + 5);
            }

            // 向右填充（编号更大的奇数）
            for (int i = lastKnownIndex + 1; i < allOddIds.Count; i++)
            {
                oddXList[i] = oddXList[i - 1] + (avgOddS + 5);
            }
        }
        else
        {
            // 无任何奇数数据：从 X=0 开始排
            for (int i = 0; i < allOddIds.Count; i++)
                oddXList[i] = i * (avgOddS + 5);
        }

        // ===== 7. 为偶数生成完整 X 坐标（同理）=====
        var evenXList = new double[allEvenIds.Count];
        bool hasEvenData = evenNumbersInRange.Count > 0 && CurrentImageRectEvenFirstCh1.Count > 0;

        if (hasEvenData)
        {
            int firstKnownIndex = allEvenIds.IndexOf(evenNumbersInRange.First());
            int lastKnownIndex = allEvenIds.IndexOf(evenNumbersInRange.Last());

            for (int i = 0; i < evenNumbersInRange.Count && i < CurrentImageRectEvenFirstCh1.Count; i++)
            {
                int globalIndex = allEvenIds.IndexOf(evenNumbersInRange[i]);
                if (globalIndex >= 0)
                    evenXList[globalIndex] = CurrentImageRectEvenFirstCh1[i].X;
            }

            for (int i = firstKnownIndex - 1; i >= 0; i--)
                evenXList[i] = evenXList[i + 1] - (avgEvenS + 5);

            for (int i = lastKnownIndex + 1; i < allEvenIds.Count; i++)
                evenXList[i] = evenXList[i - 1] + (avgEvenS + 5);
        }
        else
        {
            for (int i = 0; i < allEvenIds.Count; i++)
                evenXList[i] = i * (avgEvenS + 5);
        }

        // ===== 8. 构造 1~46 完整 Rect 列表 =====
        var fullList = new List<Rect>();

        foreach (int id in Enumerable.Range(1, 46))
        {
            if (realRectMap.TryGetValue(id, out var realRect))
            {
                fullList.Add(realRect); // 保留原始完整 Rect（含真实 X/Y/W/H）
            }
            else
            {
                if (id % 2 == 1)
                {
                    int idx = allOddIds.IndexOf(id);
                    double x = idx >= 0 ? oddXList[idx] : 0;
                    fullList.Add(new Rect(x, avgOddY, avgOddW, avgOddH));
                }
                else
                {
                    int idx = allEvenIds.IndexOf(id);
                    double x = idx >= 0 ? evenXList[idx] : 0;
                    fullList.Add(new Rect(x, avgEvenY, avgEvenW, avgEvenH));
                }
            }
        }

        // ===== 9. 转为 ObservableCollection =====
        Cache.CurrentImageRectListFirstCh1 = new ObservableCollection<Rect>(fullList);
    }

    private void ComputeAllRodsCh2()
    {
        // ===== 1. 计算奇数平均值 =====
        double avgOddS = CurrentImageRectOddFirstCh2.Any() ? CurrentImageRectOddFirstCh2.Zip(CurrentImageRectOddFirstCh2.Skip(1), (r1, r2) => Math.Abs(r2.X - r1.X)).Average() : 20.0;
        double avgOddW = CurrentImageRectOddFirstCh2.Any() ? CurrentImageRectOddFirstCh2.Average(r => r.Width) : 80.0;
        double avgOddH = CurrentImageRectOddFirstCh2.Any() ? CurrentImageRectOddFirstCh2.Average(r => r.Height) : 80.0;
        double avgOddY = CurrentImageRectOddFirstCh2.Any() ? CurrentImageRectOddFirstCh2.Average(r => r.Y) : 100.0;

        // ===== 2. 计算偶数平均值 =====
        double avgEvenS = CurrentImageRectEvenFirstCh2.Any() ? CurrentImageRectEvenFirstCh2.Zip(CurrentImageRectEvenFirstCh2.Skip(1), (r1, r2) => Math.Abs(r2.X - r1.X)).Average() : 20.0;
        double avgEvenW = CurrentImageRectEvenFirstCh2.Any() ? CurrentImageRectEvenFirstCh2.Average(r => r.Width) : 80.0;
        double avgEvenH = CurrentImageRectEvenFirstCh2.Any() ? CurrentImageRectEvenFirstCh2.Average(r => r.Height) : 80.0;
        double avgEvenY = CurrentImageRectEvenFirstCh2.Any() ? CurrentImageRectEvenFirstCh2.Average(r => r.Y) : 200.0;

        // ===== 3. 获取 [begin, end] 范围内的奇/偶编号 =====
        int begin = Cache.CgFFBoxBeginNumber2Ch2;
        int end = Cache.CgFFBoxEndNumber2Ch2;
        var oddNumbersInRange = Enumerable.Range(begin, end - begin + 1)
            .Where(x => x % 2 == 1)
            .OrderBy(x => x)
            .ToList();
        var evenNumbersInRange = Enumerable.Range(begin, end - begin + 1)
            .Where(x => x % 2 == 0)
            .OrderBy(x => x)
            .ToList();

        // ===== 4. 构建真实数据映射（用于保留原始 X/Y/W/H）=====
        var realRectMap = new Dictionary<int, Rect>();
        for (int i = 0; i < Math.Min(oddNumbersInRange.Count, CurrentImageRectOddFirstCh2.Count); i++)
            realRectMap[oddNumbersInRange[i]] = CurrentImageRectOddFirstCh2[i];
        for (int i = 0; i < Math.Min(evenNumbersInRange.Count, CurrentImageRectEvenFirstCh2.Count); i++)
            realRectMap[evenNumbersInRange[i]] = CurrentImageRectEvenFirstCh2[i];

        // ===== 5. 生成 1~46 的奇数、偶数完整列表 =====
        var allOddIds = Enumerable.Range(1, 46).Where(x => x % 2 == 1).ToList(); // [1,3,...,45] 共23个
        var allEvenIds = Enumerable.Range(1, 46).Where(x => x % 2 == 0).ToList(); // [2,4,...,46] 共23个

        // ===== 6. 为奇数生成完整 X 坐标 =====
        var oddXList = new double[allOddIds.Count];
        bool hasOddData = oddNumbersInRange.Count > 0 && CurrentImageRectOddFirstCh2.Count > 0;

        if (hasOddData)
        {
            // 找到第一个已有奇数在 allOddIds 中的位置
            int firstKnownIndex = allOddIds.IndexOf(oddNumbersInRange.First());
            int lastKnownIndex = allOddIds.IndexOf(oddNumbersInRange.Last());

            // 填充已知部分（保留原始 X）
            for (int i = 0; i < oddNumbersInRange.Count && i < CurrentImageRectOddFirstCh2.Count; i++)
            {
                int globalIndex = allOddIds.IndexOf(oddNumbersInRange[i]);
                if (globalIndex >= 0)
                    oddXList[globalIndex] = CurrentImageRectOddFirstCh2[i].X;
            }

            // 向左填充（编号更小的奇数）
            for (int i = firstKnownIndex - 1; i >= 0; i--)
            {
                oddXList[i] = oddXList[i + 1] - (avgOddS + 5);
            }

            // 向右填充（编号更大的奇数）
            for (int i = lastKnownIndex + 1; i < allOddIds.Count; i++)
            {
                oddXList[i] = oddXList[i - 1] + (avgOddS + 5);
            }
        }
        else
        {
            // 无任何奇数数据：从 X=0 开始排
            for (int i = 0; i < allOddIds.Count; i++)
                oddXList[i] = i * (avgOddS + 5);
        }

        // ===== 7. 为偶数生成完整 X 坐标（同理）=====
        var evenXList = new double[allEvenIds.Count];
        bool hasEvenData = evenNumbersInRange.Count > 0 && CurrentImageRectEvenFirstCh2.Count > 0;

        if (hasEvenData)
        {
            int firstKnownIndex = allEvenIds.IndexOf(evenNumbersInRange.First());
            int lastKnownIndex = allEvenIds.IndexOf(evenNumbersInRange.Last());

            for (int i = 0; i < evenNumbersInRange.Count && i < CurrentImageRectEvenFirstCh2.Count; i++)
            {
                int globalIndex = allEvenIds.IndexOf(evenNumbersInRange[i]);
                if (globalIndex >= 0)
                    evenXList[globalIndex] = CurrentImageRectEvenFirstCh2[i].X;
            }

            for (int i = firstKnownIndex - 1; i >= 0; i--)
                evenXList[i] = evenXList[i + 1] - (avgEvenS + 5);

            for (int i = lastKnownIndex + 1; i < allEvenIds.Count; i++)
                evenXList[i] = evenXList[i - 1] + (avgEvenS + 5);
        }
        else
        {
            for (int i = 0; i < allEvenIds.Count; i++)
                evenXList[i] = i * (avgEvenS + 5);
        }

        // ===== 8. 构造 1~46 完整 Rect 列表 =====
        var fullList = new List<Rect>();

        foreach (int id in Enumerable.Range(1, 46))
        {
            if (realRectMap.TryGetValue(id, out var realRect))
            {
                fullList.Add(realRect); // 保留原始完整 Rect（含真实 X/Y/W/H）
            }
            else
            {
                if (id % 2 == 1)
                {
                    int idx = allOddIds.IndexOf(id);
                    double x = idx >= 0 ? oddXList[idx] : 0;
                    fullList.Add(new Rect(x, avgOddY, avgOddW, avgOddH));
                }
                else
                {
                    int idx = allEvenIds.IndexOf(id);
                    double x = idx >= 0 ? evenXList[idx] : 0;
                    fullList.Add(new Rect(x, avgEvenY, avgEvenW, avgEvenH));
                }
            }
        }

        // ===== 9. 转为 ObservableCollection =====
        Cache.CurrentImageRectListFirstCh2 = new ObservableCollection<Rect>(fullList);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            Review = Calibration.Clone();

            Logger.LogHtmlInformation("Verify:OK", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("No need param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog("Verify Success!");
            Review.IsVerified = true;
            Save(Review, cancellationToken);
            return true;
        }).ConfigureAwait(false);
    }
}

public partial class RodInformation : ObservableObject
{
    // 添加无参构造函数
    public RodInformation()
    {
        Number = "Rod1";
        Percent = 0;
    }

    public RodInformation(string number, double percent)
    {
        Number = number;
        Percent = percent;
    }

    [ObservableProperty]
    private string _number = string.Empty;

    [ObservableProperty]
    private double _percent;
};

// Pole.cs
public partial class Pole : ObservableObject
{
    [ObservableProperty]
    private int _id = 1;

    [ObservableProperty]
    private int _width = 20;

    [ObservableProperty]
    private int _height = 80;
}