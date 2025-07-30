clear all;
close all;

% 指定Excel文件路径
filename = 'C:\Users\Administrator\Desktop\1.csv'; % 请替换为你的Excel文件名

% 读取Excel文件中的数据
% 假设我们读取第一个工作表中的数据
data = readtable(filename);

% 提取需要的数据列
% 假设我们需要绘制的数据在第一列和第二列
x = data.Var1*120; % 第一列数据作为x轴
y = (data.Var2-0.5)*1.1; % 第二列数据作为y轴

% 绘制折线图
plot(x, y,'-o','linewidth',2); % '-o' 表示折线图，并且数据点用圆圈标记
title('Excel Data Plot'); % 图表标题
xlabel('X Axis Label'); % x轴标签
ylabel('Y Axis Label'); % y轴标签
grid on; % 显示网格