%% Chirp Flatness
close all;
GenerateAodWaveFile( ...
    GenerateAodWaveFileConstants.FLATNESS_BANDWIDTH, ... % 带宽 (MHz)
    GenerateAodWaveFileConstants.CENTER_FREQUENCY, ... % 中心频率 (MHz)
    GenerateAodWaveFileConstants.FLATNESS_MONOTONIC_TYPE, ... % 递增, 递减, 平坦: 1, -1, 0
    GenerateAodWaveFileConstants.SAMPLE_RATE, ... % 采样率 (Msa/s)
    GenerateAodWaveFileConstants.AMPLITUDE, ... % 幅值
    GenerateAodWaveFileConstants.getOutputDirectory('Chirp\Flatness'), ... % 生成文件的目录
    GenerateAodWaveFileConstants.ZERO_SAMPLE_COUNT, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
    GenerateAodWaveFileConstants.CHIRP_MODE, ... % 平坦时间 (ns), -1 启用chirp
    GenerateAodWaveFileConstants.SOUND_PACKET_LENGTH, ... % 音包长度 (mm), -1 启用prescan
    GenerateAodWaveFileConstants.ENDPOINT_BUFFER_SAMPLES, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
    GenerateAodWaveFileConstants.OFFSET_FREQUENCY, ... % 偏移的频率
    GenerateAodWaveFileConstants.OFFSET_PERIOD_MULTIPLE_LOW, ... % 偏移的频率的2π周期的倍率
    GenerateAodWaveFileConstants.FLATNESS_SINC_COEFFICIENT, ... % sinc系数
    GenerateAodWaveFileConstants.FLATNESS_ASTIGMATISM_COMPENSATION, ... % 二次补偿系数t^2 散光
    GenerateAodWaveFileConstants.FLATNESS_SPHERICAL_ABERRATION_COMPENSATION, ... % 三次补偿系数t^3 球差
    GenerateAodWaveFileConstants.FLATNESS_SECONDARY_ASTIGMATISM_COMPENSATION, ... % 四次补偿系数t^4 二阶散光
    GenerateAodWaveFileConstants.FLATNESS_COMA_COMPENSATION, ... % sin(2πt/T)
    GenerateAodWaveFileConstants.FLATNESS_TREFOIL_COMPENSATION, ... % sin(6πt/T)
    GenerateAodWaveFileConstants.FLATNESS_QUADRAFOIL_COMPENSATION, ... % sin(8πt/T)
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER, ... % α次补偿
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER_COEFFICIENT, ... % α次补偿系数t^α
    GenerateAodWaveFileConstants.EMPTY_FILE_PATH, ... % 频率幅值文件路径
    GenerateAodWaveFileConstants.EMPTY_SLOPE_CONFIG, ... % AOD波形的斜率变化率分段的配置项集合
    GenerateAodWaveFileConstants.GENERATE_RETRY_TIMES ... % 重试次数
)

