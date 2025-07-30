clear all;
close all;

fileContents = fileread('C:\Users\Administrator\Desktop\2.txt'); % 读取整个文件
lines = strsplit(fileContents, '\n'); % 分割字符串为行
strLines = string(lines);

decArray = zeros(size(strLines));

for i = 1:length(strLines)
    hexStr1 = strLines{i};
    hexStr=hexStr1(1:end-1);

    % 将十六进制转换为无符号的十进制数
    decNum = hex2dec(hexStr);

    % 检查符号位
    if decNum > 2^15
        decNum = decNum - 2^16; % 如果是负数，进行调整
    end

    decArray(i) = decNum;
end
decArray=decArray./(32768);

plot(decArray);



