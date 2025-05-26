using Net.Utilities.Enums;
using Net.Utilities.Nlog.Extensions;
using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class LogLevelToNlogLevelConverter : AbstractSingletonConverterBase<LogLevelToNlogLevelConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not LogLevelEnum logLevelEnum) throw new NotSupportedException();

        return logLevelEnum.ToNLogLevel();
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not NLog.LogLevel logLevel) throw new NotSupportedException();

        return logLevel.ToLogLevelEnum();
    }
}