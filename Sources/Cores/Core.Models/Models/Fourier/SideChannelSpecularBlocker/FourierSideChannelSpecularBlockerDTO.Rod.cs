using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

public sealed partial class FourierSideChannelSpecularBlockerDTO
{
    public sealed partial class Rod(BitmapImageDrawable bitmapImageDrawable) : FourierSideChannelFlexibleApertureDTO.Rod(bitmapImageDrawable), IAdaptIn<Rod, Rod>
    {
        [ObservableProperty]
        public partial double MotorAbsoluteValue { get; set; }

        public Rod AdaptIn(Rod obj)
        {
            base.AdaptIn(obj);

            MotorAbsoluteValue = obj.MotorAbsoluteValue;

            return this;
        }

        public override void Reset()
        {
            base.Reset();

            MotorAbsoluteValue = 0d;
        }
    }
}