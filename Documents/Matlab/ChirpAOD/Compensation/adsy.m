% 数据
speed = [75, 100, 150, 200];
y1 = [75, 75, 112, 112];
y2 = [75, 75, 112, 112];
y3 = [75, 75, 112, 112];

% 生成更平滑的 speed 范围用于拟合曲线
speed_fit = linspace(min(speed), max(speed), 100);

%% 对 speed 和 y1 进行二次拟合
p_y1 = polyfit(speed, y1, 2); % 二次拟合系数
y1_fit = polyval(p_y1, speed_fit); % 计算拟合值

% 计算 R² (y1)
y_mean_y1 = mean(y1);
SS_total_y1 = sum((y1 - y_mean_y1) .^ 2);
SS_residual_y1 = sum((y1 - polyval(p_y1, speed)) .^ 2);
R_squared_y1 = 1 - (SS_residual_y1 / SS_total_y1);

% 构造拟合公式字符串（保留10位小数）
eqn_y1 = sprintf('y1 = %.10f*speed² + %.10f*speed + %.10f', p_y1(1), p_y1(2), p_y1(3));

%% 对 speed 和 y2 进行二次拟合
p_y2 = polyfit(speed, y2, 2); % 二次拟合系数
y2_fit = polyval(p_y2, speed_fit); % 计算拟合值

% 计算 R² (y2)
y_mean_y2 = mean(y2);
SS_total_y2 = sum((y2 - y_mean_y2) .^ 2);
SS_residual_y2 = sum((y2 - polyval(p_y2, speed)) .^ 2);
R_squared_y2 = 1 - (SS_residual_y2 / SS_total_y2);

% 构造拟合公式字符串（保留10位小数）
eqn_y2 = sprintf('y2 = %.10f*speed² + %.10f*speed + %.10f', p_y2(1), p_y2(2), p_y2(3));

%% 对 speed 和 y3 进行二次拟合
p_y3 = polyfit(speed, y3, 2); % 二次拟合系数
y3_fit = polyval(p_y3, speed_fit); % 计算拟合值

% 计算 R² (y3)
y_mean_y3 = mean(y3);
SS_total_y3 = sum((y3 - y_mean_y3) .^ 2);
SS_residual_y3 = sum((y3 - polyval(p_y3, speed)) .^ 2);
R_squared_y3 = 1 - (SS_residual_y3 / SS_total_y3);

% 构造拟合公式字符串（保留10位小数）
eqn_y3 = sprintf('y3 = %.10f*speed² + %.10f*speed + %.10f', p_y3(1), p_y3(2), p_y3(3));

%% 打印拟合公式到控制台（保留10位小数）
fprintf('----- 拟合公式（保留10位小数） -----\n');
fprintf('%s\n', eqn_y1);
fprintf('R² = %.10f\n\n', R_squared_y1);
fprintf('%s\n', eqn_y2);
fprintf('R² = %.10f\n\n', R_squared_y2);
fprintf('%s\n', eqn_y3);
fprintf('R² = %.10f\n', R_squared_y3);

%% 绘制在同一个图形窗口（上下排列）
figure;

% subplot 1: speed 和 y1 的拟合
subplot(3, 1, 1); % 3行1列，第1个子图
plot(speed, y1, 'o', 'MarkerFaceColor', 'b', 'MarkerEdgeColor', 'k'); % 原始数据
hold on;
plot(speed_fit, y1_fit, 'r-', 'LineWidth', 2); % 拟合曲线
hold off;

title('speed 与 y1 的二次拟合');
xlabel('speed');
ylabel('y1');
legend('原始数据', '拟合曲线', 'Location', 'best');
text(120, 50, {eqn_y1, sprintf('R² = %.5f', R_squared_y1)}, 'FontSize', 10); % 显示公式和R²
grid on;

% subplot 2: speed 和 y2 的拟合
subplot(3, 1, 2); % 3行1列，第2个子图
plot(speed, y2, 'o', 'MarkerFaceColor', 'g', 'MarkerEdgeColor', 'k'); % 原始数据
hold on;
plot(speed_fit, y2_fit, 'm-', 'LineWidth', 2); % 拟合曲线
hold off;

title('speed 与 y2 的二次拟合');
xlabel('speed');
ylabel('y2');
legend('原始数据', '拟合曲线', 'Location', 'best');
text(120, 50, {eqn_y2, sprintf('R² = %.5f', R_squared_y2)}, 'FontSize', 10); % 显示公式和R²
grid on;

% subplot 3: speed 和 y3 的拟合
subplot(3, 1, 3); % 3行1列，第3个子图
plot(speed, y3, 'o', 'MarkerFaceColor', 'c', 'MarkerEdgeColor', 'k'); % 原始数据
hold on;
plot(speed_fit, y3_fit, 'b-', 'LineWidth', 2); % 拟合曲线
hold off;

title('speed 与 y3 的二次拟合');
xlabel('speed');
ylabel('y3');
legend('原始数据', '拟合曲线', 'Location', 'best');
text(120, 120, {eqn_y3, sprintf('R² = %.5f', R_squared_y3)}, 'FontSize', 10); % 显示公式和R²
grid on;
