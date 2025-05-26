using CanvasViewer.Editor.Entity.Getter;

namespace CanvasViewer.Editor.Entity.Options;

public sealed class SelectionOptions(string message, bool usePickedSelection = true) : AbstractInputOptions<SelectionSet>(message)
{
    /// <summary>
    /// 将符合的类型的<code>CanvasEditor.PickedSelection</code>结果集做为选择
    /// </summary>
    public readonly List<Type> AllowedClasses = [];

    /// <summary>
    /// 选择的是否直接使用<code>CanvasEditor.PickedSelection</code>结果集做为选择
    /// </summary>
    public bool UsePickedSelection { get; set; } = usePickedSelection;

    /// <summary>
    /// 添加直接使用的类型
    /// </summary>
    public void AddAllowedClass(Type type) => AllowedClasses.Add(type);
}