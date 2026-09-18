using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class RodResult(BitmapImageDrawable bitmapImageDrawable) : Rod(bitmapImageDrawable), IAdaptIn<RodResult, RodResult>
    {
        [ObservableProperty]
        public partial double PixelSize { get; set; }

        [ObservableProperty]
        public partial Rect MinImageROI { get; set; }

        [ObservableProperty]
        public partial Rect MaxImageROI { get; set; }

        public RodResult AdaptIn(RodResult obj)
        {
            base.AdaptIn(obj);

            PixelSize = obj.PixelSize;
            MinImageROI = obj.MinImageROI;
            MaxImageROI = obj.MaxImageROI;

            return this;
        }

        public override void Reset()
        {
            base.Reset();

            PixelSize = 0d;
            MinImageROI = Rect.Empty;
            MaxImageROI = Rect.Empty;
        }
    }
}