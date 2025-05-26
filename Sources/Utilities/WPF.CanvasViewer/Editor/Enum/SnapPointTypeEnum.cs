namespace CanvasViewer.Editor.Enum;

[Flags]
public enum SnapPointTypeEnum
{
    None = 0,
    End = 1,
    Middle = 2,
    Center = 4,
    Quadrant = 8,
    Point = 16,
    All = End | Middle | Center | Quadrant | Point
}