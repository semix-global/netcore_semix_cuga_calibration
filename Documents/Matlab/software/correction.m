clc;clearvars;close all;


%x1 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625\x.csv');

%y1 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625\y.csv');

%x2 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625-1\x.csv');

%y2 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625-1\y.csv');

%x3 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230621\x.csv');

%y3 = xlsread('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230621\y.csv');
%x3=[];y3=[];

x4=zeros(18,18);
y4=zeros(18,18);
for i=1:18
    for j=1:18
        x4(i,j)=x1(i,j)+x2(i,j);
        y4(i,j)=y1(i,j)+y2(i,j);
    end
end
figure(1);
surf(x4)
figure(2);
surf(y4)
 fid = fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625-1\x4.txt','w');
 fprintf(fid,[repmat('%5.9f\t', 1, size(x4,2)), '\n'], x4');     
 fclose(fid);
 
 fid = fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230625-1\y4.txt','w');
 fprintf(fid,[repmat('%5.9f\t', 1, size(y4,2)), '\n'], y4');     
 fclose(fid);
