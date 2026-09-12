using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

public partial class FourierSideChannelSpecularBlockerDTOItem
{
    public sealed partial class Rod(BitmapImageDrawable bitmapImageDrawable) : ObservableObject, IAdaptIn<Rod, Rod>
    {
        public int Index { get; init; }

        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = false;

        [ObservableProperty]
        public partial Rect ImageROI { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public BitmapImageROIDrawable BitmapImageROIDrawable { get; } = new(bitmapImageDrawable);

        [ObservableProperty]
        public partial double MotorAbsoluteValue { get; set; }

        public Rod AdaptIn(Rod obj)
        {
            IsDeleted = obj.IsDeleted;
            ImageROI = obj.ImageROI;
            MotorAbsoluteValue = obj.MotorAbsoluteValue;

            return this;
        }

        public void Reset()
        {
            IsDeleted = false;
            ImageROI = Rect.Empty;
            MotorAbsoluteValue = 0d;
        }
    }
}
