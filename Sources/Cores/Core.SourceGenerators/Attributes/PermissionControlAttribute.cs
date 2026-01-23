namespace Core.SourceGenerators.Attributes;

/// <summary>
/// 标记需要权限控制的 UserControl。
/// Source Generator 将自动生成权限检查代码。
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PermissionControlAttribute : System.Attribute
{
}
