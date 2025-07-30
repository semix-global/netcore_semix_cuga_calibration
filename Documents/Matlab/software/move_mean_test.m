% 生成模拟数据
x = linspace(0, 4*pi, 500); % 时间点
data1 = sin(x) + 0.2 * randn(size(x)); % 红色曲线 (带噪声)

% 平滑处理
windowSize = 20; % 移动平均窗口大小
smoothedData1 = movmean(data1, windowSize);

% 寻找极值点
dy = diff(smoothedData1); % 计算一阶差分
signChangeIdx = find(diff(sign(dy)) ~= 0); % 找到符号变化点（即极值点）

% 检查极值点
extremaX = x(signChangeIdx + 1); % 极值点的 x 坐标
extremaY = smoothedData1(signChangeIdx + 1); % 极值点的 y 值

% % 寻找极小值点
% secondDeriv = diff(dy); % 计算二阶导数
% minExtremaIdx = sign(secondDeriv(signChangeIdx)); % 判断是极小值点
% 
% % 找到极小值点
% minExtremaX = extremaX(minExtremaIdx > 0); % 极小值点的 x 坐标
% minExtremaY = extremaY(minExtremaIdx > 0); % 极小值点的 y 值

% 绘制原始数据与平滑数据
figure;
hold on;
plot(x, data1, 'red', 'DisplayName', 'Original Data 1'); % 原始数据
plot(x, smoothedData1, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1'); % 平滑数据

% 标记极值点和极小值点
scatter(extremaX, extremaY, 50, 'blue', 'filled', 'DisplayName', 'Extrema'); % 标记极值点
% scatter(minExtremaX, minExtremaY, 50, 'green', 'filled', 'DisplayName', 'Min Extrema'); % 标记极小值点

% 连接极小值点
% plot(minExtremaX, minExtremaY, 'go-', 'LineWidth', 2, 'DisplayName', 'Min Extrema Line'); 

legend;
title('Original vs Smoothed Curves with Extrema and Min Extrema');
xlabel('Time');
ylabel('Value');


% 生成模拟数据
x = extremaX; % 时间点
data1 = extremaY; % 红色曲线 (带噪声)

% 寻找极值点
dy = diff(data1); % 计算一阶差分
signChangeIdx = find(diff(sign(dy)) ~= 0); % 找到符号变化点（即极值点）

% 检查极值点
extremaX = x(signChangeIdx + 1); % 极值点的 x 坐标
extremaY = data1(signChangeIdx + 1); % 极值点的 y 值

% % 寻找极小值点
% secondDeriv = diff(dy); % 计算二阶导数
% minExtremaIdx = sign(secondDeriv(signChangeIdx)); % 判断是极小值点
% 
% % 找到极小值点
% minExtremaX = extremaX(minExtremaIdx > 0); % 极小值点的 x 坐标
% minExtremaY = extremaY(minExtremaIdx > 0); % 极小值点的 y 值

% 绘制原始数据与平滑数据
figure;
hold on;
plot(x, data1, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1'); % 平滑数据

% 标记极值点和极小值点
scatter(extremaX, extremaY, 50, 'blue', 'filled', 'DisplayName', 'Extrema'); % 标记极值点
% scatter(minExtremaX, minExtremaY, 50, 'green', 'filled', 'DisplayName', 'Min Extrema'); % 标记极小值点

% 连接极小值点
% plot(minExtremaX, minExtremaY, 'go-', 'LineWidth', 2, 'DisplayName', 'Min Extrema Line'); 

legend;
title('Original vs Smoothed Curves with Extrema and Min Extrema');
xlabel('Time');
ylabel('Value');



% 生成模拟数据
x = extremaX; % 时间点
data1 = extremaY; % 红色曲线 (带噪声)

% 寻找极值点
dy = diff(data1); % 计算一阶差分
signChangeIdx = find(diff(sign(dy)) ~= 0); % 找到符号变化点（即极值点）

% 检查极值点
extremaX = x(signChangeIdx + 1); % 极值点的 x 坐标
extremaY = data1(signChangeIdx + 1); % 极值点的 y 值

% % 寻找极小值点
% secondDeriv = diff(dy); % 计算二阶导数
% minExtremaIdx = sign(secondDeriv(signChangeIdx)); % 判断是极小值点
% 
% % 找到极小值点
% minExtremaX = extremaX(minExtremaIdx > 0); % 极小值点的 x 坐标
% minExtremaY = extremaY(minExtremaIdx > 0); % 极小值点的 y 值

% 绘制原始数据与平滑数据
figure;
hold on;
plot(x, data1, 'r', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1'); % 平滑数据

% 标记极值点和极小值点
scatter(extremaX, extremaY, 50, 'blue', 'filled', 'DisplayName', 'Extrema'); % 标记极值点
% scatter(minExtremaX, minExtremaY, 50, 'green', 'filled', 'DisplayName', 'Min Extrema'); % 标记极小值点

% 连接极小值点
% plot(minExtremaX, minExtremaY, 'go-', 'LineWidth', 2, 'DisplayName', 'Min Extrema Line'); 

legend;
title('Original vs Smoothed Curves with Extrema and Min Extrema');
xlabel('Time');
ylabel('Value');


