%======================
% Chirp 信号生成与频谱分析（频谱衰减与逆变换，2列3行图）
%======================

close all;
clc;

% 1. 参数定义
f_start = 275e6;
f_end = 155e6;
fs = 1064e6;
flat_time = 1000e-9;

N = round(flat_time * fs);
dt = 1 / fs;
t = (0:N - 1) * dt;

% 2. 生成 chirp 信号（线性下降）
f_t = f_start + (f_end - f_start) / max(t) * t;
dPhase = 2 * pi * f_t * dt;
phi = cumsum(dPhase);
x = cos(phi); % 原始信号

% 3. 原始频谱分析
X = fft(x);
frequencies = linspace(-fs / 2, fs / 2, N);
X_shifted = fftshift(X);
magnitude_original = abs(X_shifted);

% 4. 修改频谱（将 155-275 MHz 范围衰减为 0.5 倍）
f_low = 155e6;
f_high = 275e6;
index_range = (abs(frequencies) >= f_low) & (abs(frequencies) <= f_high);

magnitude_modified = magnitude_original;
phase = angle(X_shifted);

magnitude_modified(index_range) = 0.5 * magnitude_modified(index_range);
X_modified = magnitude_modified .* exp(1i * phase);
magnitude_half = abs(X_modified);

X_modified1 = X_shifted;
X_modified1 = 0.5 * X_modified1;
magnitude_half1 = abs(X_modified1);

error1 = max(abs(magnitude_half - magnitude_modified));
error2 = max(abs(magnitude_half1 - magnitude_modified)); % 0
error3 = max(abs(magnitude_half1 - magnitude_half));

% 5. 逆变换，得到修改后的时域信号
X_modified_unshifted = ifftshift(X_modified1);
x_modified = ifft(X_modified_unshifted);

% 6. 修改后频谱分析
X2 = fft(x_modified);
X2_shifted = fftshift(X2);
magnitude_modified = abs(X2_shifted);

% 7. 绘图：2列3行 subplot
fig = figure('Name', 'Chirp Signal Analysis', 'WindowState', 'maximized');
set(fig, 'Units', 'normalized', 'Position', [0 0 1 1]);
set(groot, 'DefaultAxesFontSize', 12)
set(groot, 'DefaultLineLineWidth', 1.5)

% 左上：原始时域图
subplot(3, 2, 1);
plot(t * 1e9, x);
xlabel('Time (ns)');
ylabel('Amplitude');
title('Original Chirp Signal');
grid on;

% 右上：修改后时域图
subplot(3, 2, 2);
plot(t * 1e9, x_modified);
xlabel('Time (ns)');
ylabel('Amplitude');
title('Modified Chirp Signal');
grid on;

% 左中：原始频谱图
subplot(3, 2, 3);
plot(frequencies / 1e6, magnitude_original);
xlabel('Frequency (MHz)');
ylabel('Magnitude');
title('Original Spectrum');
grid on;

% 右中：修改后频谱图
subplot(3, 2, 4);
plot(frequencies / 1e6, magnitude_modified);
xlabel('Frequency (MHz)');
ylabel('Magnitude');
title('Modified Spectrum');
grid on;

% 左下：原始频谱 × 0.5（用于对比）
subplot(3, 2, 5);
plot(frequencies / 1e6, magnitude_half);
xlabel('Frequency (MHz)');
ylabel('Magnitude');
title('Original Spectrum (155–275 MHz × 0.5)');
grid on;

% 右下：绘制频谱差异曲线
subplot(3, 2, 6);
plot(frequencies / 1e6, magnitude_modified./ magnitude_original);
xlabel('Frequency (MHz)');
ylabel('Magnitude Difference');
title('Difference: Modified vs. Half-Attenuated');
grid on;
