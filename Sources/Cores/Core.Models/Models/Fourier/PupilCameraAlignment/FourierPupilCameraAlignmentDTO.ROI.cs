using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.PupilCameraAlignment;

public sealed partial class FourierPupilCameraAlignmentDTO
{
    public partial class ROI(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<ROI, ROI>
    {
        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        [Newtonsoft.Json.JsonConstructor]
        private ROI() : this(new BitmapImageDrawable())
        {
        }

        public ROI AdaptIn(ROI obj)
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