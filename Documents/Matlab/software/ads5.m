% 读取 Excel 数据
filename = 'C:\Users\DELL\Desktop\20250102173739.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

% 提取第二列数据
z1 = T{:, 5};

% 1. 设置平滑窗口大小
windowSize = 1000;  % 移动平均窗口大小
smoothedData = movmean(z1, windowSize);  % 使用移动平均平滑数据

% 2. 计算平滑数据的一阶导数（近似）
dz1 = diff(smoothedData);  % 一阶导数（差分）

% 3. 查找导数零交叉点来识别极值
% 极大值：导数从正变负
% 极小值：导数从负变正

% 创建一个标记数组，标记导数零交叉点
isMax = (dz1(1:end-1) > 0) & (dz1(2:end) < 0);  % 导数从正变负，极大值
isMin = (dz1(1:end-1) < 0) & (dz1(2:end) > 0);  % 导数从负变正，极小值

% 4. 获取极大值和极小值的位置
locsMax = find(isMax) + 1;  % 极大值位置
locsMin = find(isMin) + 1;  % 极小值位置

% 获取极大值和极小值的数值
peaksMax = smoothedData(locsMax);  % 极大值的数值
peaksMin = smoothedData(locsMin);  % 极小值的数值

% 5. 设置合并阈值（相邻极值点的最大距离）
mergeThreshold = 50;  % 可以根据实际数据调整该值，单位：数据点间隔

% 6. 合并相近的极大值和极小值
% 合并相近的极大值
mergedMaxLocs = locsMax(1);  % 保留第一个极大值位置
mergedMaxValues = peaksMax(1);  % 保留第一个极大值数值

for i = 2:length(locsMax)
    if locsMax(i) - locsMax(i-1) <= mergeThreshold  % 如果当前极大值与前一个极大值的距离小于阈值
        continue;  % 跳过当前极大值
    else
        mergedMaxLocs = [mergedMaxLocs, locsMax(i)];  % 否则，合并该极大值
        mergedMaxValues = [mergedMaxValues, peaksMax(i)];
    end
end

% 合并相近的极小值
mergedMinLocs = locsMin(1);  % 保留第一个极小值位置
mergedMinValues = peaksMin(1);  % 保留第一个极小值数值

for i = 2:length(locsMin)
    if locsMin(i) - locsMin(i-1) <= mergeThreshold  % 如果当前极小值与前一个极小值的距离小于阈值
        continue;  % 跳过当前极小值
    else
        mergedMinLocs = [mergedMinLocs, locsMin(i)];  % 否则，合并该极小值
        mergedMinValues = [mergedMinValues, peaksMin(i)];
    end
end

% 7. 选出最终的最大值和最小值点
% 对于极大值，选择数值最大的那个点
[finalMaxValue, maxIndex] = max(mergedMaxValues);
finalMaxLoc = mergedMaxLocs(maxIndex);  % 最终最大值的位置

% 对于极小值，选择数值最小的那个点
[finalMinValue, minIndex] = min(mergedMinValues);
finalMinLoc = mergedMinLocs(minIndex);  % 最终最小值的位置

% 8. 创建一个新的图形窗口
figure;
hold on;

% 绘制原始数据和平滑数据
plot(z1, 'DisplayName', T.Properties.VariableNames{2}, 'Visible', 'on', 'Color', 'black');
plot(smoothedData, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data');  % 平滑数据

% 绘制最终的最大值点
plot(finalMaxLoc, finalMaxValue, 'go', 'MarkerFaceColor', 'g', 'DisplayName', 'Final Maxima');

% 绘制最终的最小值点
plot(finalMinLoc, finalMinValue, 'ro', 'MarkerFaceColor', 'r', 'DisplayName', 'Final Minima');

hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');

% 输出最终的极大值和极小值的信息
fprintf('最终极大值点:\n');
fprintf('极大值: %f, 位置: %d\n', finalMaxValue, finalMaxLoc);

fprintf('\n最终极小值点:\n');
fprintf('极小值: %f, 位置: %d\n', finalMinValue, finalMinLoc);
