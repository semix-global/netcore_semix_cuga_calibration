% 参数设置
fs = 1064e6;       % 采样率 (1064 MS/s)
T = 1000e-9;       % 采样时长 (1000 ns)
f_start = 155e6;   % 起始频率 (155 MHz)
f_end = 255e6;     % 终止频率 (255 MHz)

% 生成时间序列
t = 0:1/fs:T-1/fs; % 时间向量
N = length(t);     % 采样点数

% 生成线性调频信号 (155-255 MHz)
freq_sweep = linspace(f_start, f_end, N);
signal = sin(2 * pi * freq_sweep .* t);

% 傅里叶分析
fft_result = fft(signal);
fft_magnitude = abs(fft_result)/N * 2;  % 归一化幅值
fft_freq = (0:N-1)*(fs/N);              % 频率向量

% 对180-270MHz频段幅度减半
f_low = 180e6;  % 处理频段下限
f_high = 270e6; % 处理频段上限
band_indices = find(fft_freq >= f_low & fft_freq <= f_high);
modified_fft = fft_result;
modified_fft(band_indices) = modified_fft(band_indices) * 0.5;  % 幅度减半

% 确保Nyquist频率后的对称性
modified_fft(N - band_indices + 2) = modified_fft(N - band_indices + 2) * 0.5;

% 逆傅里叶变换恢复时域信号
recovered_signal = real(ifft(modified_fft));

% 计算处理后的频谱
modified_magnitude = abs(modified_fft)/N * 2;
positive_freq = fft_freq(1:floor(N/2));
positive_magnitude = fft_magnitude(1:floor(N/2));
positive_modified = modified_magnitude(1:floor(N/2));

% 绘图
figure('Position', [100, 100, 800, 800])

% 原始频谱 vs 处理后的频谱
subplot(3,1,1)
plot(positive_freq/1e6, positive_magnitude, 'b')  % 原始频谱
hold on
plot(positive_freq/1e6, positive_modified, 'r')   % 处理后的频谱
title('频谱比较 (蓝色:原始, 红色:180-270MHz减半)')
xlabel('频率 (MHz)')
ylabel('幅度')
xlim([100 300])  % 显示100-300 MHz范围
grid on
legend('原始信号', '处理后信号')

% 原始时域信号 (前100个点)
subplot(3,1,2)
plot(t*1e9, signal, 'b')  % 时间显示为ns
title('原始时域信号 (前100个采样点)')
xlabel('时间 (ns)')
ylabel('幅度')
grid on

% 恢复的时域信号 (前100个点)
subplot(3,1,3)
plot(t*1e9, recovered_signal, 'r')  % 时间显示为ns
title('恢复的时域信号 (180-270MHz减半后)')
xlabel('时间 (ns)')
ylabel('幅度')
grid on

% 打印关键信息
fprintf('采样点数: %d\n', N);
fprintf('频率分辨率: %.2f MHz\n', fs/N/1e6);
fprintf('信号持续时间: %.0f ns\n', T*1e9);
fprintf('采样率: %.0f MS/s\n', fs/1e6);
fprintf('频率范围: %.0f-%.0f MHz\n', f_start/1e6, f_end/1e6);
fprintf('处理频段: %.0f-%.0f MHz (幅度减半)\n', f_low/1e6, f_high/1e6);