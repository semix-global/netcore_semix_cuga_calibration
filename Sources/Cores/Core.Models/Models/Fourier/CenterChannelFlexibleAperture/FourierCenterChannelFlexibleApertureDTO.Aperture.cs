using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class FourierCenterChannelFlexibleApertureDTO
{
    public sealed partial class Aperture(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<Aperture, Aperture>
    {
        [ObservableProperty]
        public partial Circle ImageCircle { get; set; } = Circle.Empty;

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageCircleDrawable BitmapImageCircleDrawable { get; } = new(bitmapImageDrawable);

        [Newtonsoft.Json.JsonConstructor]
        private Aperture() : this(new BitmapImageDrawable())
        {
        }

        public Aperture AdaptIn(Aperture obj)
        {
            ImageCircle = obj.ImageCircle;

            return this;
        }

        public void Reset()
        {
            ImageCircle = Circle.Empty;
        }
    }
}