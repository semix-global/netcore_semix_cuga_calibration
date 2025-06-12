

%% Prescan AOD 180-260MHz 3.1 us 线性递增的扫频信号 
clear all
close all
% k=(260-180)/3100;%MHz,ns
fa = 275;
fb = 155;
delta_BW = 15;
v = 5.742;
l=2;
tau = l*1000/v;
k=(fa-fb+delta_BW)/tau;%MHz,ns   1/5.742=174;1.5/5.742=261
Fs=1064;%Fs,M；采样率
N=round(tau*Fs/1000);
t=(1:N)';%ns
Ts=1/Fs;
delta_pos = 1;
c=0;
f=(fa+delta_pos)-k/2*t - c*t.^2;%递减prescan-,递增prescan+
%功率调节....................................

figure(1) 
plot(t,f)
title('Prescan AOD Ampli-f(幅频响应函数)')
%画原始信号.........................................

sig=cos(2*pi*f.*t/1000);%修改相位常量修改：+5.7;phi
sig1=cos(2*pi*f.*t/1000);%;修改相位常量：+5.7
%加窗信号
Arr=ones(1,N);
n=15;%可调节参数
Arr(1:n)=-sin((-2*pi/(4*n))*(1:n));%自定义函数：正弦函数
Arr(N-n+1:N)=-sin((-2*pi/(4*n))*(n:-1:1));
sig2=sig1;%.*Arr';


figure(2)
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


figure(3)
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
%ylim([0 120]);

% 幅值补偿
%% 准备数据
frequencies = [155, 160, 165, 170, 175, 180, 185, 190, 195, 200, ...
               205, 210, 215, 220, 225, 230, 235, 240, 245, 250, ...
               255, 260, 265, 270, 275]';
coefficients = [0.625, 0.6, 0.55, 0.6125, 0.55, 0.475, 0.4375, ...
                0.4625, 0.45, 0.4375, 0.425, 0.425, 0.4375, 0.45, ...
                0.45, 0.5, 0.4625, 0.45, 0.4625, 0.5375, 0.7, 0.8, ...
                0.775, 0.75, 1.0]';
N_flat = 46;%定义平坦部分点数
N_fft = floor(N/2)+1;
N_rise = floor((N_fft - N_flat)/2);
N_coeff = length(coefficients);
% coefficients插值生成N列数据
rise = ones(N_rise,1);
sample_coeff = (1:(N_coeff-1)/N_flat:N_coeff)';
coefficients_flat = interp1(coefficients,sample_coeff);
coefficients_flat = coefficients_flat(2:end)+0.0;
coefficients_interp1 =  [rise;coefficients_flat;rise];
% frequencies插值生成N_flat列数据
sample_freq = (1:(N_coeff-1)/N_fft:N_coeff)';
frequencies_interp1 = interp1(frequencies,sample_freq(2:end));


%% 逆变换
% 频谱
P2 = p2(floor(N/2)+1:N,1);%%% 频谱取一半范围 %%%
% P2_flat = P2(N_rise:(N_flat+N_rise)-1);
P3 = P2.*coefficients_interp1;%%% 频谱补偿 乘or除？指数系数？趋势反过来了？ %%%
P3_direction = flip(P3);
P3_all = [P3_direction;P3(2:end)];
%% 含相位的频谱
Amp_compensation = ifftshift(P3_all);%频谱幅值
Phi_compensation = angle(fft(sig2));%频谱相位
%重建频域信号
ans_fft = Amp_compensation.*exp(1i*Phi_compensation);%
answer_compensation = ifft(ans_fft);%求逆傅里叶变换

figure(4);subplot(221);plot(P2)
subplot(222);plot(t,sig2);
subplot(223);plot(P3)
subplot(224);plot(t,answer_compensation)

y=int16(32768*sig2.*1);% 降低幅值0.8
FileName='B1_chirp_263_163_174ns_相位修改+5.7_215.txt';
dlmwrite(FileName,y,'delimiter' , '\n' , 'precision', 6);%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
type(FileName);
