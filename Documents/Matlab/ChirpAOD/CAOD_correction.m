

%% chirp AOD 非线性的扫频信号 
clear all
close all

L=4.5;%开放功能
v=5.742;
fa=380;fb=200; %a起始，b结束；开放功能
T_chirp = L/v*1000;
ramp=(fa-fb)/(T_chirp*1.064);%MHz,ns   1/5.742=174;1.5/5.742=261
% Fs=1000;%Fs,M
Fs=1228.8;%Fs,M；采样率
Amp=1;

N=round(L/v*Fs);
t=(1:N)';%ns

Phi0 = fa - ramp/2*t;

A = 0;   % Piston,即相位对齐
Phi1 = A;

B = 0;   % defocus
Phi2 = B*t;

C = 0;   % Astigmatism
Phi3 = C*t.^2;

D = 10;   % Coma
Phi4 = D*sin(2*pi*t/N);

E = 0;   % Trefoil
Phi5 = E*sin(6*pi*t/N);

F = 0;   % Spherical Aberration
Phi6 = F*t.^3;

G = 0;   % Secondary Astigmatism
Phi7 = G*t.^4;

H = 0;   % Quadrafoil
Phi8 =  H*sin(8*pi*t/N);

Phi=fa - ramp/2*t + A + B*t + C*t.^2+ F*t.^3 + G*t.^4 + D*sin(2*pi*t/N) + E*sin(6*pi*t/N)  + H*sin(8*pi*t/N);

figure(1)
plot(t,Phi0)
title('频率线性响应曲线')

figure(2)
plot(t,Phi-Phi0)
title('频率非线性响应曲线')

figure(3)
plot(t,Phi)
title('频率响应曲线')

% 画原始信号.........................................
Ts=1/Fs;
% sig=cos(2*pi*f.*t/1000);
sig=zeros(1,N);
sig(t)=cos(2*pi*Phi.*t/1064);

% figure(3)
% plot(t,sig);
% title('sig(原始信号)');
% xlabel('ns');
% ylabel('Amp');

%加窗信号
% Arr=ones(1,N);
% Arr(1:N)=-sin((-2*pi/(4*n))*(n:-1:1));
% Arr(1:n)=sin((pi/2*(0:n-1)/(n-1))).^2;%hann函数
% Arr(N-n+1:N)=sin((pi/2*((n:-1:1)-1)/(n-1))).^2;
% sig2=sig1; %.*Arr';
% sig3=sig.*Arr';
y=int16(32768*sig);
figure(4)
subplot(211)
plot(t,sig);
title('sig原始信号');
xlabel('ns');
ylabel('Amp');
subplot(212)
% Freq=Fs*(0:N-1)/N;
Freq=linspace(-Fs/2,Fs/2,N);
p2=fftshift(abs(fft(sig(t))));
% p3=p2*(x.^2+x);
% p2=abs(fft(sig2));%test
plot(Freq,p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on
%ylim([0 120]);

y=int64(y);
for i=1:length(y)
    if y(i)<0
        y(i)=y(i)+2^32;
    end
end
% 转换为十六进制字符串，确保是4个十六进制数字
strArray = dec2hex(y,4);
lastFourChars = strArray(:, end-3:end);

stringArray=string(lastFourChars);

filename = sprintf('B3_CAOD_%.2fmm_440_240_%.f_COMA%.3f.txt',L,N,D);
% 打开文件用于写入
fileID = fopen(filename, 'wt');

% 检查文件是否成功打开
if fileID == -1
    error('File cannot be opened');
end

% 循环遍历字符串数组的每个元素
for i = 1:length(stringArray)
    if i==length(stringArray)
        fprintf(fileID, '%s', stringArray(i));
    else
        fprintf(fileID, '%s\n', stringArray(i));
    end
end
    
% 关闭文件
fclose(fileID);

