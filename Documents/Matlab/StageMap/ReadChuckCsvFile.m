function [xArray, yArray] = ReadChuckCsvFile(file)

    % 打开文件
    fid = fopen(file, 'r');

    % 检查文件是否成功打开
    if fid == -1
        error('Failed to open file.');
    end

    % 读取整个文件到 cell 数组中, 按照换行获取
    data = textscan(fid, '%s', 'Delimiter', '\n');
    lines = data{1};

    % 关闭文件
    fclose(fid);

    % 获取行数和列数
    num_rows = length(lines);
    first_line = strrep(lines{1}, '(', '');
    first_line = strrep(first_line, ')', '');
    num_cols = length(strsplit(first_line, ',')) / 2;

    % 初始化XArray和YArray
    xArray = zeros(num_rows, num_cols);
    yArray = zeros(num_rows, num_cols);

    % 解析每一行的数据
    for row = 1:num_rows
        % 去掉括号并按逗号分割
        line = strrep(lines{row}, '(', '');
        line = strrep(line, ')', '');
        data = strsplit(line, ',');

        % 将字符串数据转换为数字
        for col = 1:num_cols
            x = str2double(data{2 * col - 1});
            y = str2double(data{2 * col});
            xArray(row, col) = x;
            yArray(row, col) = y;
        end

    end

end
