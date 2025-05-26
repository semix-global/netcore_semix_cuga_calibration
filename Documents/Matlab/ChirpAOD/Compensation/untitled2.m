% 参数设置（示例值，您可以根据实际需求修改）
    sampleRate = 1000;          % 采样率 (MHz)
    totalSampleCount = 1024;    % 总采样点数
    f1 = 100;                  % 信号频率 (MHz)
    
    % 生成测试信号（100 MHz 正弦波 + 噪声）
    t = (0:totalSampleCount-1)' / sampleRate;  % 时间轴 (µs)
    aodWaveSignals = sin(2*pi*f1*t) + 0.5*randn(size(t));  % 正弦波 + 高斯噪声
    
    % 傅里叶变换（双边频谱）
    nfft = 2^nextpow2(totalSampleCount);      % FFT 点数（优化为2的幂）
    fftSignal = fft(aodWaveSignals, nfft);    % 计算FFT
    fftMagnitude = abs(fftSignal);            % 取幅度谱
    fftMagnitude = fftshift(fftMagnitude);    % 零频移至中心
    
    % 生成对称频率轴 (MHz)
    frequencies = (-nfft/2:nfft/2-1) * (sampleRate / nfft);
    
    % 绘制结果
    figure('Position', [100, 100, 1200, 500]);
    
    % 1. 时域信号
    subplot(1, 2, 1);
    plot(t, aodWaveSignals, 'b', 'LineWidth', 1);
    xlabel('Time (\mu s)');
    ylabel('Amplitude');
    title('Time Domain Signal');
    grid on;
    
    % 2. 频域信号（双边频谱）
    subplot(1, 2, 2);
    plot(frequencies, fftMagnitude, 'r', 'LineWidth', 1);
    xlabel('Frequency (MHz)');
    ylabel('Magnitude');
    title('Double-Sided FFT Spectrum');
    xlim([-sampleRate/2, sampleRate/2]);  % 显示完整频率范围
    grid on;
    
    % 添加零频标记
    hold on;
    plot([0, 0], ylim, 'k--', 'LineWidth', 0.5);
    text(0, max(fftMagnitude)*0.9, 'DC (0 MHz)', 'HorizontalAlignment', 'center');