namespace Core.Models.Models.Common.Cookies;

public interface ICalibrationViewModelCookie<out TCache, out TDTO>
    where TCache : CalibrationCacheBase
    where TDTO : CalibrationDTOBase
{
    bool IsArray { get; }

    TCache Cache { get; }

    TDTO Calibration { get; }

    TDTO[] Calibrations { get; }
}