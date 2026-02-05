using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis;

[IOCAppService(ServiceType = typeof(AdsXYMoveDiagnosisWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsXYMoveDiagnosisWindowViewModel(
    StageViewModel stageViewModel,
    AdsViewModel adsViewModel,
    IHostEnvironment hostEnvironment,
    ISynchronizationContextProvider SynchronizationContextProvider,
    ILogger<AdsXYMoveDiagnosisWindowViewModel> logger) : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    public static string LogHtmlFileName => "AdsXY_MoveDiagnosis_AnyDirection";

    public Guid HtmlLogUniqueId { get; internal set; }

    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    [ObservableProperty]
    private bool _isEnableWindow = true;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

    [ObservableProperty]
    private List<double> _z1List = [];

    [ObservableProperty]
    private List<double> _z2List = [];

    [ObservableProperty]
    private List<double> _z3List = [];

    [ObservableProperty]
    private List<double> _hList = [];

    [ObservableProperty]
    private List<double> _pList = [];

    [ObservableProperty]
    private List<double> _rList = [];

    [ObservableProperty]
    private List<double> _xSpeedList = [];

    [ObservableProperty]
    private List<double> _ySpeedList = [];

    public IEnumerable<StageCoordinateSystemEnum> StageCoordinateSystemValues
    {
        get { return Enum.GetValues(typeof(StageCoordinateSystemEnum)).Cast<StageCoordinateSystemEnum>(); }
    }

    public struct XYMoveData
    {
        public List<double> z1List;
        public List<double> z2List;
        public List<double> z3List;
        public List<double> hList;
        public List<double> rList;
        public List<double> pList;
        public List<double> xSpeedList;
        public List<double> ySpeedList;
        public StageCoordinateSystemEnum type;
        public Point startPos;
        public Point endPos;
        public int time;
        public double speedX;
        public double speedY;
        public double hightMax;
        public double rollMax;
        public double pitchMax;
        public double sumHRP;
        public DateTime runTime;
    };

    [ObservableProperty]
    private ObservableCollection<Position> _selectPositions =
    [
        new() { Number = "pos1", Type = StageCoordinateSystemEnum.Machine, StartPos = new Point(-150000.000, 0), EndPos = new Point(150000.000, 0), Time = 8, SpeedX = 100, SpeedY = 150 },
        new() { Number = "pos2", Type = StageCoordinateSystemEnum.Machine, StartPos = new Point(150000.000, 0), EndPos = new Point(-150000.000, 0), Time = 8, SpeedX = 100, SpeedY = 150 },
        new() { Number = "pos3", Type = StageCoordinateSystemEnum.Machine, StartPos = new Point(0, -150000.000), EndPos = new Point(0, 150000.000), Time = 8, SpeedX = 100, SpeedY = 150 },
        new() { Number = "pos4", Type = StageCoordinateSystemEnum.Machine, StartPos = new Point(0, 150000.000), EndPos = new Point(0, -150000.000), Time = 8, SpeedX = 100, SpeedY = 150 }
    ];

    [ObservableProperty]
    private Position _selectedPosition;

    [ObservableProperty]
    private double _singleDiagnosisSpeed = 90;

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageType = StageCoordinateSystemEnum.Machine;

    [ObservableProperty]
    private int _waitTime = 5;

    [ObservableProperty]
    private double _speedX = 50;

    [ObservableProperty]
    private double _speedY = 50;

    [ObservableProperty]
    private List<WpfPlotModel> _plotListZ = [];

    [ObservableProperty]
    private ObservableCollection<XYMoveData> _adsXYMoveDataAnyDirectionList = [];

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private async Task OnceDianosisActionAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(100);
        var tempPositions = SelectPositions;
        HList.Clear();
        RList.Clear();
        PList.Clear();
        AdsXYMoveDataAnyDirectionList.Clear();
        StageCoordinateSystemEnum StageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;
        for (int j = 0; j < tempPositions.Count; j++)
        {
            StageCoordinateSystemEnum = tempPositions[j].Type;
            var StartPosition = tempPositions[j].StartPos;
            var EndPosition = tempPositions[j].EndPos;
            var WaitTime = tempPositions[j].Time;
            var SpeedX = tempPositions[j].SpeedX;
            var SpeedY = tempPositions[j].SpeedY;

            logger.LogHtmlInformation($"BeginTest_Number_{AdsXYMoveDataAnyDirectionList.Count}", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
            {
                StartPosition,
                EndPosition,
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }), HtmlLogUniqueId.LoggingHtml());
            CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            try
            {
                Task<List<List<double>>> task = Task.FromResult(new List<List<double>>());
                if (StageCoordinateSystemEnum == StageCoordinateSystemEnum.Bright)
                {
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StartPosition, calChipSiteModelEnum);
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 10000, cancellationToken);
                    task = Task.Run(() => adsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(WaitTime)));
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(EndPosition, calChipSiteModelEnum);
                }

                if (StageCoordinateSystemEnum == StageCoordinateSystemEnum.Dark)
                {
                    stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(StartPosition, calChipSiteModelEnum);
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 10000, cancellationToken);
                    task = Task.Run(() => adsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(WaitTime)));
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                    stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(EndPosition, calChipSiteModelEnum);
                }

                if (StageCoordinateSystemEnum == StageCoordinateSystemEnum.Machine)
                {
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(StartPosition);
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 10000, cancellationToken);
                    task = Task.Run(() => adsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(WaitTime)));
                    await Task.Delay(hostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(EndPosition);
                }

                var transBuffer = await task.ConfigureAwait(false);
                if (transBuffer.Count > 0)
                {
                    var XSpeedList = transBuffer[6];
                    if (XSpeedList.Count == 0 && XSpeedList is null)
                        return;
                    var z1List = transBuffer[0].ToList();
                    var z2List = transBuffer[1].ToList();
                    var z3List = transBuffer[2].ToList();
                    var heightList = transBuffer[3].ToList();
                    var rollList = transBuffer[4].ToList();
                    var pitchList = transBuffer[5].ToList();
                    var xspeedList = transBuffer[6].ToList();
                    var yspeedList = transBuffer[7].ToList();
                    var heightMax = heightList.Max(Math.Abs);
                    var rollMax = rollList.Max(Math.Abs);
                    var pitchMax = pitchList.Max(Math.Abs);
                    Z1List = z1List;
                    Z2List = z2List;
                    Z3List = z3List;
                    HList = heightList;
                    RList = rollList;
                    PList = pitchList;
                    XSpeedList = xspeedList;
                    YSpeedList = yspeedList;
                    XYMoveData xYMoveData = new XYMoveData
                    {
                        z1List = z1List,
                        z2List = z2List,
                        z3List = z3List,
                        hList = heightList,
                        pList = pitchList,
                        rList = rollList,
                        xSpeedList = xspeedList,
                        ySpeedList = yspeedList,
                        type = StageType,
                        startPos = StartPosition,
                        endPos = EndPosition,
                        time = WaitTime,
                        speedX = SpeedX,
                        speedY = SpeedY,
                        hightMax = heightMax,
                        rollMax = rollMax,
                        pitchMax = pitchMax,
                        sumHRP = heightMax + rollMax + pitchMax,
                        runTime = DateTime.Now
                    };
                    AdsXYMoveDataAnyDirectionList.Add(xYMoveData);
                    logger.LogHtmlInformation($"StartPosition_{StartPosition} EndPosition_{EndPosition} MoveCurve",
                        HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                        {
                            StartPosition = StartPosition,
                            EndPosition = EndPosition,
                            WaitTime,
                            SpeedX,
                            SpeedY,
                            RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            PlotZ1Z2Z3 = new HtmlPlot2DLinesChart([
                                ("Z1", z1List.ToPoints()),
                                ("Z2", z2List.ToPoints()),
                                ("Z3", z3List.ToPoints())
                            ], "PlotZ1Z2Z3Curve")
                        }), HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation($"StartPosition_{StartPosition} EndPosition_{EndPosition} MoveCurve",
                        HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                        {
                            StartPosition = StartPosition,
                            EndPosition = EndPosition,
                            WaitTime,
                            SpeedX,
                            SpeedY,
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            SumHrp = heightMax + rollMax + pitchMax,
                            RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", heightList.ToPoints()),
                                ("R", rollList.ToPoints()),
                                ("P", pitchList.ToPoints()),
                                ("Xspeed", xspeedList.ToPoints()),
                                ("Yspeed", yspeedList.ToPoints())
                            ], "PlotHrpAndSpeedCurve")
                        }), HtmlLogUniqueId.LoggingHtml());
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@Name}: Once Diagnosis Action Failed", nameof(AdsGainsDiagnosisViewModel));
            }
        }

        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{DiagnosisHtmlLogFileName}_OK"));
        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        HtmlLogUniqueId = Guid.NewGuid();
    }

    public async void OnSelectionChangedCommand(Position selectedItem)
    {
        if (selectedItem != null)
        {
            await Task.Delay(100);
            var tempPositions = SelectPositions;
            int selectedIndex = tempPositions.IndexOf(selectedItem);
            if ((AdsXYMoveDataAnyDirectionList.Count > 0) && (selectedIndex >= 0) && (selectedIndex < AdsXYMoveDataAnyDirectionList.Count))
            {
                Z1List = AdsXYMoveDataAnyDirectionList[selectedIndex].z1List;
                Z2List = AdsXYMoveDataAnyDirectionList[selectedIndex].z2List;
                Z3List = AdsXYMoveDataAnyDirectionList[selectedIndex].z3List;
                HList = AdsXYMoveDataAnyDirectionList[selectedIndex].hList;
                PList = AdsXYMoveDataAnyDirectionList[selectedIndex].pList;
                RList = AdsXYMoveDataAnyDirectionList[selectedIndex].rList;
                XSpeedList = AdsXYMoveDataAnyDirectionList[selectedIndex].xSpeedList;
                YSpeedList = AdsXYMoveDataAnyDirectionList[selectedIndex].ySpeedList;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private void Close()
    {
        CloseView(true);
    }

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        if (message.Value.IsWindowEnable.HasValue)
            IsEnableWindow = message.Value.IsWindowEnable.Value;
    }
}

public partial class Position : ObservableObject
{
    // 添加无参构造函数
    public Position()
    {
        Number = "position1";
        Type = StageCoordinateSystemEnum.Machine;
        StartPos = new Point(-150000.000, 0);
        EndPos = new Point(150000.000, 0);
        Time = 8;
        SpeedX = 100;
        SpeedY = 150;
    }

    public Position(string number, StageCoordinateSystemEnum type, Point startpos, Point endpos, int time, double speedx, double speedy)
    {
        Number = number;
        Type = type;
        StartPos = startpos;
        EndPos = endpos;
        Time = time;
        SpeedX = speedx;
        SpeedY = speedy;
    }

    [ObservableProperty]
    private string _number;

    [ObservableProperty]
    private StageCoordinateSystemEnum _type;

    [ObservableProperty]
    private Point _startPos;

    [ObservableProperty]
    private Point _endPos;

    [ObservableProperty]
    private int _time;

    [ObservableProperty]
    private double _speedX;

    [ObservableProperty]
    private double _speedY;
};