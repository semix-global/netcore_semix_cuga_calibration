clear all;
close all;

PMT=xlsread('C:\Users\Administrator\Desktop\prescan\下降2300上升2800.csv');
PMT1=xlsread('C:\Users\Administrator\Desktop\prescan\原始.csv');
PMT2=xlsread('C:\Users\Administrator\Desktop\prescan\定点 2300.csv')
PMT3=xlsread('C:\Users\Administrator\Desktop\prescan\定点 3200.csv')
PMTdata_process=PMT(2:end,3);
PMTdata_raw=PMT1(2:end,3);
PMTdata_point_1=PMT2(2:end,3);
PMTdata_point_2=PMT3(2:end,3);
figure(1)
plot(1:1000,PMTdata_process);
hold on
plot(1:1000,PMTdata_raw);
hold on;
plot(1:1000,PMTdata_point_1);
hold on;
plot(1:1000,PMTdata_point_2);
