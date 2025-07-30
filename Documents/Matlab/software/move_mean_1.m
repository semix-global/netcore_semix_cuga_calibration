% 生成模拟数据
x = linspace(0, 4*pi, 500); % 时间点
data1 = sin(x) + 0.2 * randn(size(x)); % 红色曲线 (带噪声)

% 平滑处理
windowSize = 20; % 移动平均窗口大小
smoothedData1 = movmean(data1, windowSize);

% 使用希尔伯特变换计算包络线
analyticSignal = hilbert(smoothedData1);
envelope = abs(analyticSignal); % 包络线

% 寻找包络线的极值点
[peaks, locsP] = findpeaks(envelope); % 寻找波峰（包络线的局部最大值）
[valleys, locsV] = findpeaks(-envelope); % 寻找波谷（包络线的局部最小值，即负包络的最大值）

% 获取波峰和波谷的位置
peakX = x(locsP); % 波峰的 x 坐标
peakY = smoothedData1(locsP); % 波峰的 y 坐标

valleyX = x(locsV); % 波谷的 x 坐标
valleyY = smoothedData1(locsV); % 波谷的 y 坐标

% 绘制原始数据、平滑数据、波峰、波谷和包络线
figure;
hold on;
plot(x, data1, 'r', 'DisplayName', 'Original Data 1'); % 原始数据
plot(x, smoothedData1, 'b', 'LineWidth', 1.5, 'DisplayName', 'Smoothed Data 1'); % 平滑数据
plot(x, envelope, 'g--', 'DisplayName', 'Envelope'); % 包络线

% 标记波峰和波谷
scatter(peakX, peakY, 100, 'filled', 'MarkerFaceColor', 'red', 'DisplayName', 'Peaks');
scatter(valleyX, valleyY, 100, 'filled', 'MarkerFaceColor', 'blue', 'DisplayName', 'Valleys');

legend;
title('Original, Smoothed Curves and Envelope with Peaks and Valleys');
xlabel('Time');
ylabel('Amplitude');
