close all;
% C:\Users\DELL\Documents\4f4a831c-0f2e-45cb-b3fe-34661b36542a.xlsx

GenerateAodWaveFile( ...
    120, ... % 带宽 (MHz)
    215, ... % 中心频率 (MHz)
    -1, ... % 递增, 递减, 平坦: 1, -1, 0
    10640, ... % 采样率 (Msa/s)
    1, ... % 幅值
    fullfile(char(java.lang.System.getProperty('user.home')), 'Desktop', 'Aod'), ... % 生成文件的目录
    0, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
    1000, ... % 平坦时间 (ns), -1 启用chirp
    -1, ... % 音包长度 (mm), -1 启用prescan
    1000, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
    5, ... % sinc系数
    100, ... % 二次补偿系数t^2 散光
    10, ... % 三次补偿系数t^3 球差
    5, ... % 四次补偿系数t^4 二阶散光
    5, ... % sin(2πt/T)
    2, ... % sin(6πt/T)
    0.1, ... % sin(8πt/T)
    1, ... % α次补偿
    1.1, ... % α次补偿系数t^α
    '', ... % 频率幅值文件路径
    1000 ... % 重试次数
)
