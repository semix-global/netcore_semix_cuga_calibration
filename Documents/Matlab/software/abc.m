clear all;
close all;

fileContents = load('C:\Users\Administrator\Desktop\3.txt'); % 读取整个文件
x=619:4270;
y=fileContents(619:4270)';
windowSize = 231; % 定义窗口大小
smoothedY = movmean(y, windowSize);
%p = polyfit(x, y, 6); % 2 表示二次多项式
figure;
plot(x, y, 'o'); % 绘制原始数据点
hold on; % 保持图像，以便在同一图上绘制拟合曲线
%x_fit = linspace(min(x), max(x), 100); % 生成更多的 x 值以平滑曲线
%y_fit = polyval(p, x_fit); % 使用拟合的多项式计算 y 值
plot(x, smoothedY, '-'); % 绘制拟合曲线
legend('Data Points', 'Fitted Curve'); % 添加图例
title('Polynomial Fit');
xlabel('x');
ylabel('y');
