using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting;

/// <summary>
/// 暗场增益参数
/// </summary>
public sealed partial class SettingDarkFieldGainParam : ObservableObject, IAdaptIn<SettingDarkFieldGainParam, SettingDarkFieldGainParam>
{
    /// <summary>
    /// PMTId
    /// </summary>
    [ObservableProperty]
    private int _pmtId = 8;

    /// <summary>
    /// PMTId
    /// </summary>
    [ObservableProperty]
    private int _channelId = 3;

    /// <summary>
    /// 增益最小值
    /// </summary>
    [ObservableProperty]
    private double _gainMin = -10;

    /// <summary>
    /// 增益最大值
    /// </summary>
    [ObservableProperty]
    private double _gainMax = 10;

    /// <summary>
    /// 增益步近
    /// </summary>
    [ObservableProperty]
    private double _gainInterval = 0.5;

    /// <summary>
    /// 前后跳过多少点
    /// </summary>
    [ObservableProperty]
    private int _judgeGainSkipCout;

    /// <summary>
    /// 目标Pmt值
    /// </summary>
    [ObservableProperty]
    private double _targetPmtAverageValue = 2000;

    /// <summary>
    ///暗场波形功率以及电压
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GainOfCoefficientParam> _gainOfCoefficientList = [];

    #region Mapper

    public SettingDarkFieldGainParam AdaptIn(SettingDarkFieldGainParam obj)
    {
        GainMin = obj.GainMin;
        GainMax = obj.GainMax;
        GainInterval = obj.GainInterval;
        JudgeGainSkipCout = obj.JudgeGainSkipCout;
        TargetPmtAverageValue = obj.TargetPmtAverageValue;
        GainOfCoefficientList = [.. obj.GainOfCoefficientList.Select(x => new GainOfCoefficientParam().AdaptIn(x))];

        return obj;
    }

    #endregion Mapper
}

/// <summary>
/// 暗场波形功率以及电压
/// </summary>
public sealed partial class GainOfCoefficientParam : ObservableObject, IAdaptIn<GainOfCoefficientParam, GainOfCoefficientParam>
{
    /// <summary>
    /// 校准暗场波形功率系数
    /// </summary>
    [ObservableProperty]
    private double _coefficient;

    /// <summary>
    /// 最佳增益电压
    /// </summary>
    [ObservableProperty]
    private double _gain;

    /// <summary>
    /// 最佳增益值
    /// </summary>
    [ObservableProperty]
    private double _targetPmtAverageValue;

    /// <summary>
    /// gain集合值
    /// </summary>
    [ObservableProperty]
    private List<double> _targetPmtValueList = [];

    #region Mapper

    public GainOfCoefficientParam AdaptIn(GainOfCoefficientParam obj)
    {
        Coefficient = obj.Coefficient;
        Gain = obj.Gain;
        TargetPmtAverageValue = obj.TargetPmtAverageValue;
        TargetPmtValueList = [.. obj.TargetPmtValueList];

        return obj;
    }

    #endregion Mapper
}