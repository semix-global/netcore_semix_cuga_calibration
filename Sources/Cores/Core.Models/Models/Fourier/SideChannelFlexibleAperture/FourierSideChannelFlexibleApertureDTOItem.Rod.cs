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
        public int Index { get; init; }

        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = true;

        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        public Rod AdaptIn(Rod obj)
        {
            IsDeleted = obj.IsDeleted;
            ImageROI = obj.ImageROI;

            return this;
        }

        public void Reset()
        {
            IsDeleted = true;
            ImageROI = Rect.Empty;
        }
    }
}