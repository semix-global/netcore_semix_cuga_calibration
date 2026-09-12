using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class RodResult(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<RodResult, RodResult>
    {
        public int Index { get; init; }

        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = false;

        [ObservableProperty]
        public partial double PixelSize { get; set; }

        [ObservableProperty]
        public partial Rect MinImageROI { get; set; }

        [ObservableProperty]
        public partial Rect MaxImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        [Newtonsoft.Json.JsonConstructor]
        private RodResult() : this(new BitmapImageDrawable())
        {
        }

        public RodResult AdaptIn(RodResult obj)
        {
            IsDeleted = obj.IsDeleted;
            PixelSize = obj.PixelSize;
            MinImageROI = obj.MinImageROI;
            MaxImageROI = obj.MaxImageROI;

            return this;
        }

        public void Reset()
        {
            IsDeleted = false;
            PixelSize = 0d;
            MinImageROI = Rect.Empty;
            MaxImageROI = Rect.Empty;
        }
    }
}