using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class RodResult(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<RodResult, RodResult>
    {
        [ObservableProperty]
        public partial int Index { get; set; }

        [ObservableProperty]
        public partial double PixelSize { get; set; }

        [ObservableProperty]
        public partial Rect MinImageROI { get; set; }

        [ObservableProperty]
        public partial Rect MaxImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable)
        {
            ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XCenterYMin
        };

        public RodResult AdaptIn(RodResult obj)
        {
            Index = obj.Index;
            PixelSize = obj.PixelSize;
            MinImageROI = obj.MinImageROI;
            MaxImageROI = obj.MaxImageROI;

            return obj;
        }

        public void Reset()
        {
            Index = 0;
            PixelSize = 0d;
            MinImageROI = Rect.Empty;
            MaxImageROI = Rect.Empty;
        }
    }
}