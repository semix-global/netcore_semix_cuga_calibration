namespace Local.SQL.DB.Providers.Models.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class EntityAnnotationAttribute : Attribute
{
    /// <summary>
    /// UI显示的顺序
    /// </summary>
    public int DisplayOrderNum { get; set; } = 0;

    /// <summary>
    /// 插入设置该字段服务器端时间，默认值true，指定为false插入时不设置
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}