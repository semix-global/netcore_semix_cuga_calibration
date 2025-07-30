% 读取 Excel 数据
filename = 'C:\Users\DELL\Desktop\20250102173739.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

% 提取第二列数据
z1 = T{:, 5};

% 1. 计算数据的标准差和均值（用于确定峰值高度的阈值）
dataStd = std(z1);
dataMean = mean(z1);

% 2. 自适应设置平滑窗口大小（基于数据标准差）
maxWindowSize = 1000;
adaptiveWindowSize = max(50, round(dataStd * 10));  % 根据标准差动态调整窗口大小，最小窗口50

% 3. 自适应平滑处理
smoothedData = movmean(z1, adaptiveWindowSize);

% 4. 自适应设置最小峰值高度
% 这里使用数据标准差的两倍作为高度阈值，避免极小的波动被误识别
adaptivePeakHeight = 2 * dataStd;

% 5. 自适应设置最小峰间距
% 通过计算数据局部波动性来动态调整最小峰间距
localStd = std(smoothedData);  % 计算平滑数据的标准差，反映数据的波动性
adaptiveMinPeakDistance = round(localStd * 100);  % 根据局部标准差来动态设置峰间距

% 限制最小峰间距的最大值，避免峰间距过大
maxMinPeakDistance = 500;  % 设置一个最大值
adaptiveMinPeakDistance = min(adaptiveMinPeakDistance, maxMinPeakDistance);

% 6. 查找平滑数据的极大值和极小值
[peaksMax, locsMax] = findpeaks(smoothedData, 'MinPeakDistance', 1, 'MinPeakHeight', 0.01);  % 极大值及其索引

% 查找极小值：通过取负值来查找极小值
[peaksMin, locsMin] = findpeaks(-smoothedData, 'MinPeakDistance', 1, 'MinPeakHeight', 0.01);  % 极小值及其索引

% 创建一个新的图形窗口
figure;
hold on;

% 绘制原始数据和平滑数据
plot(z1, 'DisplayName', T.Properties.VariableNames{2}, 'Visible', 'on', 'Color', 'black');
plot(smoothedData, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1');  % 平滑数据

% 绘制平滑数据上的极大值
plot(locsMax, peaksMax, 'go', 'MarkerFaceColor', 'g', 'DisplayName', 'Local Maxima');

% 绘制平滑数据上的极小值
plot(locsMin, -peaksMin, 'ro', 'MarkerFaceColor', 'r', 'DisplayName', 'Local Minima');  % 注意：极小值是取负值

hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');

% 输出极大值和极小值的信息
fprintf('所有极大值点:\n');
for i = 1:length(peaksMax)
    fprintf('极大值: %f, 位置: %d\n', peaksMax(i), locsMax(i));
end

fprintf('\n所有极小值点:\n');
for i = 1:length(peaksMin)
    fprintf('极小值: %f, 位置: %d\n', -peaksMin(i), locsMin(i));  % 还原极小值
end
