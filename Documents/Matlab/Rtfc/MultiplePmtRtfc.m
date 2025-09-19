% 读取Excel文件（指定工作表）
filename = 'E:\校准项目\多光斑RTFC\0813\rtfc_2025_08_13_MutiPmt.xlsx'; % 替换为你的Excel文件名
%sheetName = 'Sheet1'; % 替换为你的工作表名称，或者使用数字索引

% 方法2：使用工作表索引（例如第2个工作表）
 data = readtable(filename, 'Sheet', 1);

% 提取时间列并转换为datetime格式
time = datetime(data.time, 'InputFormat', 'yyyy/M/d HH:mm');

% 提取需要绘制的数据列
pmt = data.pmt;
calchipType = data.type;
ch1_offset = data.CH1Offset;
ch2_offset = data.CH2Offset;
ch3_offset = data.CH3Offset;
ch1_ecs = data.CH1ECS;
ch2_ecs = data.CH2ECS;
ch3_ecs = data.CH3ECS;

% 分割
start_indices=find(pmt==13);
group_length=start_indices(2)-start_indices(1);
groups = {};  % 存储所有分割后的组
valid_groups = 0;  % 记录有效组数

for i = 1:length(start_indices)
    start_idx = start_indices(i);
    end_idx = start_idx + group_length - 1;
    
    % 检查是否越界
    if end_idx > height(data)
        continue;  % 跳过不完整的组
    end
    
    % 提取当前组的数据
    current_group = data(start_idx:end_idx, :);
    
    % 检查是否符合 13→3 的递减模式
    expected_pmt = 13:-1:3;
    if isequal(current_group.pmt', expected_pmt)
        valid_groups = valid_groups + 1;
        groups{valid_groups} = current_group;  % 存储有效组
    end
end

% 绘图
% 创建三个图形窗口
fig1 = figure('Name', 'ch1Offset vs pmtID', 'Position', [100 100 800 600]);
fig2 = figure('Name', 'ch2Offset vs pmtID', 'Position', [300 200 800 600]);
fig3 = figure('Name', 'ch3Offset vs pmtID', 'Position', [500 300 800 600]);

% 定义通用样式
colors = lines(length(groups)); % 使用lines颜色图生成不同颜色
line_styles = {'-', '--', ':', '-.'}; % 不同线型
marker_types = {'o', 's', 'd', '^', 'v'}; % 不同标记类型

% ch1offset
figure(fig1);
hold on;

for i = 1:length(groups)
    plot(groups{i}.pmt, groups{i}.CH1Offset, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 2, ...
        'Marker', marker_types{mod(i-1, length(marker_types))+1}, ...
        'MarkerSize', 8, ...
        'MarkerFaceColor', colors(i,:), ...
        'DisplayName', ['组 ', num2str(i)]);

    % 线性拟合
    p = polyfit(groups{i}.pmt, groups{i}.CH1Offset, 1);
    fit_y = polyval(p, groups{i}.pmt);
    
    % 绘制拟合线
    plot(groups{i}.pmt, fit_y, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 1.5, ...
        'DisplayName', sprintf('组 %d 拟合: y=%.3fx+%.3f', i, p(1), p(2)));
end

title('ch1Offset vs pmtID', 'FontSize', 14);
xlabel('pmtID', 'FontSize', 12);
ylabel('ch1Offset', 'FontSize', 12);
lgd1=legend('show', 'Location', 'best');
grid on;
set(gca, 'FontSize', 11, 'LineWidth', 1.5);
hold off;

% ch2offset
figure(fig2);
hold on;

for i = 1:length(groups)
    plot(groups{i}.pmt, groups{i}.CH2Offset, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 2, ...
        'Marker', marker_types{mod(i-1, length(marker_types))+1}, ...
        'MarkerSize', 8, ...
        'MarkerFaceColor', colors(i,:), ...
        'DisplayName', ['组 ', num2str(i)]);

      % 线性拟合
    p = polyfit(groups{i}.pmt, groups{i}.CH2Offset, 1);
    fit_y = polyval(p, groups{i}.pmt);
    
    % 绘制拟合线
    plot(groups{i}.pmt, fit_y, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 1.5, ...
        'DisplayName', sprintf('组 %d 拟合: y=%.3fx+%.3f', i, p(1), p(2)));
end

title('ch2Offset vs pmtID', 'FontSize', 14);
xlabel('pmtID', 'FontSize', 12);
ylabel('ch2Offset', 'FontSize', 12);
lgd2=legend('show', 'Location', 'best');
grid on;
set(gca, 'FontSize', 11, 'LineWidth', 1.5);
hold off;

% ch3offset
figure(fig3);
hold on;

for i = 1:length(groups)
    plot(groups{i}.pmt, groups{i}.CH3Offset, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 2, ...
        'Marker', marker_types{mod(i-1, length(marker_types))+1}, ...
        'MarkerSize', 8, ...
        'MarkerFaceColor', colors(i,:), ...
        'DisplayName', ['组 ', num2str(i)]);

      % 线性拟合
    p = polyfit(groups{i}.pmt, groups{i}.CH3Offset, 1);
    fit_y = polyval(p, groups{i}.pmt);
    
    % 绘制拟合线
    plot(groups{i}.pmt, fit_y, ...
        'Color', colors(i,:), ...
        'LineStyle', line_styles{mod(i-1, length(line_styles))+1}, ...
        'LineWidth', 1.5, ...
        'DisplayName', sprintf('组 %d 拟合: y=%.3fx+%.3f', i, p(1), p(2)));
end

title('ch3Offset vs pmtID', 'FontSize', 14);
xlabel('pmtID', 'FontSize', 12);
ylabel('ch3Offset', 'FontSize', 12);
lgd3=legend('show', 'Location', 'best');
grid on;
set(gca, 'FontSize', 11, 'LineWidth', 1.5);
hold off;

% 轴标签增加勾选隐藏图例项
set(lgd1, 'ItemHitFcn', @(src,event)toggleVisibility(src,event));
set(lgd2, 'ItemHitFcn', @(src,event)toggleVisibility(src,event));
set(lgd3, 'ItemHitFcn', @(src,event)toggleVisibility(src,event));
% 将三个窗口并排显示
figure(fig1);
figure(fig2);
figure(fig3);



% 通用隐藏图例项回调函数
function toggleVisibility(~, event)
    % 获取被点击的图例项对应的图形对象
    hLine = event.Peer;
    
    % 切换可见性
    if strcmp(hLine.Visible, 'on')
        hLine.Visible = 'off';
        
        % 如果是拟合线，隐藏对应的置信区间
        tags = hLine.Tag;
        if contains(tags, 'fit')
            % 查找相关的置信区间对象并隐藏
            ax = ancestor(hLine, 'axes');
            allLines = findobj(ax, 'Type', 'line');
            for i = 1:length(allLines)
                if contains(allLines(i).Tag, [tags(1:end-3) 'conf'])
                    allLines(i).Visible = 'off';
                end
            end
        end
    else
        hLine.Visible = 'on';
        
        % 如果是拟合线，显示对应的置信区间
        tags = hLine.Tag;
        if contains(tags, 'fit')
            % 查找相关的置信区间对象并显示
            ax = ancestor(hLine, 'axes');
            allLines = findobj(ax, 'Type', 'line');
            for i = 1:length(allLines)
                if contains(allLines(i).Tag, [tags(1:end-3) 'conf'])
                    allLines(i).Visible = 'on';
                end
            end
        end
    end
end