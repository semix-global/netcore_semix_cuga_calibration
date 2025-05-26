%% Prescan AOD 205-355MHz 3.1 us 线性递增的扫频信号
clear all
close all

L = 3.2; %开放功能
v = 5.742; Delta = 0; %
delay = 0;
fa = 355 + Delta; fb = 205 + Delta; %a高频，b低频；开放功能
T_chirp = L / v * 1000;
ramp = (fa - fb) / T_chirp; %MHz,ns   1/5.742=174;1.5/5.742=261
% Fs=1000;%Fs,M
Fs = 1064; %Fs,M；采样率

N = round(1000 * L / v * Fs / 1000 + 30 + 2 * delay);
t = (1:N)'; %ns
t1 = (1:delay + 15)';
t2 = (delay + 15 + 1:N - (delay + 15))';
t3 = (N - (delay + 15) + 1:N)';
% tso=t2-t2(1);
% f=110+k/2*t;%递增Chirp
% f=263-k/2*t;
% Phi=fa*(t2-t2(1))-ramp/2*(t2-t2(1)).^2;%递减prescan-,递增prescan+

% C = -0.000001;%开放功能
values = [0.0001];

for C = values
    Phi1 = ramp / 2 * C * (t2 - ((length(t2) + 31) / 2)) .^ 2; %中间
    %     Phi1=ramp/2*C*(t2-length(t2)).^2; %起始
    %     Phi1=ramp/2*C*(t2-t2(1)).^2; %末尾
    Phi2 = fa - ramp / 2 * (t2 - t2(1));
    Phi = Phi2 - Phi1;

    %功率调节....................................
    y0 = 1; %拟合参数1
    xc = 210.8; %拟合参数2
    A = 3.07; %拟合参数3
    a = 18.7; %拟合参数4
    b = 13.3; %拟合参数5
    c = 24.4; %拟合参数6
    % Amp=y0+A*((1+exp(-(f+34-xc+a/2)/b)).^(-1)).*(1-(1+exp(-(f+34-xc-a/2)/c)).^(-1));%Prescan AOD 拟合曲线
    Amp = 1;
    figure(1)
    plot((t2 - t2(1)), Phi1)
    title('频率非线性响应曲线')

    figure(2)
    plot((t2 - t2(1)), Phi2)

    figure(3)
    plot((t2 - t2(1)), Phi)

    %画原始信号.........................................
    Ts = 1 / Fs;

    % sig=cos(2*pi*f.*t/1000);
    sig = zeros(1, N);
    sig1 = sig;
    sig1(t2) = cos(2 * pi * Phi .* t2 / 1000) %.*Amp;
    Amp1 = t1 / length(t1);
    sig1(t1) = cos(2 * pi * (fa + Delta) * t1 / 1000) .* Amp1;
    Amp3 = t1(length(t1):-1:1) / length(t1);
    sig1(t3) = cos(2 * pi * (fb + Delta) * t3 / 1000) .* Amp3;

    % sig1(N-40:N)=0;%test
    figure(4)
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
    % subplot(212)
    % plot(t,f);
    % title('f');
    % xlabel('ns');
    % ylabel('Frequency');

    %加窗信号
    Arr = ones(1, N);
    n = 15 + delay; %可调节参数
    Arr(1:n) = -sin((-2 * pi / (4 * n)) * (1:n)); %自定义函数：正弦函数
    Arr(N - n + 1:N) = -sin((-2 * pi / (4 * n)) * (n:-1:1));
    % Arr(1:n)=sin((pi/2*(0:n-1)/(n-1))).^2;%hann函数
    % Arr(N-n+1:N)=sin((pi/2*((n:-1:1)-1)/(n-1))).^2;
    sig2 = sig1; %.*Arr';
    % sig3=sig.*Arr';
    y = int16(32768 * sig2);
    figure(5)
    subplot(211)
    plot(t, sig2);
    title('sig2(sig1乘以窗函数)');
    xlabel('ns');
    ylabel('Amp');
    subplot(212)
    % Freq=Fs*(0:N-1)/N;
    Freq = linspace(-Fs / 2, Fs / 2, length(t2));
    p2 = fftshift(abs(fft(sig1(t2))));
    % p3=p2*(x.^2+x);
    % p2=abs(fft(sig2));%test
    plot(Freq, p2);
    title('p2(输出FFT信号)');
    xlabel('Frequency');
    ylabel('Amp');
    xlim([0 500])
    hold on
    %ylim([0 120]);

    y = int16(32768 * sig1);
    figure(6)
    subplot(211)
    plot(t, sig1);
    title('sig1(sig1乘以窗函数)');
    xlabel('ns');
    ylabel('Amp');
    subplot(212)

    plot(Freq, p2);
    title('p2(输出FFT信号)');
    xlabel('Frequency');
    ylabel('Amp');
    xlim([0 500])
    hold on

    y = int64(y);

    for i = 1:length(y)

        if y(i) < 0
            y(i) = y(i) + 2 ^ 32;
        end

    end

    % 转换为十六进制字符串，确保是4个十六进制数字
    strArray = dec2hex(y, 4);
    lastFourChars = strArray(:, end - 3:end);

    stringArray = string(lastFourChars);

    filename = sprintf('B3_CAOD_3.2mm_355_205_557ns_0delay_%.6f.txt', C);
    % 打开文件用于写入
    fileID = fopen(filename, 'wt');

    % 检查文件是否成功打开
    if fileID == -1
        error('File cannot be opened');
    end

    % 循环遍历字符串数组的每个元素
    for i = 1:length(stringArray)

        if i == length(stringArray)
            fprintf(fileID, '%s', stringArray(i));
        else
            fprintf(fileID, '%s\n', stringArray(i));
        end

    end

    % 关闭文件
    fclose(fileID);

    % clear
    %
    %
    % % writematrix(stringArray, FileName);
    % % % dlmwrite(FileName,array,'\n');%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
    % % type(FileName);
    %
    % FileName='B3_CAOD_3.2mm_355_205_557ns_0delay_0C.txt';
    % dlmwrite(FileName,y,'delimiter' , '\n' , 'precision', 6)%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
    % type(FileName)
end
