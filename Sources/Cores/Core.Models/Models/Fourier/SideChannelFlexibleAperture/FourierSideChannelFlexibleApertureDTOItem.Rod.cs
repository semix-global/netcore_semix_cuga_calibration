using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class Rod(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<Rod, Rod>
    {
        [ObservableProperty]
        public partial int Index { get; set; }

        [ObservableProperty]
        public partial double MotorAbsoluteValue { get; set; }

        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable)
        {
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XMinYMin |
                                      BitmapImageROIResizeJoystickStateEnum.XCenterYMin |
                                      BitmapImageROIResizeJoystickStateEnum.XMaxYMin
        };

        public Rod AdaptIn(Rod obj)
        {
            Index = obj.Index;
            MotorAbsoluteValue = obj.MotorAbsoluteValue;
            ImageROI = obj.ImageROI;

            return obj;
        }

        public void Reset()
        {
            Index = 0;
            MotorAbsoluteValue = 0d;
            ImageROI = Rect.Empty;
        }
    }
}