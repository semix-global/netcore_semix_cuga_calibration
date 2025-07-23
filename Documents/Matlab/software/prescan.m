% Prescan AOD  线性递增的扫频信号 Tef=4.18us,T=4.75us,270-150MHz
%note:(1)补偿函数不能有负值，offset；（2）补偿函数长度要与波形点数一致;(3)去掉开头或结尾的若干点，拟合结果更好
clear all;
close all;
% k=(260-180)/3100;%MHz,ns
k=(96)/(4180);%MHz,ns
% Fs=1000;%Fs,M
Fs=1064;%Fs,M；采样率
N=round(4180.*Fs/1000+400);
t=(0:N-1)';%ns
% f=110+k/2*t;%递增Chirp
f=258-k/2*t;%递减prescan-,递增prescan+

%读取Haze profile.............................
filePath = 'path_to_your_excel_file.xlsx';

% 读取Excel文件
T = readtable('C:\Users\Administrator\Desktop\校准\haze.csv');

% 读取第columnIndex列的数据
profile = T{:, 3};

profileprocess = (profile-min(profile))/4096; 

for i=0:799
    for j=1:5
        Amp(i*5+j)=1-profileprocess(i+1);
    end
    
end
%............................................
Amp1=1;
figure
plot(Amp)
title('Haze profile')
%画原始信号..................................
Ts=1/Fs;
sig=cos(2*pi*f.*t/1000);
%sig1=cos(2*pi*f.*t/1000).*25.*Amp.^-0.5;
sig1=cos(2*pi*f.*t/1000).*Amp1.^-1;
% sig1(N-40:N)=0;%test
figure
subplot(211)
plot(t,sig);
title('sig(原始信号)');
xlabel('N');
ylabel('Amp');
subplot(212)
plot(t,sig1);
title('sig1（sig除以幅频响应函数）');
xlabel('N');
ylabel('Amp');



%加窗信号
Arr=ones(1,N);
n=300;%可调节参数
Arr(1:n)=-sin((-2*pi/(4*n))*(1:n));%自定义函数：正弦函数
Arr(N-n+1:N)=-sin((-2*pi/(4*n))*(n:-1:1));

sig2=sig1.*Arr';
y = int16(32768*sig2);
figure
subplot(211)
plot(t,sig2);
title('sig2(sig1乘以窗函数)');
xlabel('ns');
ylabel('Amp');
subplot(212)
Freq=linspace(-Fs/2,Fs/2,N);
p2=fftshift(abs(fft(sig2)));

plot(Freq,p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on


FileName='PRESCAN_4750_bu.txt';
dlmwrite(FileName,y,'delimiter' , '\n' , 'precision', 6)%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
type(FileName)
