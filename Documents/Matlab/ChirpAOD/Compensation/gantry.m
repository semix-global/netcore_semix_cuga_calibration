count = 123;
rangeX = [-333333, 343444];
rangeY = [-335333, 343544];

[realX, realY] = generateRandomLinePoints(count, rangeX, rangeY);

% 随机产生0，100内数字

% 要修正的点，全在 x=3 垂直线上
idealX = randi(rangeX) * ones(1, count);
idealY = realY;

% 拟合原始线斜率
p = polyfit(realY, realX, 1);
k = p(1);
b = p(2);

idealX_new = idealX + (idealY - idealY(floor((length(idealY) + 1) / 2))) * k;
idealy_new = idealY;

% 可视化
figure; hold on; grid on;

% 原始点和拟合线
plot(realX, realY, 'bo-', 'LineWidth', 1.5, 'DisplayName', 'real');

y_fit = linspace(min(realY) - 1, max(realY) + 1, 100);
x_fit = k * y_fit + b;

plot(x_fit, y_fit, 'b--', 'DisplayName', '原始拟合线');

% 修正前点
plot(idealX, idealY, 'rx--', 'LineWidth', 1.5, 'DisplayName', 'ideal');

% 修正后点
plot(idealX_new, idealy_new, 'g*--', 'LineWidth', 1.5, 'DisplayName', 'ideal_new');

legend('Location', 'northwest');
xlabel('X');
ylabel('Y');
title('修正 x 值：使斜率一致，中心点固定');

axis equal;

function [x, y] = generateRandomLinePoints(n, xRange, yRange)
    slope = (rand * 20 - 10);
    intercept = rand * (yRange(2) - yRange(1)) + yRange(1); % 截距：落在 y 范围内

    x = sort(rand(1, n) * (xRange(2) - xRange(1)) + xRange(1));

    y = slope * x + intercept;
end
