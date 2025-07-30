clear all;
close all;

fa=183.5;fb=236.5; %a低频，b高频
fa1=fa;fb1=fb;
bandwidth=fb-fa;%带宽Mhz

T=4300;%平坦时间ns
symbol_waveform=1;% 0代表递减, 1代表递增 
symbol_function=0;% 0代表选择波形的起始频率从0处就计算 。1代表波形加入了delay，波形的起始频率从delay结束后开始计算
symbol_AOD=1; %%%0代表prescan   1代表chirp
%%%symbol_function=0 上升沿有效，delay方案无效。symbol_function=1时上升沿无效，delay方案有效。%%%
%%%上升沿或者delay 选取不合适会导致错误%%%
%%%prescan 上升沿一般选择300，chirp 上升沿一般选择15。delay方案 prescan或者chirp一般选择300%%%

rise_down=300;%波形上升和下降沿
delay=300;
Amp=1;%波形幅值
%%%%%%确保上述参数设置完成%%%%%

ramp=bandwidth/T;%MHz,ns   1/5.742=174;1.5/5.742=261
Fs=1064;%Fs,M；采样率
if symbol_function==1
    N=round(T*Fs/1000+2*delay);
else
    N=round(T*Fs/1000+2*rise_down);
end
t=(1:N)';%ns
t1=(1:delay)';
t2=(delay+1:N-(delay))';
t3=(N-delay+1:N)';
sig1=zeros(1,N);
if symbol_AOD==0
    name='prescan';
    result=sprintf('%s_%dM_%dns_%dAMP_%d.txt', name,bandwidth,T,Amp,N);
else
    name='chirp';
    result=sprintf('%s_%dM_%dns_%dAMP_%d.txt', name,bandwidth,T,Amp,N);
