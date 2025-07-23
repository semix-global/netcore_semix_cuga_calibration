clc;clearvars;close all;


fid1 = xlsread('C:\Users\Administrator\Desktop\temp.xlsx');

data = fid1(:,2:801);
figure(1)
surf(data);
hold off;

% 获取行数
numRows = size(data, 1);

%colors = ['r', 'g', 'b', 'k']; % 红色、绿色、蓝色、黑色
% 十种颜色
colors = lines(11);
% 绘制10条线，每种颜色一条

% 循环遍历每一行
for i = 1:numRows
    % 取出第i行的数据
    rowData = data(i, :);
 
    % 选择颜色和线型
    %color = colors(mod(i-1, length(colors)) + 1); % 循环选择颜色

    figure(2)
    % 绘制第i行的数据，使用选择的颜色和线型
    plot(rowData, 'Color', colors(i,:), 'MarkerFaceColor', colors(i, :),'DisplayName', [' ' num2str(fid1(i,1))],'LineWidth', 2);
    hold on;
    % 设置图例（可选）
    
    
    % 可以选择在每个循环后暂停，以便查看每个图形（可选）
    % pause;
end
% 设置图例
legend show; % 显示图例

% 设置图形的标题和轴标签（可选）
title('Different Colors and Line Styles for Each Row');
xlabel('Index');
ylabel('Value');