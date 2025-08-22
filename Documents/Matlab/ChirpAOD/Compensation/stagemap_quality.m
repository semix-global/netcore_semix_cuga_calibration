fileNames = ["C:\Users\DELL\Desktop\Test\17.txt",
"C:\Users\DELL\Desktop\Test\16.txt",
"C:\Users\DELL\Desktop\Test\15.txt",
"C:\Users\DELL\Desktop\Test\14.txt",
"C:\Users\DELL\Desktop\Test\13.txt",
"C:\Users\DELL\Desktop\Test\12.txt",
"C:\Users\DELL\Desktop\Test\11.txt",
"C:\Users\DELL\Desktop\Test\10.txt",
"C:\Users\DELL\Desktop\Test\9.txt",
"C:\Users\DELL\Desktop\Test\8.txt",
"C:\Users\DELL\Desktop\Test\7.txt",
"C:\Users\DELL\Desktop\Test\6.txt",
"C:\Users\DELL\Desktop\Test\5.txt",
"C:\Users\DELL\Desktop\Test\4.txt",
"C:\Users\DELL\Desktop\Test\3.txt",
"C:\Users\DELL\Desktop\Test\2.txt",
"C:\Users\DELL\Desktop\Test\1.txt"]

allData = {};  % 存储所有解析后的数组

for i = 1:length(fileNames)
    % 读取文件内容
    fileContent = fileread(fileNames{i});
    
    % 解析字符串为数组（假设格式是 "[x, y, z, ...]"）
    try
        data = jsondecode(fileContent);  % 直接解析 JSON 格式的数组
        allData{end+1} = data(:)';  % 确保是行向量并存储
    catch
        error('Failed to parse file: %s', fileNames{i});
    end
end

% 2. 计算最大长度
maxLen = max(cellfun(@length, allData));

% 3. 居中对齐补零
paddedData = cell(size(allData));
for i = 1:length(allData)
    currentData = allData{i};
    currentLen = length(currentData);
    leftPad = floor((maxLen - currentLen) / 2);
    rightPad = maxLen - currentLen - leftPad;
    
    % 手动补零
    paddedData{i} = [zeros(1, leftPad), currentData, zeros(1, rightPad)];
end

% 4. 转换为矩阵并绘制
dataMatrix = vertcat(paddedData{:});

figure;
%hold on;
for i = 1:17
    plot(dataMatrix(i, :), 'DisplayName', fileNames{i});
end
%hold off;
xlabel('Index');
ylabel('Value');
title('All Data from Files');
legend('show', 'Interpreter', 'none');  % 防止路径中的下划线被解释为 LaTeX
grid on;