clc;clearvars;close all;

% 打开文件
fileID = fopen('C:\Users\Administrator\Desktop\swath_12_116\ymodem_1.bin','r');
dataType = 'int16';
dataArray = fread(fileID, dataType);
% 关闭文件
fclose(fileID);
 
% 显示数据
% disp(dataArray);


figure(1)
plot(dataArray);

a = dataArray(1000:1570);

figure(2)
plot(a,'linewidth',1.5);

data = a;
span = 11; % 窗口大小
polyorder = 3 % 多项式的阶数
smoothed_data = sgolayfilt(data, polyorder, span);
smoothed_data = 0.4*smoothed_data;
hold on;

plot(0:570,smoothed_data,'lineWidth',3);
hold off;


% figure(3)
% plot(b);
% 
% for i=1:10
%     for j=1:circle_time-1
%         c(j*i)=dataArray(406 + j*1)- b(j);
%     end
% end
% 
% figure(4)
% plot(c);