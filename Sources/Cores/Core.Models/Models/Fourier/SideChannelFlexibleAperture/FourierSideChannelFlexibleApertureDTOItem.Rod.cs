using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public partial class Rod(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<Rod, Rod>
    {
        public int Index { get; init; }

        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = false;

        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        [Newtonsoft.Json.JsonConstructor]
        private Rod() : this(new BitmapImageDrawable())
        {
        }

        public Rod AdaptIn(Rod obj)
        {
            IsDeleted = obj.IsDeleted;
            ImageROI = obj.ImageROI;

            return this;
        }

        public virtual void Reset()
        {
            IsDeleted = false;
            ImageROI = Rect.Empty;
        }
    }
}