namespace Core.Models.Models.Common.Status.Interfaces;

public interface IStatus<TCalibrationSelectedItem>
{
    TCalibrationSelectedItem SelectedItem { get; set; }
}