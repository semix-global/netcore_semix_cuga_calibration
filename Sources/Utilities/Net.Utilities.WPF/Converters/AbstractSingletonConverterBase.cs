using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Net.Utilities.WPF.Converters;

public abstract class AbstractSingletonConverterBase<TConverter> : DependencyObject, IValueConverter
    where TConverter : class, new()
{
    private static readonly Lazy<TConverter> InstanceConstructor = new(() => new TConverter(), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// 使用Lazy来延迟初始化单例实例, 默认`Mode: LazyThreadSafetyMode.PublicationOnly`线程安全
    /// </summary>
    public static TConverter Instance => InstanceConstructor.Value;

    public abstract object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public abstract object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}