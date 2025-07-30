k = (280 - 280) / 4300; %MHz,ns，
Fs = 1064; %采样率： 1.064
N = round(4300 .* Fs / 1000 + 400); % 采样点数取整
t = (0:N - 1)'; %ns
f = 255 - k / 2 * t; %递减prescan-,递增prescan+
%功率调节....................................
y0 = 1; %拟合参数1
xc = 210.8; %拟合参数2
A = 3.07; %拟合参数3
a = 18.7; %拟合参数4
b = 13.3; %拟合参数5
c = 24.4; %拟合参数6
att = 1;
Amp = 1 / att;
figure
plot(t, Amp)
title('Prescan AOD Ampli-f(幅频响应函数)')
%画原始信号.........................................
Ts = 1 / Fs;
sig = cos(2 * pi * f .* t / 1000);
sig1 = cos(2 * pi * f .* t / 1000) .* Amp .^ -1;
figure
subplot(211)
plot(t, sig);
title('sig(原始信号)');
xlabel('ns');
ylabel('Amp');
subplot(212)
plot(t, sig1);
title('sig1（sig除以幅频响应函数）');
xlabel('ns');
ylabel('Amp');
Arr = ones(1, N);
n = 200; %可调节参数
Arr(1:n) = -sin((-2 * pi / (4 * n)) * (1:n)); %自定义函数：正弦函数
Arr(N - n + 1:N) = -sin((-2 * pi / (4 * n)) * (n:-1:1));
sig2 = sig1 .* Arr';
y = int16(32768 * sig2)
figure
subplot(211)
plot(t, sig2);
title('sig2(sig1乘以窗函数)');
xlabel('ns');
ylabel('Amp');
subplot(212)
Freq = linspace(-Fs / 2, Fs / 2, N);
p2 = fftshift(abs(fft(sig2)));
plot(Freq, p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on
FileName = 'B2_pres_265_155_4300ns_4975.txt';
dlmwrite(FileName, y, 'delimiter', '\n', 'precision', 6) %将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
type(FileName)
