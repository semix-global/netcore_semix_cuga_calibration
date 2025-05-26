namespace Local.SQL.DB.Providers.Models.Attributes;

/// <summary>
/// 雪花Id
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SnowflakeAttribute : Attribute
{
    public bool Enable { get; set; } = true;
}