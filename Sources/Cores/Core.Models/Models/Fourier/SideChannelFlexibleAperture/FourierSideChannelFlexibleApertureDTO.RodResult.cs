using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class FourierSideChannelFlexibleApertureDTO
{
    public sealed partial class RodResult(BitmapImageDrawable bitmapImageDrawable) : Rod(bitmapImageDrawable), IAdaptIn<RodResult, RodResult>
    {
        [ObservableProperty]
        public partial Rect MinImageROI { get; set; }

        [ObservableProperty]
        public partial Rect MaxImageROI { get; set; }

        [ObservableProperty]
        public partial double PixelSize { get; set; }

        public RodResult AdaptIn(RodResult obj)
        {
            base.AdaptIn(obj);

            MinImageROI = obj.MinImageROI;
            MaxImageROI = obj.MaxImageROI;
            PixelSize = obj.PixelSize;

            return this;
        }

        public override void Reset()
        {
            base.Reset();

            MinImageROI = Rect.Empty;
            MaxImageROI = Rect.Empty;
            PixelSize = 0d;
        }
    }
}