end
conditions=0;
j=0;
while conditions==0
    ramp=bandwidth/T;
    if symbol_function==1
        if symbol_waveform==1
            Phi=fa*(t2-t2(1))+ramp/2*(t2-t2(1)).^2;   
            sig1(t2)=cos(2*pi*Phi/1000).*Amp;
            Amp1=t1/length(t1);
            sig1(t1)=cos(2*pi*(fa)*t1/1000).*Amp1;
            Amp3=t1(length(t1):-1:1)/length(t1);
            sig1(t3)=cos(2*pi*(fb)*t3/1000).*Amp3;
            Freq=linspace(-Fs/2,Fs/2,length(t2));
            p2=fftshift(abs(fft(sig1(t2))));

            a=round(((fa+fb)/2-(fb-fa)/4)/(Fs/length(t2))+length(t2)/2);
            b=round(((fa+fb)/2+(fb-fa)/4)/(Fs/length(t2))+length(t2)/2);
            amplitude = mean(p2(a:b));
            for i=round(length(t2)/2):length(p2)
                if p2(i)-amplitude>0
                    if abs(p2(i)-amplitude)>abs(p2(i-1)-amplitude) 
                        x1=i-1;
                    else
                        x1=i;
                    end
                    break;
                else

                end
            end
            for i=1:length(p2)
                if p2(length(p2)-i)-amplitude>0
                    if abs(p2(length(p2)-i)-amplitude)>abs(p2(length(p2)-i+1)-amplitude)
                        x2=length(p2)-i+1;
                    else
                        x2=length(p2)-i;
                    end
                    break;
                else

                end
            end
            if abs(Freq(x1)-fa1)<(Fs/length(t2))
                if abs(Freq(x2)-fb1)<(Fs/length(t2))
                    conditions=1;
                elseif Freq(x1)<fb1
                    bandwidth=bandwidth-Fs/(2*length(t));
                else 
                    bandwidth=bandwidth+Fs/(2*length(t));
                end 
            elseif Freq(x1)<fa1
                fa=fa+Fs/(2*length(t));
            else 
                fa=fa-Fs/(2*length(t));
            end
        else
            Phi=fb*(t2-t2(1))-ramp/2*(t2-t2(1)).^2;
            sig1(t2)=cos(2*pi*Phi/1000).*Amp;
            Amp1=t1/length(t1);
            sig1(t1)=cos(2*pi*(fb)*t1/1000).*Amp1;
            Amp3=t1(length(t1):-1:1)/length(t1);
            sig1(t3)=cos(2*pi*(fa)*t3/1000).*Amp3; 
            Freq=linspace(-Fs/2,Fs/2,length(t2));
            p2=fftshift(abs(fft(sig1(t2))));

            a=round(((fa+fb)/2-(fb-fa)/4)/(Fs/length(t2))+length(t2)/2);
            b=round(((fa+fb)/2+(fb-fa)/4)/(Fs/length(t2))+length(t2)/2);
            amplitude = mean(p2(a:b));
            for i=round(length(t2)/2):length(p2)
                if p2(i)-amplitude>0
                    if abs(p2(i)-amplitude)>abs(p2(i-1)-amplitude) 
                        x1=i-1;
                    else
                        x1=i;
                    end
                    break;
                else

                end
            end
            for i=1:length(p2)
                if p2(length(p2)-i)-amplitude>0
                    if abs(p2(length(p2)-i)-amplitude)>abs(p2(length(p2)-i+1)-amplitude)
                        x2=length(p2)-i+1;
                    else
                        x2=length(p2)-i;
                    end
                    break;
                else

                end
            end

            if abs(Freq(x2)-fb1)<(Fs/length(t2))
                if abs(Freq(x1)-fa1)<(Fs/length(t2))
                    conditions=1;
                elseif Freq(x1)<fa1
                    bandwidth=bandwidth-Fs/(2*length(t));
                else 
                    bandwidth=bandwidth+Fs/(2*length(t));
                end 
            elseif Freq(x2)<fb1
                fb=fb+Fs/(2*length(t));
            else 
                fb=fb-Fs/(2*length(t));
            end  
        end
    else
        if symbol_waveform==1
            sig1=cos(2*pi*(fa+ramp/2*t).*t/1000)*Amp;
            Arr=ones(1,N);
            Arr(1:rise_down)=-sin((-2*pi/(4*rise_down))*(1:rise_down));%自定义函数：正弦函数
            Arr(N-rise_down+1:N)=-sin((-2*pi/(4*rise_down))*(rise_down:-1:1));
            sig1=sig1.*Arr';

            Freq=linspace(-Fs/2,Fs/2,N);
            p2=fftshift(abs((fft(sig1))));
            a=round(((fa+fb)/2-(fb-fa)/4)/(Fs/length(t))+length(t)/2);
            b=round(((fa+fb)/2+(fb-fa)/4)/(Fs/length(t))+length(t)/2);

            amplitude = mean(p2(a:b));
            for i=round(length(p2)/2):length(p2)
                if p2(i)-amplitude>0
                    if abs(p2(i)-amplitude)>abs(p2(i-1)-amplitude) 
                        x1=i-1;
                    else
                        x1=i;
                    end
                    break;
                else

                end
            end
            for i=1:length(p2)
                if p2(length(p2)-i)-amplitude>0
                    if abs(p2(length(p2)-i)-amplitude)>abs(p2(length(p2)-i+1)-amplitude)
                        x2=length(p2)-i+1;
                    else
                        x2=length(p2)-i;
                    end
                    break;
                else

                end
            end

            if abs(Freq(x1)-fa1)<(Fs/length(t))
                if abs(Freq(x2)-fb1)<(Fs/length(t))
                    conditions=1;
                elseif Freq(x2)<fb1
                    bandwidth=bandwidth+Fs/(2*length(t));
                else 
                    bandwidth=bandwidth-Fs/(2*length(t));
                    
                end 
            elseif Freq(x1)<fa1
                fa=fa+Fs/(2*length(t));
                
            else 
                fa=fa-Fs/(2*length(t)); 
            end
        else
            sig1=cos(2*pi*(fb-ramp/2*t).*t/1000)*Amp;
            Arr=ones(1,N);
            Arr(1:rise_down)=-sin((-2*pi/(4*rise_down))*(1:rise_down));%自定义函数：正弦函数
            Arr(N-rise_down+1:N)=-sin((-2*pi/(4*rise_down))*(rise_down:-1:1));
            sig1=sig1.*Arr';

            Freq=linspace(-Fs/2,Fs/2,N);
            p2=fftshift(abs((fft(sig1))));
            a=round(((fa+fb)/2-(fb-fa)/4)/(Fs/length(t))+length(t)/2);
            b=round(((fa+fb)/2+(fb-fa)/4)/(Fs/length(t))+length(t)/2);
            amplitude = mean(p2(a:b));
            for i=round(length(t)/2):length(p2)
                if p2(i)-amplitude>0
                    if abs(p2(i)-amplitude)>abs(p2(i-1)-amplitude) 
                        x1=i-1;
                    else
                        x1=i;
                    end
                    break;
                else

                end
            end
            for i=1:length(p2)
                if p2(length(p2)-i)-amplitude>0
                    if abs(p2(length(p2)-i)-amplitude)>abs(p2(length(p2)-i+1)-amplitude)
                        x2=length(p2)-i+1;
                    else
                        x2=length(p2)-i;
                    end
                    break;
                else

                end
            end

            if abs(Freq(x2)-fb1)<(Fs/length(t))
                if abs(Freq(x1)-fa1)<(Fs/length(t))
                    conditions=1;
                elseif Freq(x1)<fa1
                    bandwidth=bandwidth-Fs/(2*length(t));
                else
                    bandwidth=bandwidth+Fs/(2*length(t));
                end
            elseif Freq(x1)<fb1
                fb=fb-Fs/(2*length(t));
            else 
                fb=fb+Fs/(2*length(t));
            end
        end
    end
    j=j+1;
end    

y=int16(32768*sig1);
figure(1)
subplot(211)
plot(t,sig1);
title('sig1(sig1乘以窗函数)');
xlabel('ns');
ylabel('Amp');
subplot(212)

plot(Freq,p2);
title('p2(输出FFT信号)');
xlabel('Frequency');
ylabel('Amp');
xlim([0 500])
hold on

y=int64(y);
for i=1:length(y)
    if y(i)<0
        y(i)=y(i)+2^32;
    end
end
% 转换为十六进制字符串，确保是4个十六进制数字
strArray = dec2hex(y,4);
lastFourChars = strArray(:, end-3:end)

stringArray=string(lastFourChars);

% 打开文件用于写入
fileID = fopen(result, 'wt');

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

% writematrix(stringArray, FileName);
% % dlmwrite(FileName,array,'\n');%将矩阵M导出到FileName文件中，分隔符为制表符，有效数位为6位。
% type(FileName);


