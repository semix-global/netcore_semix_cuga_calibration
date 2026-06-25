using Core.Models.Exceptions;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Microscope.Enums;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(MicroscopeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class MicroscopeViewModel(
    ICalibrationMicroscopeService calibrationMicroscopeService,
    ILogger<MicroscopeViewModel> logger,
    AfViewModel afViewModel,
    IApplicationCookieService applicationCookieCacheProvider,
    StageViewModel stageViewModel) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationMicroscopeService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<MicroscopeLensInformation> GetMicroscopeLensInformations()
    {
        var ret = calibrationMicroscopeService.GetMicroscopeLensInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public MicroscopeLensInformation CgMicroscopeLensToMicroscopeLensInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        var ret = calibrationMicroscopeService.CgMicroscopeLensToMicroscopeLensInfo(cgMicroscopeLens);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public MicroscopeLensInformation GetCurrentMicroscopeLensInformation()
    {
        var ret = calibrationMicroscopeService.GetCurrentMicroscopeLensInformation();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    #region 坐标转换

    public Vector GetMicroscopeLensInformationOffset(
        MicroscopeLensInformation previousMicroscopeLensInformation,
        MicroscopeLensInformation currentMicroscopeLensInformation)
    {
        var microscopeCentricities = applicationCookieCacheProvider.GetCalibrations<MicroscopeCentricityItemDto>();

        var previousMicroscopeCentricity = microscopeCentricities.SingleOrDefault(t => t.LensInformation == previousMicroscopeLensInformation);
        var currentMicroscopeCentricity = microscopeCentricities.SingleOrDefault(t => t.LensInformation == currentMicroscopeLensInformation);

        return previousMicroscopeCentricity is not null && currentMicroscopeCentricity is not null
            ? currentMicroscopeCentricity.Offset - previousMicroscopeCentricity.Offset
            : Vector.Zero;
    }

    public Point GetMicroscopeLensInformationPosition(
        MicroscopeLensInformation previousMicroscopeLensInformation,
        MicroscopeLensInformation currentMicroscopeLensInformation,
        Point position) => position + GetMicroscopeLensInformationOffset(previousMicroscopeLensInformation, currentMicroscopeLensInformation);

    #endregion

    public void SwitchMicroscopeLensInformation(MicroscopeLensInformation microscopeLensInformation, bool isMoveToMicroscopeCenter = false)
    {
        var switchMicroscopeLensInformationNotAutoFocus = SwitchMicroscopeLensInformationNotAutoFocus(microscopeLensInformation, isMoveToMicroscopeCenter);
        if (switchMicroscopeLensInformationNotAutoFocus == false) throw new CugaException("Switch Microscope Lens Information Not AutoFocus Failed");

        afViewModel.ToggleBrightFieldEnable(true);

        Thread.Sleep(500);
    }

    public bool SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation, bool isMoveToMicroscopeCenter = false)
    {
        var resultFocusList = applicationCookieCacheProvider.GetCalibrations<MicroscopeFocusItemDto>();

        var previousMicroscopeLensInformation = GetCurrentMicroscopeLensInformation();
        var newMicroscopeFocusItemDto = resultFocusList.SingleOrDefault(t => t.LensInformation == microscopeLensInformation);

        afViewModel.ToggleBrightFieldEnable(false);

        var taskAf1 = Task.Run(() =>
        {
            if (previousMicroscopeLensInformation == microscopeLensInformation) return true;
            if (newMicroscopeFocusItemDto?.IsOk == true)
            {
                afViewModel.SetSensorEcsValue(newMicroscopeFocusItemDto.EcsValue);
            }

            return true;
        });

        var taskAf2 = Task.Run(() =>
        {
            if (newMicroscopeFocusItemDto?.IsOk == true)
            {
                afViewModel.SetSensorBrightFieldChuckStandardEcsValue(microscopeLensInformation, newMicroscopeFocusItemDto.EcsValue);
            }

            return true;
        });

        var taskAf3 = Task.Run(() =>
        {
            afViewModel.SetSensorMicroscopeObjValue(microscopeLensInformation);
            return true;
        });

        var taskMicroscope1 = Task.Run(() =>
        {
            if (previousMicroscopeLensInformation == microscopeLensInformation) return true;

            var ret = calibrationMicroscopeService.SwitchMicroscopeLensInformationNotAutoFocus(microscopeLensInformation);
            return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
        });

        var taskMicroscope2 = Task.Run(() =>
        {
            if (newMicroscopeFocusItemDto?.IsOk == true)
            {
                SetVoltage(newMicroscopeFocusItemDto.MicroscopeVoltage);
            }

            return true;
        });

        var taskMove = Task.Run(() =>
        {
            if (isMoveToMicroscopeCenter == false) return true;

            stageViewModel.MoveRelativeStageXy((Point)GetMicroscopeLensInformationOffset(previousMicroscopeLensInformation, microscopeLensInformation));

            return true;
        });

        var waitAll = Task.WaitAll([taskAf1, taskAf2, taskAf3, taskMicroscope1, taskMicroscope2, taskMove], TimeSpan.FromSeconds(10));
        if (waitAll == false) throw new CugaException("Wait all task failed");

        return taskAf1.Result && taskAf2.Result && taskAf3.Result && taskMicroscope1.Result && taskMicroscope2.Result && taskMove.Result;
    }

    public void SetVoltage(double voltage)
    {
        var ret = calibrationMicroscopeService.SetVoltage(voltage);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetVoltage()
    {
        var ret = calibrationMicroscopeService.GetVoltage();
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
        return ret.Anything;
    }

    public (double min, double max) GetVoltageRange()
    {
        var ret = calibrationMicroscopeService.GetVoltageRange();
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
        return ret.Anything;
    }

    /// <summary>
    /// 设置电压到AfError反馈值0V附近(二分法快速逼近)
    /// </summary>
    /// <param name="setVoltageAfErrorThreshold"></param>
    /// <param name="maxIterations">最大循环次数</param>
    /// <param name="logGuid"></param>
    /// <param name="logName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>是否成功</returns>
    public (bool isSuccess, double Voltage, double AfErrorAverage) SetVoltageToAfErrorZeroFast(double setVoltageAfErrorThreshold,
        int maxIterations, Guid? logGuid, string? logName, CancellationToken cancellationToken)
    {
        var buffersAverageList = new List<double>();
        var voltageList = new List<int>();
        var midVoltage = 0;
        try
        {
            var iterations = 0;
            var (startVoltage, endVoltage) = GetVoltageRange();

            var previousAfErrorAverage = double.MaxValue;
            while (iterations < maxIterations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                midVoltage = Convert.ToInt32((startVoltage + endVoltage) / 2);
                voltageList.Add(midVoltage);
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        SetVoltage(midVoltage);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (logGuid is not null && logName is not null)
                            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Set voltage to af error zero out of range!{ex.Message}"), logGuid.Value.LoggingHtml());
                        else
                            logger.LogError(ex, "{@Name} Set voltage {@Voltage} to af error zero out of range!", nameof(MicroscopeViewModel), midVoltage);
                    }
                }

                Thread.Sleep(1000);
                List<double> currentTraceBuffer = [];
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        currentTraceBuffer = afViewModel.GetSensorAfErrorTraceBufferList(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception ex)
                    {
                        if (logGuid is not null && logName is not null)
                            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Trace buffer is empty!{ex.Message}"), logGuid.Value.LoggingHtml());
                        else
                            logger.LogError(ex, "{@Name} Trace buffer is empty!", nameof(MicroscopeViewModel));
                        continue;
                    }

                    break;
                }

                var currentAfErrorAverage = currentTraceBuffer.Average();
                buffersAverageList.Add(currentAfErrorAverage);

                if (logGuid is not null && logName is not null)
                    logger.LogHtmlInformation($"{logName}: Set Voltage {midVoltage} ,Time:{iterations}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        StartVoltage = startVoltage,
                        EndVoltage = endVoltage,
                        MidVoltage = midVoltage,
                        CurrentTraceBufferValueAverage = currentAfErrorAverage,
                        TraceBuffer = new HtmlPlot2DLinesChart([
                            ("CurrentTraceBuffer", currentTraceBuffer.ToPoints())
                        ], "TraceBuffer")
                    }), logGuid.Value.LoggingHtml());
                else
                    logger.LogWarning("{@Name} Set Voltage Error Time {@Times}", nameof(MicroscopeViewModel), iterations);

                if (Math.Abs(previousAfErrorAverage) < setVoltageAfErrorThreshold && Math.Abs(previousAfErrorAverage) <= Math.Abs(currentAfErrorAverage))
                    return (true, previousAfErrorAverage < 0 ? startVoltage : endVoltage, previousAfErrorAverage);

                previousAfErrorAverage = currentAfErrorAverage;
                if (currentAfErrorAverage < 0)
                {
                    startVoltage = midVoltage;
                }
                else
                {
                    endVoltage = midVoltage;
                }

                if (voltageList.Count(t => t == midVoltage) >= 3)
                    return (Math.Abs(currentAfErrorAverage) < setVoltageAfErrorThreshold, midVoltage, currentAfErrorAverage);

                iterations++;
            }

            if (logGuid is not null && logName is not null)
                logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Set voltage to af error zero failed!"), logGuid.Value.LoggingHtml());
            else
                logger.LogError("{@Name} Set voltage to af error zero failed!", nameof(MicroscopeViewModel));

            return (false, midVoltage, buffersAverageList.Select(t => Math.Abs(t)).Min());
        }
        catch (Exception ex)
        {
            if (logGuid is not null && logName is not null)
                logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Set voltage to af error zero out of range!{ex.Message}"), logGuid.Value.LoggingHtml());
            else
                logger.LogError(ex, "{@Name} Set voltage to af error zero out of range!", nameof(MicroscopeViewModel));
            return (false, 0, 0);
        }
        finally
        {
            if (logGuid is not null && logName is not null)
                logger.LogHtmlInformation($"{logName}: Iterations Result", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    BuffersAverage = new HtmlPlot2DLinesChart([
                        ("BuffersAverage", buffersAverageList.ToPoints())
                    ], "BuffersAverage")
                }), logGuid.Value.LoggingHtml());
        }
    }

    #endregion 服务
}