close all;
GenerateAodWaveFile( ...
    GenerateAodWaveFileConstants.FLATNESS_BANDWIDTH, ... % 带宽 (MHz)
    GenerateAodWaveFileConstants.CENTER_FREQUENCY, ... % 中心频率 (MHz)
    GenerateAodWaveFileConstants.FLATNESS_MONOTONIC_TYPE, ... % 递增, 递减, 平坦: 1, -1, 0
    GenerateAodWaveFileConstants.SAMPLE_RATE, ... % 采样率 (Msa/s)
    GenerateAodWaveFileConstants.AMPLITUDE, ... % 幅值
    GenerateAodWaveFileConstants.getOutputDirectory('Chirp\Flatness'), ... % 生成文件的目录
    GenerateAodWaveFileConstants.ZERO_SAMPLE_COUNT, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
    GenerateAodWaveFileConstants.CHIRP_MODE, ... % 平坦时间 (ns), -1 启用chirp
    GenerateAodWaveFileConstants.SOUND_PACKET_LENGTH, ... % 音包长度 (mm), -1 启用prescan
    GenerateAodWaveFileConstants.ENDPOINT_BUFFER_SAMPLES, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
    GenerateAodWaveFileConstants.OFFSET_FREQUENCY, ... % 偏移的频率
    GenerateAodWaveFileConstants.OFFSET_PERIOD_MULTIPLE_HIGH, ... % 偏移的频率的2π周期的倍率
    GenerateAodWaveFileConstants.FLATNESS_SINC_COEFFICIENT, ... % sinc系数
    GenerateAodWaveFileConstants.FLATNESS_ASTIGMATISM_COMPENSATION, ... % 二次补偿系数t^2 散光
    GenerateAodWaveFileConstants.FLATNESS_SPHERICAL_ABERRATION_COMPENSATION, ... % 三次补偿系数t^3 球差
    GenerateAodWaveFileConstants.FLATNESS_SECONDARY_ASTIGMATISM_COMPENSATION, ... % 四次补偿系数t^4 二阶散光
    GenerateAodWaveFileConstants.FLATNESS_COMA_COMPENSATION, ... % sin(2πt/T)
    GenerateAodWaveFileConstants.FLATNESS_TREFOIL_COMPENSATION, ... % sin(6πt/T)
    GenerateAodWaveFileConstants.FLATNESS_QUADRAFOIL_COMPENSATION, ... % sin(8πt/T)
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER, ... % α次补偿
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER_COEFFICIENT, ... % α次补偿系数t^α
    GenerateAodWaveFileConstants.EMPTY_FILE_PATH, ... % 频率幅值文件路径
    GenerateAodWaveFileConstants.EMPTY_SLOPE_CONFIG, ... % AOD波形的斜率变化率分段的配置项集合
    GenerateAodWaveFileConstants.GENERATE_RETRY_TIMES ... % 重试次数
)

%% Prescan Flatness
close all;
GenerateAodWaveFile( ...
    GenerateAodWaveFileConstants.FLATNESS_BANDWIDTH, ... % 带宽 (MHz)
    GenerateAodWaveFileConstants.CENTER_FREQUENCY, ... % 中心频率 (MHz)
    GenerateAodWaveFileConstants.FLATNESS_MONOTONIC_TYPE, ... % 递增, 递减, 平坦: 1, -1, 0
    GenerateAodWaveFileConstants.SAMPLE_RATE, ... % 采样率 (Msa/s)
    GenerateAodWaveFileConstants.AMPLITUDE, ... % 幅值
    GenerateAodWaveFileConstants.getOutputDirectory('Prescan\Flatness'), ... % 生成文件的目录
    GenerateAodWaveFileConstants.ZERO_SAMPLE_COUNT, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
    GenerateAodWaveFileConstants.FLATNESS_TIME, ... % 平坦时间 (ns), -1 启用chirp
    GenerateAodWaveFileConstants.PRESCAN_MODE, ... % 音包长度 (mm), -1 启用prescan
    GenerateAodWaveFileConstants.ENDPOINT_BUFFER_SAMPLES, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
    GenerateAodWaveFileConstants.OFFSET_FREQUENCY, ... % 偏移的频率
    GenerateAodWaveFileConstants.OFFSET_PERIOD_MULTIPLE_LOW, ... % 偏移的频率的2π周期的倍率
    GenerateAodWaveFileConstants.FLATNESS_SINC_COEFFICIENT, ... % sinc系数
    GenerateAodWaveFileConstants.FLATNESS_ASTIGMATISM_COMPENSATION, ... % 二次补偿系数t^2 散光
    GenerateAodWaveFileConstants.FLATNESS_SPHERICAL_ABERRATION_COMPENSATION, ... % 三次补偿系数t^3 球差
    GenerateAodWaveFileConstants.FLATNESS_SECONDARY_ASTIGMATISM_COMPENSATION, ... % 四次补偿系数t^4 二阶散光
    GenerateAodWaveFileConstants.FLATNESS_COMA_COMPENSATION, ... % sin(2πt/T)
    GenerateAodWaveFileConstants.FLATNESS_TREFOIL_COMPENSATION, ... % sin(6πt/T)
    GenerateAodWaveFileConstants.FLATNESS_QUADRAFOIL_COMPENSATION, ... % sin(8πt/T)
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER, ... % α次补偿
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER_COEFFICIENT, ... % α次补偿系数t^α
    GenerateAodWaveFileConstants.EMPTY_FILE_PATH, ... % 频率幅值文件路径
    GenerateAodWaveFileConstants.EMPTY_SLOPE_CONFIG, ... % AOD波形的斜率变化率分段的配置项集合
    GenerateAodWaveFileConstants.GENERATE_RETRY_TIMES ... % 重试次数
)

