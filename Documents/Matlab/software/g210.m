

%% Prescan AOD  线性递增的扫频信号 4.1us 150-270
clear all
close all
% k=(260-180)/3100;%MHz,ns
%k=(280-196)/4180;%MHz,ns
k=0;
% Fs=1000;%Fs,M
Fs=1064;%Fs,M；采样率
N=round(4180*1.064+600);
t=(0:N-1)';%ns
% f=110+k/2*t;%递增Chirp
f=210-k/2*t;%递减prescan-,递增prescan+
%功率调节....................................
y0=1;%拟合参数1
xc=210.8;%拟合参数2
A=3.07;%拟合参数3
a=18.7;%拟合参数4
b=13.3;%拟合参数5
c=24.4;%拟合参数6
% Amp=y0+A*((1+exp(-(f+34-xc+a/2)/b)).^(-1)).*(1-(1+exp(-(f+34-xc-a/2)/c)).^(-1));%Prescan AOD 拟合曲线
Amp=1;
figure
plot(t,Amp)
title('Prescan AOD Ampli-f(幅频响应函数)')
%画原始信号.........................................
Ts=1/Fs;
sig=cos(2*pi*f.*t/1000);
sig1=cos(2*pi*f.*t/1000).*Amp.^-1;
% sig1(N-40:N)=0;%test
figure
subplot(211)
plot(t,sig);
title('sig(原始信号)');
xlabel('ns');
ylabel('Amp');
subplot(212)
plot(t,sig1);
title('sig1（sig除以幅频响应函数）');
xlabel('ns');
ylabel('Amp');
% subplot(212)
% plot(t,f);
% title('f');
% xlabel('ns');
% ylabel('Frequency');

%加窗信号
Arr=ones(1,N);
n=0;%可调节参数
Arr(1:n)=-sin((-2*pi/(4*n))*(1:n));%自定义函数：正弦函数
Arr(N-n+1:N)=-sin((-2*pi/(4*n))*(n:-1:1));
% Arr(1:n)=sin((pi/2*(0:n-1)/(n-1))).^2;%hann函数
% Arr(N-n+1:N)=sin((pi/2*((n:-1:1)-1)/(n-1))).^2;
sig2=sig1.*Arr';
% sig3=sig.*Arr';
y=int16(32768*sig2)
figure
subplot(211)
plot(t,sig2);
title('sig2(sig1乘以窗函数)');
xlabel('ns');
ylabel('Amp');
subplot(212)
% Freq=Fs*(0:N-1)/N;
Freq=linspace(-Fs/2,Fs/2,N);
p2=fftshift(abs(fft(sig2)));
% p3=p2*(x.^2+x);
% p2=abs(fft(sig2));%test
plot(Freq,p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on
%ylim([0 120]);

FileName='g210.txt';
dlmwrite(FileName,y,'delimiter' , '\n' , 'precision', 6)%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
type(FileName)
