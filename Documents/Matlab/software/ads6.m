% 读取 CSV 数据
filename = 'C:\Users\DELL\Desktop\20250103153137.csv';  % 替换为你的文件名
T = readtable(filename, 'VariableNamingRule', 'preserve');  % 读取 CSV 数据

[numRows, numCols] = size(T);

for col = 2:numCols
    % 提取数据列（根据需要修改列索引）
    z1 = T{:, col};
    
    % 1. 平滑处理
    windowSize = 100;  % 移动平均窗口大小
    smoothedData = movmean(z1, windowSize);  % 平滑数据
    

    
    % 3. 五次多项式拟合
    p5 = polyfit(1:length(smoothedData), smoothedData', 5);  % 五次多项式拟合
    y5 = polyval(p5, 1:length(smoothedData));  % 生成五次多项式曲线

    yConst = 2500;  % 常数值
    diffData = y5 - yConst;  % 计算曲线与2500之间的差值
    
    % 使用梯形法进行积分，注意这里计算的是差值的积分
    area = trapz(diffData);  % 数值积分

    if area > 0  
        % 先找五次多项式的最小值，再找最大值
        [maxValue, maxIndex] = max(y5);  % 找到最大值及其索引
        [minValue, minIndex] = min(y5(1:maxIndex));  % 在最大值前面找到最小值
        openingDirection = sprintf('开口向下 %.2f', area);
    else 
        % 先找五次多项式的最大值，再找最小值
        [minValue, minIndex] = min(y5);  % 找到最小值及其索引
        [maxValue, maxIndex] = max(y5(1:minIndex));  % 在最小值前面找到最大值
        openingDirection = sprintf('开口向上 %.2f', area);
    end
    
    % 5. 找到原始数据对应的最大值和最小值
    maxOriginalValue = z1(maxIndex);  % 在原始数据中找到最大值
    minOriginalValue = z1(minIndex);  % 在原始数据中找到最小值
    
    % 6. 绘制结果
    figure;
    hold on;
    
    plot(z1, 'DisplayName', T.Properties.VariableNames{col}, 'Visible', 'on', 'Color', 'black');
    % 绘制平滑数据和拟合曲线
    plot(smoothedData, 'b', 'DisplayName', 'Smoothed Data');
    
    % 绘制五次拟合曲线
    plot(1:length(smoothedData), y5, 'r--', 'LineWidth', 1.5, 'DisplayName',  ['5 Fit(' openingDirection ')']);
    
    % 绘制拟合曲线的最大值点
    plot(maxIndex, maxValue, 'go', 'MarkerFaceColor', 'g', 'DisplayName', 'Maximum Value (Fit)');
    
    % 绘制拟合曲线的最小值点
    plot(minIndex, minValue, 'ro', 'MarkerFaceColor', 'r', 'DisplayName', 'Minimum Value (Fit)');
    
    % 绘制原始数据的最大值点
    plot(maxIndex, maxOriginalValue, 'bo', 'MarkerFaceColor', 'b', 'DisplayName', 'Maximum Value (Original)');
    
    % 绘制原始数据的最小值点
    plot(minIndex, minOriginalValue, 'mo', 'MarkerFaceColor', 'm', 'DisplayName', 'Minimum Value (Original)');
    
    hold off;
    
    % 添加图例和标签
    legend('show');
    xlabel('Index');
    ylabel('Value');
    title('Polynomial Fits, Maximum and Minimum Values');
    
    % 输出最大值和最小值的信息
    fprintf('拟合曲线最大值: %f, 位置: %d\n', maxValue, maxIndex);
    fprintf('拟合曲线最小值: %f, 位置: %d\n', minValue, minIndex);
    fprintf('原始数据最大值: %f, 位置: %d\n', maxOriginalValue, maxIndex);
    fprintf('原始数据最小值: %f, 位置: %d\n', minOriginalValue, minIndex);
end
