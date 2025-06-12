% 读取 Excel 数据
excelData = readtable('RtfcDiagnosis1.xlsx');

% 确保数据是数值格式
XAxisTemperature = excelData.XAxisTemperature;
BF_CalChipAfEcs = excelData.BF_CalChipAfEcs;

% 按 XAxisTemperature 排序
[sortedX, sortIdx] = sort(XAxisTemperature);
sortedBF = BF_CalChipAfEcs(sortIdx);

%% 创建一个大窗体，包含 4 个子图
fig = figure('WindowState', 'maximized');
set(fig, 'Units', 'normalized', 'Position', [0 0 1 1]);
set(groot, 'DefaultAxesFontSize', 12)
set(groot, 'DefaultLineLineWidth', 1.5)

% 图1：X vs BF（带线性拟合）
subplot(1, 2, 1);
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

% 图2：XAxisTemperature 变化趋势
subplot(1, 2, 2);
plot(XAxisTemperature, 'bo-', 'MarkerSize', 6, 'LineWidth', 1);
xlabel('Data Point Index');
ylabel('XAxisTemperature (°C)');
title('XAxisTemperature Trend');
grid on;
