function aodWaveFilePath = GenerateAodWaveFile( ...
        bandWidth, ... % 带宽 (MHz)
        centerFrequency, ... % 中心频率 (MHz)
        monotonicTypeEnum, ... % 递增, 递减, 平坦: 1, -1, 0
        sampleRate, ... % 采样率 (Msa/s)
        amplitude, ... % 幅值
        aodWaveDirectory, ... % 生成文件的目录
        zeroSampleCount, ... % 前面添加多少补零采样点个数, 相当于添加延迟(sa)
        flatnessTime, ... % 平坦时间 (ns), -1 启用chirp
        soundPacketLength, ... % 音包长度 (mm)
        endpointNumberOfSamples, ... % 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
        offsetFrequency, ... % 偏移的频率
        offsetFrequencyPeriodMultiple, ... % 偏移的频率的2π周期的倍率
        sincCoefficient, ... % sinc系数
        astigmatismCompensationCoefficient, ... % 二次补偿系数t^2 散光
        sphericalAberrationCompensationCoefficient, ... % 三次补偿系数t^3 球差
        secondaryAstigmatismCompensationCoefficient, ... % 四次补偿系数t^4 二阶散光
        comaCompensationCoefficient, ... % sin(2πt/T)
        trefoilCompensationCoefficient, ... % sin(6πt/T)
        quadrafoilCompensationCoefficient, ... % sin(8πt/T)
        alphaOrder, ... % α次补偿
        alphaOrderCoefficient, ... % α次补偿系数t^α
        frequencyAmplitudeFilePath, ... % 频率幅值文件路径
        slopeDeltaKs, ... % AOD波形的斜率变化率分段的配置项集合
        generateRetryTimes ... % 重试次数
    )
    %% 参数

    if monotonicTypeEnum ~= 1 && monotonicTypeEnum ~= -1 && monotonicTypeEnum ~= 0
        error('[monotonicTypeEnum] must be 1, -1, or 0');
    end

    if monotonicTypeEnum ~= 0 && bandWidth <= 0
        error('[bandWidth] must be > 0 when [monotonicTypeEnum] is not 0');
    end

    if monotonicTypeEnum == 0 && bandWidth ~= 0
        error('[bandWidth] must be 0 when [monotonicTypeEnum] is 0');
    end

    if centerFrequency <= 0
        error('[centerFrequency] must be > 0');
    end

    if sampleRate <= 0
        error('[samplingFrequency] must be > 0');
    end

    if amplitude <= 0 || amplitude > 1
        error('[amplitude] must be in the range (0, 1]');
    end

    if zeroSampleCount < 0
        error('[zeroSampleCount] must be >= 0');
    end

    SOUND_SPEED_MM_PER_US = 5.742; % 音速 (mm/us)

    if flatnessTime == -1

        if soundPacketLength <= 0
            error('[flatnessTime] == -1 : [soundPacketLength] must be > 0');
        end

        flatnessTime = round(soundPacketLength / SOUND_SPEED_MM_PER_US * 1000); % (ns): mm/(mm/us) * 1000 = us * 1000 = ns
    end

    if flatnessTime <= 0
        error('[flatnessTime] must be > 0');
    end

    if endpointNumberOfSamples < 0
        error('[endpointNumberOfSamples] must be >= 0');
    end

    switch monotonicTypeEnum
        case {1, -1}
            lowFrequency = centerFrequency - bandWidth / 2;
            highFrequency = centerFrequency + bandWidth / 2;
        case 0
            lowFrequency = centerFrequency;
            highFrequency = centerFrequency;
    end

    readonlyBandWidth = bandWidth;
    readonlyCenterFrequency = centerFrequency;
    readonlyLowFrequency = lowFrequency;
    readonlyHighFrequency = highFrequency;

    numberOfSamples = round(flatnessTime * sampleRate / 1000 + 2 * endpointNumberOfSamples); % 计算总采样点数(sa): ns * (Msa/s) / 1000 = ns * (Gsa/s) = (10^-9s)*(10^9sa/s) = sa
    dt = 1 / sampleRate; % 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

    %% 采样点集合

    allSampleIndices = (1:numberOfSamples)';
    headerSampleIndices = (1:endpointNumberOfSamples)';
    flatnessSampleIndices = (endpointNumberOfSamples + 1:numberOfSamples - endpointNumberOfSamples)';
    footerSampleIndices = (numberOfSamples - endpointNumberOfSamples + 1:numberOfSamples)';
    aodWaveSignals = zeros(numberOfSamples, 1);

    %% 文件名

    if ~exist(aodWaveDirectory, 'dir')
        mkdir(aodWaveDirectory);
    end

    switch monotonicTypeEnum
        case 1
            frequencyFileName = sprintf('%0.3fMhz_%0.3fMhz', readonlyLowFrequency, readonlyHighFrequency);
        case -1
            frequencyFileName = sprintf('%0.3fMhz_%0.3fMhz', readonlyHighFrequency, readonlyLowFrequency);
        case 0
            frequencyFileName = sprintf('%0.3fMhz_%0.3fMhz', readonlyCenterFrequency, readonlyCenterFrequency);
    end

    if soundPacketLength == -1
        aodWaveFilePath = fullfile(aodWaveDirectory, ...
            sprintf('prescan_%0.3fBWMhz_%s_%0.3fns_%0.3fAMP_%0.3fastigmatism_%0.3fsphericalAberration_%0.3fsecondaryAstigmatism_%0.3fcomaCompensationCoefficient_%dCount_$%d$%d$600$02$%s$%s$.txt', ...
            readonlyBandWidth, ...
            frequencyFileName, ...
            flatnessTime, ...
            amplitude, ...
            astigmatismCompensationCoefficient, ...
            sphericalAberrationCompensationCoefficient, ...
            secondaryAstigmatismCompensationCoefficient, ...
            comaCompensationCoefficient, ...
            numberOfSamples, ...
            numberOfSamples + zeroSampleCount, ...
            zeroSampleCount, ...
            regexprep(sprintf('%.3f', offsetFrequency), '\.?0+$', ''), ...
            regexprep(sprintf('%.3f', offsetFrequencyPeriodMultiple), '\.?0+$', '')));
    else
        aodWaveFilePath = fullfile(aodWaveDirectory, ...
            sprintf('chirp_%0.3fmm_%0.3fBWMhz_%s_%0.3fns_%0.3fAMP_%0.3fastigmatism_%0.3fsphericalAberration_%0.3fsecondaryAstigmatism_%0.3fcomaCompensationCoefficient%dCount_$%d$%d$600$03$%s$%s$.txt', ...
            soundPacketLength, ...
            readonlyBandWidth, ...
            frequencyFileName, ...
            flatnessTime, ...
            amplitude, ...
            astigmatismCompensationCoefficient, ...
            sphericalAberrationCompensationCoefficient, ...
            secondaryAstigmatismCompensationCoefficient, ...
            comaCompensationCoefficient, ...
            numberOfSamples, ...
            numberOfSamples + zeroSampleCount, ...
            zeroSampleCount, ...
            regexprep(sprintf('%.3f', offsetFrequency), '\.?0+$', ''), ...
            regexprep(sprintf('%.3f', offsetFrequencyPeriodMultiple), '\.?0+$', '')));
    end

    %% 波形生成

    isOk = 0;
    count = 1;

    while isOk == 0

        %% 频率

        t = (flatnessSampleIndices - flatnessSampleIndices(1)) * dt; % (us): sa * (us/sa) = us

        switch monotonicTypeEnum
            case {1, -1}
                kSegments = ones(length(flatnessSampleIndices), 1) * (bandWidth / (length(flatnessSampleIndices) -1));
                slopeDeltaKConfigurationsCount = length(slopeDeltaKs);

                if slopeDeltaKConfigurationsCount > 0
                    segmentLength = floor(length(flatnessSampleIndices) / slopeDeltaKConfigurationsCount);

                    for i = 1:slopeDeltaKConfigurationsCount
                        startIndex = (i - 1) * segmentLength + 1;

                        if i == slopeDeltaKConfigurationsCount
                            endIndex = length(flatnessSampleIndices);
                        else
                            endIndex = i * segmentLength;
                        end

                        kSegments(startIndex:endIndex) = kSegments(startIndex:endIndex) + slopeDeltaKs(i);
                    end

                end

                % 从0开始积分，结果为[0, bandWidth]
                kSegments = [0; kSegments(1:end - 1)];
                dLinearFrequencies = cumsum(kSegments);
                dAstigmatismFrequencies = astigmatismCompensationCoefficient * t .^ 2;
                dSphericalAberrationFrequencies = sphericalAberrationCompensationCoefficient * t .^ 3;
                dSecondaryAstigmatismFrequencies = secondaryAstigmatismCompensationCoefficient * t .^ 4;
                dComaFrequencies = comaCompensationCoefficient * sin(2 * pi * t / max(t));
                dTrefoilFrequencies = trefoilCompensationCoefficient * sin(6 * pi * t / max(t));
                dQuadrafoilFrequencies = quadrafoilCompensationCoefficient * sin(8 * pi * t / max(t));
                dAlphaOrderFrequencies = alphaOrderCoefficient * (t .^ alphaOrder);
            case 0
                dLinearFrequencies = zeros(length(t), 1);
                dAstigmatismFrequencies = zeros(length(t), 1);
                dSphericalAberrationFrequencies = zeros(length(t), 1);
                dSecondaryAstigmatismFrequencies = zeros(length(t), 1);
                dComaFrequencies = zeros(length(t), 1);
                dTrefoilFrequencies = zeros(length(t), 1);
                dQuadrafoilFrequencies = zeros(length(t), 1);
                dAlphaOrderFrequencies = zeros(length(t), 1);
        end

        switch monotonicTypeEnum
            case 1
                dHeaderFrequencies = ones(length(headerSampleIndices), 1) * lowFrequency;
                dFooterFrequencies = ones(length(footerSampleIndices), 1) * highFrequency;
                dFlatnessFrequencies = lowFrequency + dLinearFrequencies + dAstigmatismFrequencies + dSphericalAberrationFrequencies + dSecondaryAstigmatismFrequencies + dComaFrequencies + dTrefoilFrequencies + dQuadrafoilFrequencies + dAlphaOrderFrequencies;
            case -1
                dHeaderFrequencies = ones(length(headerSampleIndices), 1) * highFrequency;
                dFooterFrequencies = ones(length(footerSampleIndices), 1) * lowFrequency;
                dFlatnessFrequencies = highFrequency - dLinearFrequencies - dAstigmatismFrequencies - dSphericalAberrationFrequencies - dSecondaryAstigmatismFrequencies - dComaFrequencies - dTrefoilFrequencies - dQuadrafoilFrequencies - dAlphaOrderFrequencies;
            case 0
                dHeaderFrequencies = ones(length(headerSampleIndices), 1) * centerFrequency;
                dFooterFrequencies = ones(length(footerSampleIndices), 1) * centerFrequency;
                dFlatnessFrequencies = ones(length(flatnessSampleIndices), 1) * centerFrequency;
        end

        if any(dFlatnessFrequencies <= 0) || any(dFlatnessFrequencies >= readonlyHighFrequency * 1.5)
            PlotAodWaveAnalysis( ...
                flatnessSampleIndices, ...
                dLinearFrequencies, ...
                dAstigmatismFrequencies, ...
                dSphericalAberrationFrequencies, ...
                dSecondaryAstigmatismFrequencies, ...
                dComaFrequencies, ...
                dTrefoilFrequencies, ...
                dQuadrafoilFrequencies, ...
                alphaOrder, ...
                dAlphaOrderFrequencies, ...
                dFlatnessFrequencies, ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                NaN, ...
                NaN, ...
                [], ...
                [], ...
                [], ...
                [], ...
                [], ...
                aodWaveFilePath);
            error('Synthesized frequencies must be in the range (0, %0.3f MHz)', readonlyHighFrequency * 1.5);
        end

        %% 相位

        dHeaderPhases = 2 * pi * dHeaderFrequencies * dt;
        headerPhases = cumsum(dHeaderPhases);

        dFlatnessPhases = 2 * pi * dFlatnessFrequencies * dt;
        flatnessPhases = cumsum(dFlatnessPhases);

        dFooterPhases = 2 * pi * dFooterFrequencies * dt;
        footerPhases = cumsum(dFooterPhases);

        %% 波形

        aodWaveSignals(headerSampleIndices) = cos(headerPhases) .* ((headerSampleIndices - min(headerSampleIndices)) / length(headerSampleIndices)) * amplitude;
        aodWaveSignals(flatnessSampleIndices) = cos(flatnessPhases) * amplitude;
        aodWaveSignals(footerSampleIndices) = cos(footerPhases) .* (1 - (footerSampleIndices - min(footerSampleIndices) + 1) / length(footerSampleIndices)) * amplitude;

        %% 傅里叶
        flatnessAodWaveSignals = aodWaveSignals(flatnessSampleIndices);
        fftResult = fft(flatnessAodWaveSignals);

        % 取前一半正频率
        % fftFrequencies = (0:length(flatnessAodWaveSignals) / 2)' * samplingFrequency / length(flatnessAodWaveSignals);
        fftFrequencies = HalfFrequencyScale(length(flatnessAodWaveSignals), sampleRate)';
        fftMagnitudes = abs(fftResult(1:floor(length(flatnessAodWaveSignals) / 2) + 1));

        % fftMagnitudes平滑下
        fftDerivative = diff(fftMagnitudes) ./ diff(fftFrequencies);
        fftDerivativeFrequencies = fftFrequencies(1:end - 1);

        aboveIdx = find(abs(fftDerivative) >= 0.0001);

        fftDerivativeSign = zeros(size(fftDerivative));
        fftDerivativeSign(aboveIdx(1):aboveIdx(end)) = sign(fftDerivative(aboveIdx(1):aboveIdx(end)));
        fftDerivativeSign(1:aboveIdx(1) - 1) = fftDerivativeSign(aboveIdx(1));
        fftDerivativeSign(aboveIdx(end) + 1:end) = fftDerivativeSign(aboveIdx(end));

        fftExtremumPointIndices = find(diff(fftDerivativeSign) ~= 0) + 1;
        fftExtremumPointFrequencies = fftFrequencies(fftExtremumPointIndices);
        fftExtremumPointMagnitudes = fftMagnitudes(fftExtremumPointIndices);

        minFlatnessFrequency = fftExtremumPointFrequencies(1);
        maxFlatnessFrequency = fftExtremumPointFrequencies(end);

        fftSecondDerivative = diff(fftDerivative) ./ diff(fftDerivativeFrequencies);
        fftSecondDerivativeFrequencies = fftDerivativeFrequencies(1:end - 1);

        aboveIdx = find(abs(fftSecondDerivative) >= 0.0001);

        fftSecondDerivativeSign = zeros(size(fftSecondDerivative));
        fftSecondDerivativeSign(aboveIdx(1):aboveIdx(end)) = sign(fftSecondDerivative(aboveIdx(1):aboveIdx(end)));
        fftSecondDerivativeSign(1:aboveIdx(1) - 1) = fftSecondDerivativeSign(aboveIdx(1));
        fftSecondDerivativeSign(aboveIdx(end) + 1:end) = fftSecondDerivativeSign(aboveIdx(end));

        fftInflectionPointIndices = find(diff(fftSecondDerivativeSign) ~= 0) + 1;
        fftInflectionPointFrequencies = fftFrequencies(fftInflectionPointIndices);
        fftInflectionPointMagnitudes = fftMagnitudes(fftInflectionPointIndices);

        if monotonicTypeEnum == 0

            if minFlatnessFrequency ~= maxFlatnessFrequency
                PlotAodWaveAnalysis( ...
                    flatnessSampleIndices, ...
                    dLinearFrequencies, ...
                    dAstigmatismFrequencies, ...
                    dSphericalAberrationFrequencies, ...
                    dSecondaryAstigmatismFrequencies, ...
                    dComaFrequencies, ...
                    dTrefoilFrequencies, ...
                    dQuadrafoilFrequencies, ...
                    alphaOrder, ...
                    dAlphaOrderFrequencies, ...
                    dFlatnessFrequencies, ...
                    flatnessPhases, ...
                    [], ...
                    [], ...
                    allSampleIndices, ...
                    aodWaveSignals, ...
                    fftFrequencies, ...
                    fftMagnitudes, ...
                    [], ...
                    [], ...
                    fftExtremumPointFrequencies, ...
                    fftExtremumPointMagnitudes, ...
                    fftInflectionPointFrequencies, ...
                    fftInflectionPointMagnitudes, ...
                    minFlatnessFrequency, ...
                    maxFlatnessFrequency, ...
                    fftDerivativeFrequencies, ...
                    fftDerivative, ...
                    fftDerivativeSign, ...
                    fftSecondDerivativeFrequencies, ...
                    fftSecondDerivative, ...
                    aodWaveFilePath);
                error('Left and right frequencies cannot be the same when monotonicTypeEnum is 0');
            end

        else

            leftIdx = find(fftInflectionPointFrequencies < minFlatnessFrequency, 1, 'last');

            if ~isempty(leftIdx)
                minFlatnessFrequency = fftInflectionPointFrequencies(leftIdx);
            else
                minFlatnessFrequency = NaN;
            end

            rightIdx = find(fftInflectionPointFrequencies > maxFlatnessFrequency, 1, 'first');

            if ~isempty(rightIdx)
                maxFlatnessFrequency = fftInflectionPointFrequencies(rightIdx);
            else
                maxFlatnessFrequency = NaN;
            end

            if isnan(minFlatnessFrequency) || isnan(maxFlatnessFrequency)
                PlotAodWaveAnalysis( ...
                    flatnessSampleIndices, ...
                    dLinearFrequencies, ...
                    dAstigmatismFrequencies, ...
                    dSphericalAberrationFrequencies, ...
                    dSecondaryAstigmatismFrequencies, ...
                    dComaFrequencies, ...
                    dTrefoilFrequencies, ...
                    dQuadrafoilFrequencies, ...
                    alphaOrder, ...
                    dAlphaOrderFrequencies, ...
                    dFlatnessFrequencies, ...
                    flatnessPhases, ...
                    [], ...
                    [], ...
                    allSampleIndices, ...
                    aodWaveSignals, ...
                    fftFrequencies, ...
                    fftMagnitudes, ...
                    [], ...
                    [], ...
                    fftExtremumPointFrequencies, ...
                    fftExtremumPointMagnitudes, ...
                    fftInflectionPointFrequencies, ...
                    fftInflectionPointMagnitudes, ...
                    minFlatnessFrequency, ...
                    maxFlatnessFrequency, ...
                    fftDerivativeFrequencies, ...
                    fftDerivative, ...
                    fftDerivativeSign, ...
                    fftSecondDerivativeFrequencies, ...
                    fftSecondDerivative, ...
                    aodWaveFilePath);
                error('Failed to find valid frequency edges');
            end

        end

        switch monotonicTypeEnum
            case 0

                if abs(minFlatnessFrequency - readonlyCenterFrequency) < (sampleRate / length(flatnessSampleIndices))
                    isOk = 1;
                elseif minFlatnessFrequency < readonlyCenterFrequency
                    centerFrequency = centerFrequency + sampleRate / (2 * length(flatnessSampleIndices));
                else
                    centerFrequency = centerFrequency - sampleRate / (2 * length(flatnessSampleIndices));
                end

            case 1

                if abs(minFlatnessFrequency - readonlyLowFrequency) < (sampleRate / length(flatnessSampleIndices))

                    if abs(maxFlatnessFrequency - readonlyHighFrequency) < (sampleRate / length(flatnessSampleIndices))
                        isOk = 1;
                    elseif maxFlatnessFrequency < readonlyHighFrequency
                        bandWidth = bandWidth + sampleRate / (2 * length(flatnessSampleIndices));
                    else
                        bandWidth = bandWidth - sampleRate / (2 * length(flatnessSampleIndices));
                    end

                elseif minFlatnessFrequency < readonlyLowFrequency
                    lowFrequency = lowFrequency + sampleRate / (2 * length(flatnessSampleIndices));
                else
                    lowFrequency = lowFrequency - sampleRate / (2 * length(flatnessSampleIndices));
                end

            case -1

                if abs(maxFlatnessFrequency - readonlyHighFrequency) < (sampleRate / length(flatnessSampleIndices))

                    if abs(minFlatnessFrequency - readonlyLowFrequency) < (sampleRate / length(flatnessSampleIndices))
                        isOk = 1;
                    elseif minFlatnessFrequency < readonlyLowFrequency
                        bandWidth = bandWidth - sampleRate / (2 * length(flatnessSampleIndices));
                    else
                        bandWidth = bandWidth + sampleRate / (2 * length(flatnessSampleIndices));
                    end

                elseif maxFlatnessFrequency < readonlyHighFrequency
                    highFrequency = highFrequency + sampleRate / (2 * length(flatnessSampleIndices));
                else
                    highFrequency = highFrequency - sampleRate / (2 * length(flatnessSampleIndices));
                end

        end

        %% 画图
        if isOk == 1
            %% 相位

            dHeaderPhases = 2 * pi * dHeaderFrequencies * dt;
            headerPhases = cumsum(dHeaderPhases);

            dFlatnessPhases = 2 * pi * dFlatnessFrequencies * dt;
            flatnessPhases = cumsum(dFlatnessPhases) + 2 * pi * dFlatnessFrequencies * offsetFrequencyPeriodMultiple * 1 / offsetFrequency;

            dFooterPhases = 2 * pi * dFooterFrequencies * dt;
            footerPhases = cumsum(dFooterPhases);

            %% 波形

            aodWaveSignals(headerSampleIndices) = cos(headerPhases) .* ((headerSampleIndices - min(headerSampleIndices)) / length(headerSampleIndices)) * amplitude;
            aodWaveSignals(flatnessSampleIndices) = cos(flatnessPhases) * amplitude;
            aodWaveSignals(footerSampleIndices) = cos(footerPhases) .* (1 - (footerSampleIndices - min(footerSampleIndices) + 1) / length(footerSampleIndices)) * amplitude;

            %% 傅里叶
            flatnessAodWaveSignals = aodWaveSignals(flatnessSampleIndices);
            fftResult = fft(flatnessAodWaveSignals);

            % 取前一半正频率
            % fftFrequencies = (0:length(flatnessAodWaveSignals) / 2)' * samplingFrequency / length(flatnessAodWaveSignals);
            fftFrequencies = HalfFrequencyScale(length(flatnessAodWaveSignals), sampleRate)';
            fftMagnitudes = abs(fftResult(1:floor(length(flatnessAodWaveSignals) / 2) + 1));

            % fftShiftFrequencies = (-length(flatnessAodWaveSignals) / 2:length(flatnessAodWaveSignals) / 2 - 1)' * samplingFrequency / length(flatnessAodWaveSignals);
            % fftShiftFrequencies = fftshift(FrequencyScale(length(flatnessAodWaveSignals), samplingFrequency));
            fftShiftFrequencies = FrequencyScale(length(flatnessAodWaveSignals), sampleRate)';

            % fftShiftResult = fftshift(fftResult);
            fftShiftResult = fftResult;
            % fftShiftMagnitudes = abs(fftshift(fftResult));
            % fftShiftPhases = angle(fftshift(fftResult));

            targetIndices = find((abs(fftShiftFrequencies) >= minFlatnessFrequency) & (abs(fftShiftFrequencies) <= maxFlatnessFrequency));

            if exist(frequencyAmplitudeFilePath, 'file')
                freqAmpData = readtable(frequencyAmplitudeFilePath);
                [sortedFrequencies, sortIdx] = sort(freqAmpData.Frequency);
                sortedAmplitudes = freqAmpData.Coefficient(sortIdx);

                positiveCount = sum(fftShiftFrequencies(targetIndices) >= 0);
                frequencyCoefficientFrequencies = zeros(positiveCount, 1);
                frequencyCoefficientAmplitudes = zeros(positiveCount, 1);
                posCounter = 1;

                for idx = 1:length(targetIndices)
                    currentIdx = targetIndices(idx);
                    f = fftShiftFrequencies(currentIdx);
                    currentFreq = abs(f);

                    if currentFreq <= sortedFrequencies(1)
                        ampCoeff = sortedAmplitudes(1);
                    elseif currentFreq >= sortedFrequencies(end)
                        ampCoeff = sortedAmplitudes(end);
                    else
                        ampCoeff = interp1(sortedFrequencies, sortedAmplitudes, currentFreq, 'linear', 'extrap');
                    end

                    % fftShiftMagnitudes(currentIdx) = fftShiftMagnitudes(currentIdx) * ampCoeff;
                    fftShiftResult(currentIdx) = fftShiftResult(currentIdx) * ampCoeff;

                    if f >= 0
                        frequencyCoefficientFrequencies(posCounter) = currentFreq;
                        frequencyCoefficientAmplitudes(posCounter) = ampCoeff;
                        posCounter = posCounter + 1;
                    end

                end

            elseif sincCoefficient ~= 0
                flatnessBandwidth = maxFlatnessFrequency - minFlatnessFrequency;
                flatnessCenterFrequency = minFlatnessFrequency + flatnessBandwidth / 2;
                frequencyCoefficientFrequencies = zeros(length(targetIndices), 1);
                frequencyCoefficientAmplitudes = zeros(length(targetIndices), 1);
                posCounter = 1;

                for idx = 1:length(targetIndices)
                    currentIdx = targetIndices(idx);
                    f = fftShiftFrequencies(currentIdx);
                    currentFreq = abs(f);

                    x = sincCoefficient * (currentFreq - flatnessCenterFrequency) / flatnessBandwidth;

                    if x == 0
                        ampCoeff = 1; % sinc(0) = 1
                    else
                        ampCoeff = 1 / (sin(x) / x);
                    end

                    if ampCoeff == 0
                        ampCoeff = 0.1;
                    end

                    frequencyCoefficientFrequencies(posCounter) = f;
                    frequencyCoefficientAmplitudes(posCounter) = ampCoeff;
                    posCounter = posCounter + 1;

                end

                frequencyCoefficientAmplitudes = frequencyCoefficientAmplitudes / max(abs(frequencyCoefficientAmplitudes));

                for idx = 1:length(targetIndices)
                    currentIdx = targetIndices(idx);
                    % fftShiftMagnitudes(currentIdx) = fftShiftMagnitudes(currentIdx) * frequencyCoefficientAmplitudes(idx);
                    fftShiftResult(currentIdx) = fftShiftResult(currentIdx) * frequencyCoefficientAmplitudes(idx);
                end

                % frequencyCoefficientFrequencies过滤只显示正频率
                positiveIndices = frequencyCoefficientFrequencies >= 0;
                frequencyCoefficientFrequencies = frequencyCoefficientFrequencies(positiveIndices);
                frequencyCoefficientAmplitudes = frequencyCoefficientAmplitudes(positiveIndices);

            else
                frequencyCoefficientFrequencies = [];
                frequencyCoefficientAmplitudes = [];
            end

            % recoveredSignal = real(ifft(ifftshift(fftShiftMagnitudes .* exp(1i * fftShiftPhases))));
            % recoveredSignal = real(ifft(ifftshift(fftShiftResult)));
            recoveredSignal = real(ifft(fftShiftResult));

            if max(abs(recoveredSignal)) > amplitude
                aodWaveSignals(flatnessSampleIndices) = recoveredSignal / max(abs(recoveredSignal)) * amplitude;
            else
                aodWaveSignals(flatnessSampleIndices) = recoveredSignal;
            end

            fftResultResult = fft(aodWaveSignals(flatnessSampleIndices));

            % fftFrequenciesNew = (0:length(flatnessAodWaveSignals) / 2)' * samplingFrequency / length(flatnessAodWaveSignals);
            fftFrequenciesNew = HalfFrequencyScale(length(flatnessAodWaveSignals), sampleRate)';
            fftMagnitudesNew = abs(fftResultResult(1:floor(length(flatnessAodWaveSignals) / 2) + 1));

            PlotAodWaveAnalysis( ...
                flatnessSampleIndices, ...
                dLinearFrequencies, ...
                dAstigmatismFrequencies, ...
                dSphericalAberrationFrequencies, ...
                dSecondaryAstigmatismFrequencies, ...
                dComaFrequencies, ...
                dTrefoilFrequencies, ...
                dQuadrafoilFrequencies, ...
                alphaOrder, ...
                dAlphaOrderFrequencies, ...
                dFlatnessFrequencies, ...
                flatnessPhases, ...
                frequencyCoefficientFrequencies, ...
                frequencyCoefficientAmplitudes, ...
                allSampleIndices, ...
                aodWaveSignals, ...
                fftFrequencies, ...
                fftMagnitudes, ...
                fftFrequenciesNew, ...
                fftMagnitudesNew, ...
                fftExtremumPointFrequencies, ...
                fftExtremumPointMagnitudes, ...
                fftInflectionPointFrequencies, ...
                fftInflectionPointMagnitudes, ...
                minFlatnessFrequency, ...
                maxFlatnessFrequency, ...
                fftDerivativeFrequencies, ...
                fftDerivative, ...
                fftDerivativeSign, ...
                fftSecondDerivativeFrequencies, ...
                fftSecondDerivative, ...
                aodWaveFilePath);
        end

        count = count + 1;

        if count > generateRetryTimes
            %% 相位

            dHeaderPhases = 2 * pi * dHeaderFrequencies * dt;
            headerPhases = cumsum(dHeaderPhases);

            dFlatnessPhases = 2 * pi * dFlatnessFrequencies * dt;
            flatnessPhases = cumsum(dFlatnessPhases) + 2 * pi * dFlatnessFrequencies * offsetFrequencyPeriodMultiple * 1 / offsetFrequency;

            dFooterPhases = 2 * pi * dFooterFrequencies * dt;
            footerPhases = cumsum(dFooterPhases);

            %% 波形

            aodWaveSignals(headerSampleIndices) = cos(headerPhases) .* ((headerSampleIndices - min(headerSampleIndices)) / length(headerSampleIndices)) * amplitude;
            aodWaveSignals(flatnessSampleIndices) = cos(flatnessPhases) * amplitude;
            aodWaveSignals(footerSampleIndices) = cos(footerPhases) .* (1 - (footerSampleIndices - min(footerSampleIndices) + 1) / length(footerSampleIndices)) * amplitude;

            %% 傅里叶
            flatnessAodWaveSignals = aodWaveSignals(flatnessSampleIndices);
            fftResult = fft(flatnessAodWaveSignals);

            % 取前一半正频率
            % fftFrequencies = (0:length(flatnessAodWaveSignals) / 2)' * samplingFrequency / length(flatnessAodWaveSignals);
            fftFrequencies = HalfFrequencyScale(length(flatnessAodWaveSignals), sampleRate)';
            fftMagnitudes = abs(fftResult(1:length(flatnessAodWaveSignals) / 2 + 1));

            PlotAodWaveAnalysis( ...
                flatnessSampleIndices, ...
                dLinearFrequencies, ...
                dAstigmatismFrequencies, ...
                dSphericalAberrationFrequencies, ...
                dSecondaryAstigmatismFrequencies, ...
                dComaFrequencies, ...
                dTrefoilFrequencies, ...
                dQuadrafoilFrequencies, ...
                alphaOrder, ...
                dAlphaOrderFrequencies, ...
                dFlatnessFrequencies, ...
                flatnessPhases, ...
                [], ...
                [], ...
                allSampleIndices, ...
                aodWaveSignals, ...
                fftFrequencies, ...
                fftMagnitudes, ...
                [], ...
                [], ...
                fftExtremumPointFrequencies, ...
                fftExtremumPointMagnitudes, ...
                fftInflectionPointFrequencies, ...
                fftInflectionPointMagnitudes, ...
                minFlatnessFrequency, ...
                maxFlatnessFrequency, ...
                fftDerivativeFrequencies, ...
                fftDerivative, ...
                fftDerivativeSign, ...
                fftSecondDerivativeFrequencies, ...
                fftSecondDerivative, ...
                aodWaveFilePath);
            warning('Failed to generate valid AOD wave after %d retries', generateRetryTimes);
            break;
        end

    end

    %% 生成文件

    short = int16(2 ^ 15 * aodWaveSignals);
    short = int64(short);

    for i = 1:length(short)

        if short(i) < 0
            short(i) = short(i) + 2 ^ 32;
        end

    end

    strArray = dec2hex(short, 4);
    lastFourChars = strArray(:, end - 3:end);
    stringArray = string(lastFourChars);
    fileID = fopen(aodWaveFilePath, 'wt');

    if fileID == -1
        error('File cannot be opened');
    end

    for i = 1:length(stringArray)

        if i == length(stringArray)
            fprintf(fileID, '%s', stringArray(i));
        else
            fprintf(fileID, '%s\n', stringArray(i));
        end

    end

    fclose(fileID);

