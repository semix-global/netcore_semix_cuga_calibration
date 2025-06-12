clc;
clear;
close all;

% 参数设置
fs = 1064e6;            % 采样率 (1064 Msa/s)
T = 1000e-9;            % 平坦时间 (1000 ns)
f_start = 275e6;        % 起始频率 (275 MHz)
f_end = 155e6;          % 终止频率 (155 MHz)

% 生成时间向量
t = 0:1/fs:T-1/fs;
N = length(t);          % 采样点数

% 生成线性调频信号 (频率线性递减)
freq = linspace(f_start, f_end, N);
phase = 2*pi*cumsum(freq)/fs;
signal = cos(phase);

% 绘制时域波形
figure;
subplot(2,1,1);
plot(t*1e9, signal);
xlabel('时间 (ns)');
ylabel('幅度');
title('时域波形');
grid on;

% 傅里叶分析
f = linspace(-fs / 2, fs / 2, N)/1e6; % 频率轴 (MHz)
fft_signal = fft(signal);
fft_signal_shifted = fftshift(fft_signal);
magnitude = abs(fft_signal_shifted);

magnitude_pos = magnitude(N/2:N); % 只考虑正频率
f_pos = f(N/2:N);

% 归一化并找到-3dB点
magnitude_norm = magnitude_pos / max(magnitude_pos);
threshold = 0.5; % -3dB阈值

% 找到起始和终止频率
idx = find(magnitude_norm >= threshold);
actual_f_start = f_pos(idx(1));
actual_f_end = f_pos(idx(end));

% 绘制频域波形
subplot(2,1,2);
plot(f, magnitude);
xlabel('频率 (MHz)');
ylabel('幅度 (dB)');
title('频域分析');
xlim([0, fs/2/1e6]);    % 显示正频率部分
grid on;

% 显示关键参数
disp(['采样点数: ', num2str(N)]);
disp(['频率范围: ', num2str(f_start/1e6), ' MHz 到 ', num2str(f_end/1e6), ' MHz']);
disp(['持续时间: ', num2str(T*1e9), ' ns']);
disp(['采样率: ', num2str(fs/1e6), ' MHz']);