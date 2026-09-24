using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class FourierCenterChannelSpecularBlockerDTO
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

            MotorAbsoluteValue = obj.MotorAbsoluteValue;
            AngleMotorAbsoluteValue = obj.AngleMotorAbsoluteValue;
            IsSelected = obj.IsSelected;

            return this;
        }

        public override void Reset()
        {
            base.Reset();

            MotorAbsoluteValue = 0d;
            AngleMotorAbsoluteValue = 0d;
            IsSelected = false;
        }
    }
}