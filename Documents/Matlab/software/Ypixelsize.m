clc;clearvars;close all;

% 指定文件夹路径
folderPath = 'C:\Users\Administrator\Desktop\Y pixel size';

% 获取文件夹中所有图片文件的详细信息
% 假设图片文件扩展名为.jpg, .png等
files = dir(fullfile(folderPath, '*.jpg')); % 可以根据需要更改文件扩展名

% 初始化一个cell数组来存储图像数据
images = {};
filesname = {};

% 循环读取每个文件
for j = 1:length(files)
    % 读取图片文件
    img = imread(fullfile(folderPath, files(j).name));
    filesname{j} = files(j).name;
    
    I1_double = im2double(img);
    line=zeros;

    for i=1:size(I1_double,2)
        line = line+I1_double(:,i);
    end
    line=line.*-1;

    N = length(line);
    p_data = zeros(1, N);
    arr_rowsum = zeros(1, N);
    
    for k = 1:floor(N/2)
        row_sum = 0;
        for i = k:(N-k)
            if line(i) > line(max(1, i-k)) && line(i) > line(min(N, i+k))
                row_sum = row_sum - 1; % 寻找波峰
            end
        end
        arr_rowsum(k) = row_sum;
    end
    
    [minValue, minIndex] = min(arr_rowsum);
    max_window_length = minIndex;
    
    for k = 1:max_window_length
        for i = k:(N-k)
            if line(i) > line(max(1, i-k)) && line(i) > line(min(N, i+k))
                p_data(i) = p_data(i) + 1;
            end
        end
    end
    peakIndices = find(p_data == max_window_length);


    pixeldistance = zeros(1, length(peakIndices)-1);
    for i=2:length(peakIndices)
        pixeldistance(i-1) = peakIndices(i)-peakIndices(i-1);
    end
    pixeldistanceavg = sum(pixeldistance)/length(pixeldistance);
    pixelsize(j) = 10/pixeldistanceavg;  
    
    figure(j);
    plot(1:800, line, 'r-', 'LineWidth', 1.5);

    hold on;

    points_x = peakIndices;
    points_y = line(points_x);

    % 使用不同颜色和标记绘制每个点
    imgs = plot(points_x', points_y, 'g*', 'MarkerSize', 10);
    % 'r*' 红色星号, 'g+' 绿色加号, 'mo' 品红色圆圈
    % 'MarkerSize' 控制标记大小
    hold off;

    ax = gca;


    % 将数值转换为字符串
    valueStr = num2str(pixelsize(j));
    % 在图像上添加文本
    % 这里我们选择图像的右下角作为文本位置
    x_limits = get(ax, 'XLim');
    y_limits = get(ax, 'YLim');
    textX = x_limits(2)-100;
    textY = y_limits(2)-100;
    text(textX, textY, valueStr, 'Color', 'black', 'FontSize', 12, 'HorizontalAlignment', 'right');
    
    saveas(imgs, fullfile('C:\Users\Administrator\Desktop\Y pixel size1\', filesname{j}));
end


    
    
    