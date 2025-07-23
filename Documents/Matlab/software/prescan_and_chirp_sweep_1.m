

%% Prescan AOD 180-260MHz 3.1 us 线性递增的扫频信号 
clear all;
close all;

fa=205;fb=215; %a低频，b高频
fa1=fa;fb1=fb;
bandwidth=10;%带宽Mhz

T=4300;%平坦时间ns
symbol_waveform=1;% 0代表递减, 1代表递增 
symbol_window_function=1;% 0代表选择线性加窗，1代表正玹函数加窗
ramp=bandwidth/T;%MHz,ns   1/5.742=174;1.5/5.742=261

Fs=1064;%Fs,M；采样率

delay=300;%波形上升和下降沿
AMP=0.4;%波形幅值
Amp=0.4;
N=round(T*Fs/1000+2*delay);
t=(1:N)';%ns

t1=(1:delay)';
t2=(delay+1:N-(delay))';
t3=(N-delay+1:N)';
sig1=zeros(1,N);
% 
% for i=1:length(t2)
%     if i>1000&&i<=1500
%         Amp(i)=(1/500)*AMP*(1500-i);
%     elseif 1500<i&&i<=2000
%         Amp(i)=(1/500)*AMP*(i-1500);
%     else
%         Amp(i)=AMP;
%     end
% end
% 
% figure(5)
% plot(Amp);

Phi=fb*(t2-t2(1))-ramp/2*(t2-t2(1)).^2;
sig1(t2)=cos(2*pi*Phi/1000).*Amp';
Amp1=t1/length(t1).*AMP;
sig1(t1)=cos(2*pi*(fb)*t1/1000).*Amp1;
Amp3=t1(length(t1):-1:1)/length(t1).*AMP;
sig1(t3)=cos(2*pi*(fa)*t3/1000).*Amp3; 

% 加窗信号
Arr=ones(1,length(t2));
n=300;%可调节参数
Arr(1:n)=-sin((-2*pi/(4*n))*(1:n));%自定义函数：正弦函数
Arr(length(t2)-n+1:length(t2))=-sin((-2*pi/(4*n))*(n:-1:1));

sig1(t2)=sig1(t2).*Arr;

y=int16(32768*sig1);
figure
subplot(211)
plot(t2,sig1(t2));
title('sig2(sig1乘以窗函数)');
xlabel('ns');
ylabel('Amp');
subplot(212)
% Freq=Fs*(0:N-1)/N;
Freq=linspace(-Fs/2,Fs/2,length(t2));
p2=fftshift(abs((fft(sig1(t2)))));
% p3=p2*(x.^2+x);
% p2=abs(fft(sig2));%test
plot(Freq,p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on

FileName='B1_chirp_1.7mm_263_163_296ns_345.txt';
dlmwrite(FileName,y,'delimiter' , '\n' , 'precision', 6)%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
type(FileName)
