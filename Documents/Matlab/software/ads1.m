% 读取 Excel 数据
filename = 'C:\Users\DELL\Desktop\20250102173739.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

% 提取第二列数据
z1 = T{:, 5};

% 平滑处理
windowSize = 1000; % 移动平均窗口大小
smoothedData = movmean(z1, windowSize);

% 设置过滤条件
minPeakDistance = 5;  % 设置最小峰间距（根据数据的采样频率调整）
minPeakHeight = 0.1;   % 设置最小峰值高度（根据数据的幅度调整）

% 查找平滑数据的极大值和极小值
[peaksMax, locsMax] = findpeaks(smoothedData, 'MinPeakDistance', minPeakDistance, 'MinPeakHeight', minPeakHeight);  % 极大值及其索引
[peaksMax, locsMax] = findpeaks(-smoothedData, 'MinPeakDistance', minPeakDistance, 'MinPeakHeight', minPeakHeight);  % 极大值及其索引

% 合并相邻的极大值点（如果多个极大值点距离太近，只保留一个）
% 使用 `findpeaks` 的 `MinPeakDistance` 参数来合并相邻的极大值点

% 创建一个新的图形窗口
figure;
hold on;

% 绘制原始数据和平滑数据
plot(z1, 'DisplayName', T.Properties.VariableNames{2}, 'Visible', 'on', 'Color', 'black');
plot(smoothedData, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1');  % 平滑数据

% 绘制平滑数据上的极大值
plot(locsMax, peaksMax, 'go', 'MarkerFaceColor', 'g', 'DisplayName', 'Local Maxima');

hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');

% 输出极大值的信息
fprintf('所有极大值点:\n');
for i = 1:length(peaksMax)
    fprintf('极大值: %f, 位置: %d\n', peaksMax(i), locsMax(i));
end
