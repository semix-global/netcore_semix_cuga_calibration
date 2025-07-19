[idealX1, idealY1] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703080433\Ideal_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
[errorX1, errorY1] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703080433\Error_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
% [idealX2, idealY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703093659\Ideal_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
% [errorX2, errorY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckBrightFieldStageMapCalibrationViewModel\50X\20250703\Calibration\20250703093659\Error_Guid(51863f70-39ae-4b77-8af9-92e76fcd9a61).csv");
% [idealX2, idealY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250715\Calibration\20250715073256\Ideal_Guid(71b8a148-5d12-4d38-a638-2b7454f92004).csv");
% [errorX2, errorY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250715\Calibration\20250715073256\Error_Guid(71b8a148-5d12-4d38-a638-2b7454f92004).csv");
% [idealX2, idealY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250710\Calibration\20250710083625\Ideal_Guid(2e72a7f0-7d07-4ff3-a498-60f3a18cce9f).csv");
% [errorX2, errorY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250710\Calibration\20250710083625\Error_Guid(2e72a7f0-7d07-4ff3-a498-60f3a18cce9f).csv");
[idealX2, idealY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250717\Calibration\20250717072412\Ideal_Guid(6ec1c943-474e-4519-9dec-3859ea48e91c).csv");
[errorX2, errorY2] = ReadChuckCsvFile1("J:\Nano\Cuga-Calibration\Csv\ChuckStageMapCalibrationViewModel\50X-High\20250717\Calibration\20250717072412\Error_Guid(6ec1c943-474e-4519-9dec-3859ea48e91c).csv");

% 差值
templateX2_1 = errorX2 - errorX1;
templateY2_1 = errorY2 - errorY1;

figure('Name', '3D Error Maps', 'NumberTitle', 'off');

subplot(3, 2, 1);
surf(idealX1, idealY1, errorX1);
title('20250703 errorX1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 2);
surf(idealX1, idealY1, errorY1);
title('20250703 errorY1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 3);
surf(idealX2, idealY2, errorX2);
title('202507117 errorX2');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 4);
surf(idealX2, idealY2, errorY2);
title('202507117 errorY2');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 5);
surf(idealX2, idealY2, templateX2_1);
title('errorX2 - errorX1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;

subplot(3, 2, 6);
surf(idealX2, idealY2, templateY2_1);
title('errorY2 - errorY1');
xlabel('X'); ylabel('Y'); zlabel('Z');
colorbar;
