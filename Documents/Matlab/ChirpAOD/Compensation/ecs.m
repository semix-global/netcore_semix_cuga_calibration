% 读取 Excel 数据
excelData = readtable('RtfcDiagnosisB3-2.xlsx');

% 确保数据是数值格式
XAxisTemperature = str2double(excelData.XAxisTemperature);
YAxisTemperature = str2double(excelData.YAxisTemperature);
BF_CalChipAfEcs = excelData.BF_CalChipAfEcs;

% 按 XAxisTemperature 排序
[sortedX, sortIdx] = sort(XAxisTemperature);
[sortedY, sortIdY] = sort(YAxisTemperature(sortIdx));
sortedBF = BF_CalChipAfEcs(sortIdx);
sortedBFY = BF_CalChipAfEcs(sortIdx); % 去除 NaN 值

%% 创建一个大窗体，包含 4 个子图
fig = figure('WindowState', 'maximized');
set(fig, 'Units', 'normalized', 'Position', [0 0 1 1]);
set(groot, 'DefaultAxesFontSize', 12)
set(groot, 'DefaultLineLineWidth', 1.5)

% 图1：X vs BF（带线性拟合）
subplot(2, 2, 1);
plot(sortedX, sortedBF, 'ro', 'MarkerSize', 6, 'DisplayName', 'Data');
hold on;
p1 = polyfit(sortedX, sortedBF, 1);
fitX = linspace(min(sortedX), max(sortedX), 100);
fitBF = polyval(p1, fitX);
R1 = corrcoef(sortedX, sortedBF);
R1 = R1(1, 2);
plot(fitX, fitBF, 'b-', 'LineWidth', 1.5, 'DisplayName', ...
    sprintf('Fit: y = %.4fx + %.4f\nR = %.4f', p1(1), p1(2), R1));
xlabel('XAxisTemperature (°C)');
ylabel('BF\_CalChipAfEcs');
title('X vs BF (Linear Fit)');
legend('show', 'Location', 'best');
grid on;
hold off;

% 图2：Y vs BF（带线性拟合）
subplot(2, 2, 2);
plot(sortedY, sortedBFY, 'ro', 'MarkerSize', 6, 'DisplayName', 'Data');
hold on;
p2 = polyfit(sortedY, sortedBFY, 1);
fitY = linspace(min(sortedY), max(sortedY), 100);
fitBF_Y = polyval(p2, fitY);
R2 = corrcoef(sortedY, sortedBFY);
R2 = R2(1, 2);
plot(fitY, fitBF_Y, 'b-', 'LineWidth', 1.5, 'DisplayName', ...
    sprintf('Fit: y = %.4fx + %.4f\nR = %.4f', p2(1), p2(2), R2));
xlabel('YAxisTemperature (°C)');
ylabel('BF\_CalChipAfEcs');
title('Y vs BF (Linear Fit)');
legend('show', 'Location', 'best');
grid on;
hold off;

% 图3：XAxisTemperature 变化趋势
subplot(2, 2, 3);
plot(XAxisTemperature, 'bo-', 'MarkerSize', 6, 'LineWidth', 1);
xlabel('Data Point Index');
ylabel('XAxisTemperature (°C)');
title('XAxisTemperature Trend');
grid on;

% 图4：YAxisTemperature 变化趋势
subplot(2, 2, 4);
plot(YAxisTemperature, 'ro-', 'MarkerSize', 6, 'LineWidth', 1);
xlabel('Data Point Index');
ylabel('YAxisTemperature (°C)');
title('YAxisTemperature Trend');
grid on;