end

function PlotAodWaveAnalysis( ...
        flatnessSampleIndices, ...
        dLinearFrequencies, ...
        dQuadraticFrequencies, ...
        dCubicFrequencies, ...
        dQuarticFrequencies, ...
        dComaFrequencies, ...
        dTrefoilFrequencies, ...
        dQuadrafoilFrequencies, ...
        alphaOrder, ...
        dAlphaOrderFrequencies, ...
        dFlatnessFrequencies, ...
        flatnessPhases, ...
        frequencyCoefficientFrequencies, ...
        frequencyCoefficientAmplitudes, ...
        allSampleIndices, ...
        aodWaveSignals, ...
        fftFrequencies, ...
        fftMagnitudes, ...
        fftFrequenciesNew, ...
        fftMagnitudesNew, ...
        fftExtremumPointFrequencies, ...
        fftExtremumPointMagnitudes, ...
        fftInflectionPointFrequencies, ...
        fftInflectionPointMagnitudes, ...
        leftFreq, ...
        rightFreq, ...
        fftDerivativeFrequencies, ...
        fftDerivative, ...
        fftDerivativeSign, ...
        fftSecondDerivativeFrequencies, ...
        fftSecondDerivative, ...
        plotTitle)
    fig = figure('WindowState', 'maximized');
    set(fig, 'Units', 'normalized', 'Position', [0 0 1 1]);
    set(groot, 'DefaultAxesFontSize', 12)
    set(groot, 'DefaultLineLineWidth', 1.5)

    % 1. 频率补偿项分解
    subplot(3, 3, 1);
    hold on;
    plot(flatnessSampleIndices, dLinearFrequencies, 'b', 'LineWidth', 1.5, 'DisplayName', 'Linear');
    plot(flatnessSampleIndices, dQuadraticFrequencies, 'r--', 'LineWidth', 1.5, 'DisplayName', 'Quadratic(Astigmatism)');
    plot(flatnessSampleIndices, dCubicFrequencies, 'g:', 'LineWidth', 1.5, 'DisplayName', 'Cubic(Spherical Aberration)');
    plot(flatnessSampleIndices, dQuarticFrequencies, 'm-.', 'LineWidth', 1.5, 'DisplayName', 'Quartic(2nd-order Astigmatism)');
    plot(flatnessSampleIndices, dComaFrequencies, 'c', 'LineWidth', 1.5, 'DisplayName', 'Coma');
    plot(flatnessSampleIndices, dTrefoilFrequencies, 'k--', 'LineWidth', 1.5, 'DisplayName', 'Trefoil');
    plot(flatnessSampleIndices, dQuadrafoilFrequencies, 'y:', 'LineWidth', 1.5, 'DisplayName', 'Quadrafoil');
    plot(flatnessSampleIndices, dAlphaOrderFrequencies, 'Color', [0.5 0 0.5], 'LineWidth', 1.5, 'DisplayName', sprintf('Alpha Order(%0.3f)', alphaOrder));
    hold off;
    xlabel('Number of Samples');
    ylabel('Frequency (MHz)');
    title('Frequency Compensation Components');
    legend('show', 'Location', 'best');
    grid on; box on;

    % 2. 合成频率
    subplot(3, 3, 2);
    plot(flatnessSampleIndices, dFlatnessFrequencies, 'k', 'LineWidth', 2);
    xlabel('Number of Samples');
    ylabel('Frequency (MHz)');
    title('Synthesized Frequency Profile');
    grid on; box on;

    % 3. 累积相位
    if ~isempty(flatnessPhases)
        subplot(3, 3, 3);
        plot(flatnessSampleIndices, flatnessPhases, 'Color', [0.5 0 0.5], 'LineWidth', 1.5);
        xlabel('Number of Samples');
        ylabel('Phase (radians)');
        title('Cumulative Phase Profile');
        grid on; box on;
    end

    % 4. 动态幅值
    if ~isempty(frequencyCoefficientFrequencies) && ~isempty(frequencyCoefficientAmplitudes)
        subplot(3, 3, 4);
        plot(frequencyCoefficientFrequencies, frequencyCoefficientAmplitudes, 'Color', [0 0.7 0], 'LineWidth', 1.5);
        xlabel('Number of Samples');
        ylabel('Amplitude');
        title('Dynamic Frequency Amplitude Profile');
        grid on; box on;
    end

    % 5. 时域信号
    if ~isempty(allSampleIndices) && ~isempty(aodWaveSignals)
        subplot(3, 3, 5);
        plot(allSampleIndices, aodWaveSignals, 'Color', [0 0.4 0.8], 'LineWidth', 1.5);
        xlabel('Number of Samples');
        ylabel('Amplitude');
        title('Time Domain Signal');
        grid on; box on;
    end

    % 6. 频谱分析
    if ~isempty(fftFrequencies) && ~isempty(fftMagnitudes)
        subplot(3, 3, 6);
        hold on;
        plot(fftFrequencies, fftMagnitudes, 'm', 'DisplayName', 'FFT');
        plot(fftExtremumPointFrequencies, fftExtremumPointMagnitudes, 'ro', 'DisplayName', 'FFT Extrema');
        plot(fftInflectionPointFrequencies, fftInflectionPointMagnitudes, 'g*', 'DisplayName', 'FFT Inflection Points');

        if ~isnan(leftFreq)
            xline(leftFreq, '--b', sprintf('Left Edge: %.3f MHz', leftFreq), ...
                'LabelOrientation', 'horizontal', ...
                'LabelVerticalAlignment', 'top', ...
                'FontSize', 10, ...
                'DisplayName', 'Left Frequency Edge');
        end

        if ~isnan(rightFreq)
            xline(rightFreq, '--k', sprintf('Right Edge: %.3f MHz', rightFreq), ...
                'LabelOrientation', 'horizontal', ...
                'LabelVerticalAlignment', 'middle', ...
                'FontSize', 10, ...
                'DisplayName', 'Right Frequency Edge');
        end

        if ~isempty(fftFrequenciesNew) && ~isempty(fftMagnitudesNew)
            plot(fftFrequenciesNew, fftMagnitudesNew, 'c', 'DisplayName', 'Modified FFT');
        end

        hold off;
        xlabel('Frequency (MHz)');
        ylabel('Magnitude');
        title('Spectrum Analysis');
        legend('show', 'Location', 'best');
        grid on; box on;
    end

    % 7. 一阶导数
    if ~isempty(fftDerivativeFrequencies) && ~isempty(fftDerivative)
        subplot(3, 3, 7);
        hold on;
        plot(fftDerivativeFrequencies, fftDerivative, 'r');
        plot(fftDerivativeFrequencies, fftDerivativeSign, 'g*');
        hold off;
        xlabel('Frequency (MHz)');
        ylabel('Slope');
        title('Derivative of Spectrum');
        grid on; box on;
    end

    % 8. 二阶导数
    if ~isempty(fftSecondDerivativeFrequencies) && ~isempty(fftSecondDerivative)
        subplot(3, 3, 8);
        plot(fftSecondDerivativeFrequencies, fftSecondDerivative, 'b');
        xlabel('Frequency (MHz)');
        ylabel('Slope');
        title('Second Derivative of Spectrum');
        grid on; box on;
    end

    sgtitle(plotTitle, 'FontSize', 14, 'FontWeight', 'bold');
