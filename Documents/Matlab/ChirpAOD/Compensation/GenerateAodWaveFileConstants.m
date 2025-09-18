classdef GenerateAodWaveFileConstants
    %% AOD波形文件生成参数常量定义
    % 此类包含GenerateAodWaveFile函数中使用的所有常量参数
    
    properties (Constant)
        %% 基础参数
        BANDWIDTH = 210;                    % 带宽 (MHz)
        CENTER_FREQUENCY = 200;             % 中心频率 (MHz)
        MONOTONIC_TYPE_INCREASING = 1;      % 递增, 递减, 平坦: 1, -1, 0
        SAMPLE_RATE = 10640;                % 采样率 (Msa/s)
        AMPLITUDE = 0.9;                    % 幅值
        ZERO_SAMPLE_COUNT = 110;            % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
        ENDPOINT_BUFFER_SAMPLES = 10000;    % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
        OFFSET_FREQUENCY = 215;             % 偏移的频率
        GENERATE_RETRY_TIMES = 2000;        % 重试次数
        
        %% 时间和长度参数
        CHIRP_MODE = -1;                    % 平坦时间 (ns), -1 启用chirp
        PRESCAN_MODE = -1;                  % 音包长度 (mm), -1 启用prescan
        SOUND_PACKET_LENGTH = 10.2;         % 音包长度 (mm), 用于chirp模式
        FLATNESS_TIME = 1000.3;             % 平坦时间 (ns), 用于prescan模式
        
        %% 偏移频率的2π周期倍率 (两组不同值)
        OFFSET_PERIOD_MULTIPLE_LOW = 0.8;   % 偏移的频率的2π周期的倍率 (低值)
        OFFSET_PERIOD_MULTIPLE_HIGH = 1.5;  % 偏移的频率的2π周期的倍率 (高值)
        
        %% 补偿系数参数
        SINC_COEFFICIENT = 1.1;                          % sinc系数
        ASTIGMATISM_COMPENSATION = 1.2;                  % 二次补偿系数t^2 散光
        SPHERICAL_ABERRATION_COMPENSATION = 1.3;        % 三次补偿系数t^3 球差
        SECONDARY_ASTIGMATISM_COMPENSATION = 1.4;        % 四次补偿系数t^4 二阶散光
        COMA_COMPENSATION = 1.55;                        % sin(2πt/T)
        TREFOIL_COMPENSATION = 1.6;                      % sin(6πt/T)
        QUADRAFOIL_COMPENSATION = 1.7;                   % sin(8πt/T)
        ALPHA_ORDER = 1.8;                               % α次补偿
        ALPHA_ORDER_COEFFICIENT = 1.9;                   % α次补偿系数t^α
        
        %% 平坦度测试时的补偿系数 (所有为0)
        FLATNESS_SINC_COEFFICIENT = 0;                   % 平坦度测试时的sinc系数
        FLATNESS_ASTIGMATISM_COMPENSATION = 0;           % 平坦度测试时的二次补偿系数
        FLATNESS_SPHERICAL_ABERRATION_COMPENSATION = 0;  % 平坦度测试时的三次补偿系数
        FLATNESS_SECONDARY_ASTIGMATISM_COMPENSATION = 0; % 平坦度测试时的四次补偿系数
        FLATNESS_COMA_COMPENSATION = 0;                  % 平坦度测试时的sin(2πt/T)
        FLATNESS_TREFOIL_COMPENSATION = 0;               % 平坦度测试时的sin(6πt/T)
        FLATNESS_QUADRAFOIL_COMPENSATION = 0;            % 平坦度测试时的sin(8πt/T)
        FLATNESS_ALPHA_ORDER = 0;                        % 平坦度测试时的α次补偿
        FLATNESS_ALPHA_ORDER_COEFFICIENT = 0;            % 平坦度测试时的α次补偿系数
        
        %% 文件路径
        UNIFORMITY_FILE_PATH = "..\..\..\..\Sources\Cuga-Calibration-Unit-Test\Assets\AODWaveformUniformity.xlsx"; % 频率幅值文件路径
        EMPTY_FILE_PATH = "";                            % 空文件路径
        
        %% 斜率变化率配置
        SLOPE_DELTA_K_CONFIG = [-0.0002, -0.0001, 0.0001, 0.0002]; % AOD波形的斜率变化率分段的配置项集合
        EMPTY_SLOPE_CONFIG = [];                         % 空斜率配置
        
        %% 平坦度测试特殊参数
        FLATNESS_BANDWIDTH = 0;                          % 平坦度测试时的带宽
        FLATNESS_MONOTONIC_TYPE = 0;                     % 平坦度测试时的单调类型 (平坦)
    end
    
    methods (Static)
        function outputDir = getOutputDirectory(subDirName)
            %% 获取输出目录路径
            % subDirName: 子目录名称
            outputDir = fullfile('..\..\..\..\Sources\Cuga-Calibration-Unit-Test\Assets\AODWaveformFiles', subDirName);
        end
    end
end