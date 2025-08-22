fileNames = ["C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\0.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\1.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\2.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\3.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\4.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\5.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\6.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\7.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\8.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\9.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\10.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\11.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\12.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\13.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\14.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\15.txt",
             "C:\Users\DELL\Desktop\校准数据\ChuckStagemap\清晰度\B2\20250822\16.txt"]

allData = {};  % 存储所有解析后的数组

for i = 1:length(fileNames)
    try
        data = readmatrix(fileNames{i});  % 直接解析 JSON 格式的数组
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