close all;
GenerateAodWaveFile( ...
    GenerateAodWaveFileConstants.FLATNESS_BANDWIDTH, ... % 带宽 (MHz)
    GenerateAodWaveFileConstants.CENTER_FREQUENCY, ... % 中心频率 (MHz)
    GenerateAodWaveFileConstants.FLATNESS_MONOTONIC_TYPE, ... % 递增, 递减, 平坦: 1, -1, 0
    GenerateAodWaveFileConstants.SAMPLE_RATE, ... % 采样率 (Msa/s)
    GenerateAodWaveFileConstants.AMPLITUDE, ... % 幅值
    GenerateAodWaveFileConstants.getOutputDirectory('Prescan\Flatness'), ... % 生成文件的目录
    GenerateAodWaveFileConstants.ZERO_SAMPLE_COUNT, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
    GenerateAodWaveFileConstants.FLATNESS_TIME, ... % 平坦时间 (ns), -1 启用chirp
    GenerateAodWaveFileConstants.PRESCAN_MODE, ... % 音包长度 (mm), -1 启用prescan
    GenerateAodWaveFileConstants.ENDPOINT_BUFFER_SAMPLES, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
    GenerateAodWaveFileConstants.OFFSET_FREQUENCY, ... % 偏移的频率
    GenerateAodWaveFileConstants.OFFSET_PERIOD_MULTIPLE_HIGH, ... % 偏移的频率的2π周期的倍率
    GenerateAodWaveFileConstants.FLATNESS_SINC_COEFFICIENT, ... % sinc系数
    GenerateAodWaveFileConstants.FLATNESS_ASTIGMATISM_COMPENSATION, ... % 二次补偿系数t^2 散光
    GenerateAodWaveFileConstants.FLATNESS_SPHERICAL_ABERRATION_COMPENSATION, ... % 三次补偿系数t^3 球差
    GenerateAodWaveFileConstants.FLATNESS_SECONDARY_ASTIGMATISM_COMPENSATION, ... % 四次补偿系数t^4 二阶散光
    GenerateAodWaveFileConstants.FLATNESS_COMA_COMPENSATION, ... % sin(2πt/T)
    GenerateAodWaveFileConstants.FLATNESS_TREFOIL_COMPENSATION, ... % sin(6πt/T)
    GenerateAodWaveFileConstants.FLATNESS_QUADRAFOIL_COMPENSATION, ... % sin(8πt/T)
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER, ... % α次补偿
    GenerateAodWaveFileConstants.FLATNESS_ALPHA_ORDER_COEFFICIENT, ... % α次补偿系数t^α
    GenerateAodWaveFileConstants.EMPTY_FILE_PATH, ... % 频率幅值文件路径
    GenerateAodWaveFileConstants.EMPTY_SLOPE_CONFIG, ... % AOD波形的斜率变化率分段的配置项集合
    GenerateAodWaveFileConstants.GENERATE_RETRY_TIMES ... % 重试次数
)
