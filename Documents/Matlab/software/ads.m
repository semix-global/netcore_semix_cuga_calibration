% 读取 Excel 数据
filename = 'C:\Users\DELL\Desktop\20250102173739.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

% 提取第二列数据
z1 = T{:, 4};

% 平滑处理
windowSize = 100; % 移动平均窗口大小
smoothedData = movmean(z1, windowSize);

% 找到最大值和最小值点
[maxValue, maxIndex] = max(z1);  % 最大值及其索引
[minValue, minIndex] = min(z1);  % 最小值及其索引

% 判断趋势：计算整体斜率
% 通过计算第二列数据的线性回归斜率来判断是递增还是递减
p = polyfit(1:length(z1), z1, 1);  % 线性拟合，得到斜率
slope = p(1);  % 斜率

if slope > 0
    trend = '递增';
elseif slope < 0
    trend = '递减';
else
    trend = '平稳';
end

% 创建一个新的图形窗口
figure;
hold on;

% 绘制原始数据和平滑数据
plot(z1, 'DisplayName', T.Properties.VariableNames{2}, 'Visible', 'on', 'Color', 'black');
plot(smoothedData, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1');  % 平滑数据

% 标注最大值和最小值
plot(maxIndex, maxValue, 'go', 'MarkerFaceColor', 'g', 'DisplayName', 'Max Value');
plot(minIndex, minValue, 'ro', 'MarkerFaceColor', 'r', 'DisplayName', 'Min Value');

hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');

% 输出趋势信息
fprintf('曲线的最大值：%f, 最大值所在位置：%d\n', maxValue, maxIndex);
fprintf('曲线的最小值：%f, 最小值所在位置：%d\n', minValue, minIndex);
fprintf('曲线的趋势是：%s\n', trend);