end

function scale = HalfFrequencyScale(length, sampleRate)

    scale = (0:length / 2) * sampleRate / length;

    % 创建频率标度数组，对应 FFT 输出的每个频率分量
    % 输入参数:
    %   length - FFT 长度（样本数量）
    %   sampleRate - 采样率 (Hz)
    % 输出:
    %   scale - 频率数组 (Hz)

    %     f = 0;
    %     step = sampleRate / length;
    %     secondHalf = bitshift(length, -1) + 1; % 等价于 (length >> 1) + 1
    %     scale1 = zeros(1, secondHalf);
    %     % 第一部分：正频率分量（包括DC和Nyquist）
    %     for i = 1:secondHalf
    %         scale1(i) = f;
    %         f = f + step;
    %     end
    %
    % % 找出 scale 和 scale1 不同的位置
    % diff_indices = find(scale ~= scale1);
    %
    % if ~isempty(diff_indices)
    %     disp('不同的元素位置：');
    %     disp(diff_indices);
    %
    %     % 显示不同的值
    %     disp('scale 中的值：');
    %     disp(scale(diff_indices));
    %
    %     disp('scale1 中的值：');
    %     disp(scale1(diff_indices));
    %
    %     error('Frequency scale mismatch');
    % end

end

function scale = FrequencyScale(length, sampleRate)
    scale = zeros(1, length);
    step = sampleRate / length;
    nyquistBin = floor(length / 2) + 1; % Nyquist 频率位置

    % 正频率部分（0 ~ Nyquist）
    scale(1:nyquistBin) = (0:nyquistBin - 1) * step;

    % 负频率部分（-Nyquist ~ -step）
    scale(nyquistBin + 1:end) = (-nyquistBin + 1 + 1:-nyquistBin + 1 + (length - nyquistBin)) * step;

    % 创建频率标度数组，对应 FFT 输出的每个频率分量
    % 输入参数:
    %   length - FFT 长度（样本数量）
    %   sampleRate - 采样率 (Hz)
    % 输出:
    %   scale - 频率数组 (Hz)

    % scale = zeros(1, length);
    % f = 0;
    % step = sampleRate / length;
    % secondHalf = bitshift(length, -1) + 1; % 等价于 (length >> 1) + 1

    % % 第一部分：正频率分量（包括DC和Nyquist）
    % for i = 1:secondHalf
    %     scale(i) = f;
    %     f = f + step;
    % end

    % % 第二部分：负频率分量
    % f = -step * (secondHalf - 2);

    % for i = (secondHalf + 1):length
    %     scale(i) = f;
    %     f = f + step;
    % end

    % diff_indices = find(scale ~= scale1);

    % if ~isempty(diff_indices)
    %     disp('不同的元素位置：');
    %     disp(diff_indices);

    %     % 显示不同的值
    %     disp('scale 中的值：');
    %     disp(scale(diff_indices));

    %     disp('scale1 中的值：');
    %     disp(scale1(diff_indices));

    %     error('Frequency scale mismatch');
    % end

end
