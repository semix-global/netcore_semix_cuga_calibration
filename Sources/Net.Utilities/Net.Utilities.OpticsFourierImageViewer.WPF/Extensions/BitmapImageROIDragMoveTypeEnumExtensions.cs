using Net.Utilities.Graphics.Primitives.Enums.Inputs;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

public static class BitmapImageROIDragMoveTypeEnumExtensions
{
    extension(BitmapImageROIDragMoveTypeEnum @this)
    {
        public CursorTypeEnum GetMoveCursorTypeEnum()
        {
            return @this switch
            {
                BitmapImageROIDragMoveTypeEnum.None => CursorTypeEnum.Arrow,
                BitmapImageROIDragMoveTypeEnum.X => CursorTypeEnum.SizeWE,
                BitmapImageROIDragMoveTypeEnum.Y => CursorTypeEnum.SizeNS,
                _ => CursorTypeEnum.SizeAll
            };
        }
    }
}