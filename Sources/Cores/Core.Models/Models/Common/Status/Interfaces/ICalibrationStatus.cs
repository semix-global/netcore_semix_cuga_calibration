namespace Core.Models.Models.Common.Status.Interfaces;

public interface ICalibrationStatus<TCalibrationSelectedItem>
{
    TCalibrationSelectedItem SelectedItem { get; set; }
}