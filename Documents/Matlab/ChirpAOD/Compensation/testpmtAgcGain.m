% 参数设置
totalPoints = 1080; % 总点数
baseValue = -10; % 基线值
peakPoint = 500; % 峰值点位置
peakValue = 10; % 峰值
transitionWidth = 50; % 过渡宽度(单边)

% 初始化所有点为基线值
points = baseValue * ones(1, totalPoints);

% 设置峰值点
points(peakPoint) = peakValue;

% 计算上升和下降的斜率
slope = (peakValue - baseValue) / transitionWidth;

% 设置上升部分(前50点)
for i = max(1, peakPoint - transitionWidth):peakPoint - 1
    points(i) = baseValue + slope * (i - (peakPoint - transitionWidth));
end

% 设置下降部分(后50点)
for i = peakPoint + 1:min(totalPoints, peakPoint + transitionWidth)
    points(i) = peakValue - slope * (i - peakPoint);
end


% 绘制结果
figure;
plot(points, 'b-', 'LineWidth', 1.5);
hold on;
plot(peakPoint, peakValue, 'ro', 'MarkerSize', 8, 'MarkerFaceColor', 'r'); % 标记峰值点
title('三角峰信号');
xlabel('点索引');
ylabel('幅值');
xlim([1 totalPoints]);
ylim([baseValue - 1 peakValue + 1]);
grid on;
legend('信号', '峰值点');


short = int16(2 ^ 15 * points / 10.0);
short = int64(short);

for i = 1:length(short)

    if short(i) < 0
        short(i) = short(i) + 2 ^ 32;
    end

end

strArray = dec2hex(short, 4);
lastFourChars = strArray(:, end - 3:end);
stringArray = string(lastFourChars);
fileID = fopen('C:\Users\DELL\Desktop\Aod\1.txt', 'wt');

if fileID == -1
    error('File cannot be opened');
end

for i = 1:length(stringArray)

    if i == length(stringArray)
        fprintf(fileID, '%s', stringArray(i));
    else
        fprintf(fileID, '%s\n', stringArray(i));
    end

end

fclose(fileID);
