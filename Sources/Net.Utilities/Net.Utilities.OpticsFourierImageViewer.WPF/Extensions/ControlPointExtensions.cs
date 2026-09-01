using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Enums.Inputs;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

public static class ControlPointExtensions
{
    extension(ControlPoint @this)
    {
        public (BitmapImageROIResizeJoystickStateEnum ControlPointTypeEnum, CursorTypeEnum CursorTypeEnum) GetControlPointInformation()
        {
            return @this.Name switch
            {
                nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMax) => (BitmapImageROIResizeJoystickStateEnum.XMaxYMax, CursorTypeEnum.SizeNESW),
                nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMax) => (BitmapImageROIResizeJoystickStateEnum.XMinYMax, CursorTypeEnum.SizeNWSE),
                nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMin) => (BitmapImageROIResizeJoystickStateEnum.XMinYMin, CursorTypeEnum.SizeNESW),
                nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMin) => (BitmapImageROIResizeJoystickStateEnum.XMaxYMin, CursorTypeEnum.SizeNWSE),
                nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMax) => (BitmapImageROIResizeJoystickStateEnum.XCenterYMax, CursorTypeEnum.SizeNS),
                nameof(BitmapImageROIResizeJoystickStateEnum.XMinYCenter) => (BitmapImageROIResizeJoystickStateEnum.XMinYCenter, CursorTypeEnum.SizeWE),
                nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMin) => (BitmapImageROIResizeJoystickStateEnum.XCenterYMin, CursorTypeEnum.SizeNS),
                nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYCenter) => (BitmapImageROIResizeJoystickStateEnum.XMaxYCenter, CursorTypeEnum.SizeWE),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<(BitmapImageROIResizeJoystickStateEnum ControlPointTypeEnum, CursorTypeEnum CursorTypeEnum)>(nameof(@this), @this.Name)
            };
        }
    }
}