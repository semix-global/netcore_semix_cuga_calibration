using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;

public sealed partial class V0FourierCenterChannelSpecularBlockerDTO
{
    public sealed partial class ROI(BitmapImageDrawable bitmapImageDrawable) : FourierPupilCameraAlignmentDTO.ROI(bitmapImageDrawable), IAdaptIn<ROI, ROI>
    {
        [ObservableProperty]
        public partial double MotorAbsoluteValue { get; set; }

        public ROI AdaptIn(ROI obj)
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