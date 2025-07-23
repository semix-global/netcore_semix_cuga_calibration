% 读取 Excel 数据
filename = 'C:\Users\DELL\Desktop\20250102173739.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

figure;
hold on;
% 绘制原始数据和平滑数据
plot(T{:, 2}, 'DisplayName', T.Properties.VariableNames{2}, 'Visible', 'on', 'Color', 'red');
plot(T{:, 3}, 'DisplayName', T.Properties.VariableNames{3}, 'Visible', 'on', 'Color', 'blue');


hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');

figure;
hold on;
% 绘制原始数据和平滑数据
plot(T{:, 4}, 'DisplayName', T.Properties.VariableNames{4}, 'Visible', 'on', 'Color', 'red');
plot(T{:, 5}, 'DisplayName', T.Properties.VariableNames{5}, 'Visible', 'on', 'Color', 'blue');


hold off;

% 添加图例和标签
legend('show');
xlabel('行号');
ylabel('值');
title('ADS');
