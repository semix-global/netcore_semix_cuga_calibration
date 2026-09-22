using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class FourierCenterChannelFlexibleApertureDTO
{
    public partial class FindItem(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<FindItem, FindItem>
    {
        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        [Newtonsoft.Json.JsonConstructor]
        private FindItem() : this(new BitmapImageDrawable())
        {
        }

        public FindItem AdaptIn(FindItem obj)
        {
            ImageROI = obj.ImageROI;

            return this;
        }

        public virtual void Reset()
        {
            ImageROI = Rect.Empty;
        }
    }
}