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
        public partial double AngleMotorAbsoluteValue { get; set; }

        [ObservableProperty]
        public partial double MotorAbsoluteValue { get; set; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        public ROI AdaptIn(ROI obj)
        {
            base.AdaptIn(obj);

            AngleMotorAbsoluteValue = obj.AngleMotorAbsoluteValue;
            MotorAbsoluteValue = obj.MotorAbsoluteValue;
            IsSelected = obj.IsSelected;

            return this;
        }

        public override void Reset()
        {
            base.Reset();

            AngleMotorAbsoluteValue = 0d;
            MotorAbsoluteValue = 0d;
            IsSelected = false;
        }
    }
}