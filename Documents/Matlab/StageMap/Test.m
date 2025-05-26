[xx1, yy1] = ReadChuckCsvFile1("Z:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel-Magnification50X\20240731\Ideal_Guid(6d56df9d-f536-4bd4-9878-2eec5727550d).csv");
[xx2, yy2] = ReadChuckCsvFile1("Z:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel-Magnification50X\20240731\Real_Guid(6d56df9d-f536-4bd4-9878-2eec5727550d).csv");
[errorX, errorY] = CalculateChuckStageMapError(xx1, yy1, xx2, yy2);
disp(errorX);
disp(errorY);
