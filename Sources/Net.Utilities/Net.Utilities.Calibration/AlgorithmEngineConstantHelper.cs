namespace Net.Utilities.Calibration;

/// <summary>
/// 显示域算子全局默认引擎。
/// <para>显示域算子（<see cref="BitmapImageExtensions"/>）调用时未显式指定引擎的兜底选择，
/// 默认 HAlgorithm；持久化载体为 Setting 窗口 Common 页（SettingCommonParam.AlgorithmEngineTypeEnum</para>
/// </summary>
public static class AlgorithmEngineConstantHelper
{
    /// <summary>
    /// 当前全局默认算法引擎
    /// </summary>
    public static AlgorithmEngineTypeEnum Current { get; set; } = AlgorithmEngineTypeEnum.HAlgorithm;
}
