[idealX1, idealY1] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703080433\Ideal_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
[errorX1, errorY1] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703080433\Error_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
[idealX2, idealY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703093659\Ideal_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
[errorX2, errorY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703093659\Error_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");

% 差值
templateX2_1 = errorX2 - errorX1;
templateY2_1 = errorY2 - errorY1;

figure('Name', '3D Error Maps', 'NumberTitle', 'off');

subplot(3, 2, 1);
surf(errorX1);
title('errorX1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 2);
surf(errorY1);
title('errorY1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 3);
surf(errorX2);
title('errorX2');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 4);
surf(errorY2);
title('errorY2');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 5);
surf(templateX2_1);
title('errorX2 - errorX1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 6);
surf(templateY2_1);
title('errorY2 - errorY1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;
