% 数据
speed = [65, 85, 100, 143, 186, 315, 400];
x1 = [67, 69, 72, 82, 90, 89, 103];
x2 = [22, 20, 30, 37, 30, 53, 58];

% 生成更平滑的 speed 范围用于拟合曲线
speed_fit = linspace(min(speed), max(speed), 100);

%% 对 speed 和 x1 进行二次拟合
p_x1 = polyfit(speed, x1, 2); % 二次拟合系数
x1_fit = polyval(p_x1, speed_fit); % 计算拟合值

% 计算 R² (x1)
y_mean_x1 = mean(x1);
SS_total_x1 = sum((x1 - y_mean_x1) .^ 2);
SS_residual_x1 = sum((x1 - polyval(p_x1, speed)) .^ 2);
R_squared_x1 = 1 - (SS_residual_x1 / SS_total_x1);

% 构造拟合公式字符串（保留10位小数）
eqn_x1 = sprintf('x1 = %.10f*speed² + %.10f*speed + %.10f', p_x1(1), p_x1(2), p_x1(3));

%% 对 speed 和 x2 进行二次拟合
p_x2 = polyfit(speed, x2, 2); % 二次拟合系数
x2_fit = polyval(p_x2, speed_fit); % 计算拟合值

% 计算 R² (x2)
y_mean_x2 = mean(x2);
SS_total_x2 = sum((x2 - y_mean_x2) .^ 2);
SS_residual_x2 = sum((x2 - polyval(p_x2, speed)) .^ 2);
R_squared_x2 = 1 - (SS_residual_x2 / SS_total_x2);

% 构造拟合公式字符串（保留10位小数）
eqn_x2 = sprintf('x2 = %.10f*speed² + %.10f*speed + %.10f', p_x2(1), p_x2(2), p_x2(3));
% Speed 93 二次计算一个
x1_93 = polyval(p_x1, 93);
x2_93 = polyval(p_x2, 93);
% % Speed 93算一个X1

%% 打印拟合公式到控制台（保留10位小数）
fprintf('----- 拟合公式（保留10位小数） -----\n');
fprintf('%s\n', eqn_x1);
fprintf('R² = %.10f\n\n', R_squared_x1);
fprintf('%s\n', eqn_x2);
fprintf('R² = %.10f\n', R_squared_x2);

%% 绘制在同一个图形窗口（上下排列）
figure;

% subplot 1: speed 和 x1 的拟合
subplot(2, 1, 1); % 2行1列，第1个子图
plot(speed, x1, 'o', 'MarkerFaceColor', 'b', 'MarkerEdgeColor', 'k'); % 原始数据
hold on;
plot(speed_fit, x1_fit, 'r-', 'LineWidth', 2); % 拟合曲线
hold off;

title('speed 与 x1 的二次拟合');
xlabel('speed');
ylabel('x1');
legend('原始数据', '拟合曲线', 'Location', 'best');
text(100, 70, {eqn_x1, sprintf('R² = %.5f', R_squared_x1)}, 'FontSize', 10); % 显示公式和R²
grid on;

% subplot 2: speed 和 x2 的拟合
subplot(2, 1, 2); % 2行1列，第2个子图
plot(speed, x2, 'o', 'MarkerFaceColor', 'g', 'MarkerEdgeColor', 'k'); % 原始数据
hold on;
plot(speed_fit, x2_fit, 'm-', 'LineWidth', 2); % 拟合曲线
hold off;

title('speed 与 x2 的二次拟合');
xlabel('speed');
ylabel('x2');
legend('原始数据', '拟合曲线', 'Location', 'best');
text(100, 40, {eqn_x2, sprintf('R² = %.5f', R_squared_x2)}, 'FontSize', 10); % 显示公式和R²
grid on;
