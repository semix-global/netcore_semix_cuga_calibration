using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Models.Laser.AutoFocus;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis;

[IOCAppService(ServiceType = typeof(AfGetAnyNscDiagnosisWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AfGetAnyNscDiagnosisWindowViewModel(
    StageViewModel stageViewModel,
    AdsViewModel adsViewModel,
    AfViewModel afViewModel,
    ICalibrationStatusService CalibrationStatusService,
    IDialogWindowProvider DialogWindowProvider,
    IHostEnvironment hostEnvironment,
    ILogger<AfGetAnyNscDiagnosisWindowViewModel> logger) : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    public static string LogHtmlFileName => "AfGetNscCurveDiagnosis_AnyPosition";

    public Guid HtmlLogUniqueId { get; internal set; }

    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    [ObservableProperty]
    private LaserAutoFocusDto _resultLaserAutoFocusDto;

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private bool _isEnableWindow = true;

    [ObservableProperty]
    private Point _selectPosition;

    [ObservableProperty]
    private double _startEcs;

    [ObservableProperty]
    private double _endEcs;

    [ObservableProperty]
    private double _speedEcsPerSecond;

    [ObservableProperty]
    private IReadOnlyList<double> _currentEcs = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentNsc = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentLvdt = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentFa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentNa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentFb = [];

    [ObservableProperty]
    private IReadOnlyList<double> _currentNb = [];

    [ObservableProperty]
    private Point[] _currentEcsNscPointList = [];

    public struct AfGetAnyNscData
    {
        public List<double> CurrentEcs;
        public List<double> CurrentNsc;
        public List<double> CurrentLvdt;
        public List<double> CurrentFa;
        public List<double> CurrentNa;
        public List<double> CurrentFb;
        public List<double> CurrentNb;
        public Point[] CurrentEcsNscPointList;
        public Point SelectPosition;
        public double StartEcs;
        public double EndEcs;
        public double SpeedEcsPerSecond;
        public DateTime RunTime;
    };

    public partial class Position : ObservableObject
    {
        public Position()
        {
            Number = "position1";
            Value = new Point(-39116.685, -142933.168);
            StartEcs = 6000;
            EndEcs = 6700;
            Ecs = 100;
        }

        public Position(string number, Point value, double startecs, double endecs, double ecs)
        {
            Number = number;
            Value = value;
            StartEcs = startecs;
            EndEcs = endecs;
            Ecs = ecs;
        }

        [ObservableProperty]
        private string _number;

        [ObservableProperty]
        private Point _value;

        [ObservableProperty]
        private double _startEcs;

        [ObservableProperty]
        private double _endEcs;

        [ObservableProperty]
        private double _ecs;
    }

    [ObservableProperty]
    private ObservableCollection<Position> _selectPositions = new ObservableCollection<Position>
    {
        new Position { Number = "pos1", Value = new Point(-39118.185, -142953.168), StartEcs = 6000, EndEcs = 6700, Ecs = 6000 },
        new Position { Number = "pos2", Value = new Point(-39132.469, -142973.516), StartEcs = 6000, EndEcs = 6700, Ecs = 6000 },
        new Position { Number = "pos3", Value = new Point(-39215.956, -143125.986), StartEcs = 6000, EndEcs = 6700, Ecs = 6000 }
    };

    [ObservableProperty]
    private Position _selectedPosition;

    [ObservableProperty]
    private double _motorPosition = 15;

    [ObservableProperty]
    private double _motorEcs = 5000;

    [ObservableProperty]
    private int _waitTime = 5;

    [ObservableProperty]
    private List<WpfPlotModel> _plotListZ = [];

    [ObservableProperty]
    private ObservableCollection<AfGetAnyNscData> _afAnyPositionDataList = [];

    [ObservableProperty]
    private ObservableCollection<LaserAutoFocusDto> _nscAnyPositionList = [];

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private async Task LoadAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out var laserAutoFocusDto, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        ResultLaserAutoFocusDto = laserAutoFocusDto;
    }

    [RelayCommand]
    private async Task OnceDianosisActionAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(ResultLaserAutoFocusDto);
        AfAnyPositionDataList.Clear();
        for (int j = 0; j < SelectPositions.Count; j++)
        {
            SelectPosition = SelectPositions[j].Value;
            StartEcs = SelectPositions[j].StartEcs;
            EndEcs = SelectPositions[j].EndEcs;
            logger.LogHtmlInformation($"BeginTest_Number_{AfAnyPositionDataList.Count}", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
            {
                SelectPosition,
                StartEcs,
                EndEcs,
                SpeedEcsPerSecond
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(SelectPosition);

                afViewModel.SetSensorNscCompensation(0, 1);
                await Task.Delay(100, cancellationToken);

                afViewModel.SetSensorCurrentValue(true, ResultLaserAutoFocusDto.CurrentA);
                afViewModel.SetSensorCurrentValue(false, ResultLaserAutoFocusDto.CurrentB);
                await Task.Delay(100, cancellationToken);

                afViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var ecsToNmRatio = afViewModel.GetNmPerEcs() * 1000;
                double offset = 0, gain = 1;

                cancellationToken.ThrowIfCancellationRequested();

                afViewModel.SetSensorNscCompensation(offset, gain);
                afViewModel.SetSensorEcsValue(StartEcs);
                await Task.Delay(100, cancellationToken);

                var traceBufferList = afViewModel.GetSensorNscTraceBufferList(StartEcs, EndEcs, SpeedEcsPerSecond, TimeSpan.FromSeconds(Math.Abs(EndEcs - StartEcs) / SpeedEcsPerSecond + 2));
                var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                var fa = traceBufferList.Select(t => t.Fa).ToArray();
                var na = traceBufferList.Select(t => t.Na).ToArray();
                var fb = traceBufferList.Select(t => t.Fb).ToArray();
                var nb = traceBufferList.Select(t => t.Nb).ToArray();
                {
                    CurrentEcs = ecs?.ToList() ?? [];
                    CurrentNsc = nsc?.ToList() ?? [];
                    CurrentLvdt = lvdt?.ToList() ?? [];
                    CurrentFa = fa?.ToList() ?? [];
                    CurrentNa = na?.ToList() ?? [];
                    CurrentFb = fb?.ToList() ?? [];
                    CurrentNb = nb?.ToList() ?? [];
                }

                if ((ecs == null) || (nsc == null) || (ecs.Length <= 0) || (nsc.Length <= 0))
                {
                    logger.LogError("ECS or NSC data is empty!!");
                    return;
                }

                {
                    if (ecs != null && nsc != null && ecs.Length == nsc.Length)
                    {
                        var points = new Point[ecs.Length];
                        for (int i = 0; i < ecs.Length; i++)
                        {
                            points[i] = new Point(
                                ecs[i] * ecsToNmRatio,
                                nsc[i]
                            );
                        }

                        CurrentEcsNscPointList = points;
                    }
                    else
                    {
                        // 处理数据不匹配的情况
                        CurrentEcsNscPointList = Array.Empty<Point>();
                    }
                }

                AfGetAnyNscData afAnyPositionData = new AfGetAnyNscData()
                {
                    CurrentEcs = ecs?.ToList() ?? [],
                    CurrentNsc = nsc?.ToList() ?? [],
                    CurrentLvdt = lvdt?.ToList() ?? [],
                    CurrentFa = fa?.ToList() ?? [],
                    CurrentNa = na?.ToList() ?? [],
                    CurrentFb = fb?.ToList() ?? [],
                    CurrentNb = nb?.ToList() ?? [],
                    CurrentEcsNscPointList = CurrentEcsNscPointList,
                    SelectPosition = SelectPosition,
                    StartEcs = StartEcs,
                    EndEcs = EndEcs,
                    SpeedEcsPerSecond = SpeedEcsPerSecond,
                    RunTime = DateTime.Now
                };
                AfAnyPositionDataList.Add(afAnyPositionData);
                logger.LogHtmlInformation($"SelectPosition_{SelectPosition} StartPosition_{StartEcs} EndPosition_{EndEcs} DataCurve",
                    HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                    {
                        RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        PlotHrp = new HtmlPlot2DLinesChart([
                            ("ecs", ecs?.ToPoints() ?? []),
                            ("nsc", nsc?.ToPoints() ?? []),
                            ("lvdt", lvdt?.ToPoints() ?? []),
                            ("fa", fa?.ToPoints() ?? []),
                            ("na", na?.ToPoints() ?? []),
                            ("fb", fb?.ToPoints() ?? []),
                            ("nb", nb?.ToPoints() ?? [])
                        ], "PlotAfCurveOfAnyPosition1")
                    }), HtmlLogUniqueId.LoggingHtml());
                logger.LogHtmlInformation($"SelectPosition_{SelectPosition} StartPosition_{StartEcs} EndPosition_{EndEcs} DataCurve",
                    HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                    {
                        RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        PlotHrp = new HtmlPlot2DLinesChart([
                            ("ecs_nsc", CurrentEcsNscPointList)
                        ], "PlotAfCurveOfAnyPosition2")
                    }), HtmlLogUniqueId.LoggingHtml());
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

    [RelayCommand]
    private async Task GetEcsOfMotorActionAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(ResultLaserAutoFocusDto);
        for (int j = 0; j < SelectPositions.Count; j++)
        {
            if (j >= AfAnyPositionDataList.Count)
                break;
            var currentNsc = AfAnyPositionDataList[j].CurrentNsc;
            var nscVector = Vector<double>.Build.DenseOfEnumerable(currentNsc);
            Vector<double> nscIntervalVector;
            if (ResultLaserAutoFocusDto.IsNscUseMaxValue)
            {
                var nscMaxIndex = nscVector.MaximumIndex();
                var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                var nscMinNegativeRightIndex = nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
                nscIntervalVector = ResultLaserAutoFocusDto.IsNscUsePositiveSlope
                    ? nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                    : nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);

                if (ResultLaserAutoFocusDto.IsNscUsePositiveSlope)
                {
                    for (int i = nscMinPositiveLeftIndex; i <= nscMaxIndex; i++)
                    {
                        if (currentNsc[i] * currentNsc[i + 1] < 0)
                        {
                            MotorEcs = (AfAnyPositionDataList[j].CurrentEcs[i] + AfAnyPositionDataList[j].CurrentEcs[i + 1]) * 0.5;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = nscMaxIndex; i <= nscMinNegativeRightIndex; i++)
                    {
                        if (currentNsc[i] * currentNsc[i + 1] < 0)
                        {
                            MotorEcs = (AfAnyPositionDataList[j].CurrentEcs[i] + AfAnyPositionDataList[j].CurrentEcs[i + 1]) * 0.5;
                            break;
                        }
                    }
                }
            }
            else
            {
                var nscMinIndex = nscVector.MinimumIndex();
                var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                var nscMaxPositiveRightIndex = nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;
                nscIntervalVector = ResultLaserAutoFocusDto.IsNscUsePositiveSlope
                    ? nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                    : nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);

                if (ResultLaserAutoFocusDto.IsNscUsePositiveSlope)
                {
                    for (int i = nscMinIndex; i <= nscMaxPositiveRightIndex; i++)
                    {
                        if (currentNsc[i] * currentNsc[i + 1] < 0)
                        {
                            MotorEcs = (AfAnyPositionDataList[j].CurrentEcs[i] + AfAnyPositionDataList[j].CurrentEcs[i + 1]) * 0.5;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = nscMaxNegativeLeftIndex; i <= nscMinIndex; i++)
                    {
                        if (currentNsc[i] * currentNsc[i + 1] < 0)
                        {
                            MotorEcs = (AfAnyPositionDataList[j].CurrentEcs[i] + AfAnyPositionDataList[j].CurrentEcs[i + 1]) * 0.5;
                            break;
                        }
                    }
                }
            }

            SelectPositions[j].Ecs = (int)MotorEcs;
            var nscMax = nscIntervalVector.Maximum();
            var nscMin = nscIntervalVector.Minimum();
        }
    }

    public void OnSelectionChangedCommand(Position selectedItem)
    {
        if (selectedItem != null)
        {
            int selectedIndex = SelectPositions.IndexOf(selectedItem);
            if ((AfAnyPositionDataList.Count > 0) && (selectedIndex >= 0) && (selectedIndex < AfAnyPositionDataList.Count))
            {
                CurrentEcs = AfAnyPositionDataList[selectedIndex].CurrentEcs;
                CurrentNsc = AfAnyPositionDataList[selectedIndex].CurrentNsc;
                CurrentFa = AfAnyPositionDataList[selectedIndex].CurrentFa;
                CurrentNa = AfAnyPositionDataList[selectedIndex].CurrentNa;
                CurrentFb = AfAnyPositionDataList[selectedIndex].CurrentFb;
                CurrentNb = AfAnyPositionDataList[selectedIndex].CurrentNb;

                var ecs = CurrentEcs;
                var nsc = CurrentNsc;
                var ecsToNmRatio = afViewModel.GetNmPerEcs() * 1000;
                if ((ecs == null) || (nsc == null) || (ecs.Count <= 0) || (nsc.Count <= 0))
                {
                    logger.LogError("ECS or NSC data is empty!!");
                    return;
                }

                {
                    if (ecs != null && nsc != null && ecs.Count == nsc.Count)
                    {
                        var points = new Point[ecs.Count];
                        for (int i = 0; i < ecs.Count; i++)
                        {
                            points[i] = new Point(
                                ecs[i] * ecsToNmRatio,
                                nsc[i]
                            );
                        }

                        CurrentEcsNscPointList = points;
                    }
                    else
                    {
                        // 处理数据不匹配的情况
                        CurrentEcsNscPointList = Array.Empty<Point>();
                    }
                }
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