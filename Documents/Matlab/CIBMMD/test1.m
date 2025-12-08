% 输入你的已知数据
% knownPositions = [15, 89, 238, 567, 1024, 2048, 3096, 4095]; % 序号 // 16384坐标值
% knownValues = {'config_A', 'config_B', 'data_1', 'data_2', 'threshold', 'max_value', 'min_value', 'end_value'}; 

% 确保位置从1开始（MATLAB索引）
if min(knownPositions) == 0
    knownPositions = knownPositions + 1;
end

% 初始化16384个寄存器
totalRegisters = 16384;
registers = repmat("", 1, totalRegisters);

% 设置已知值
registers(knownPositions) = knownValues;

% 从后向前遍历，用后方最邻近已知值填充
nextKnownValue = registers(knownPositions(end)); % 初始化为最后一个已知值
for pos = totalRegisters:-1:1
    if registers(pos) ~= ""
        nextKnownValue = registers(pos);
    else
        registers(pos) = nextKnownValue;
    end
end

% 生成二维数列 [位置, 值]
result = [];
for i = 1:totalRegisters
    result(i, 1) = i - 1; % 位置序号（从0开始）
    result(i, 2) = string(registers(i)); % 值
end
