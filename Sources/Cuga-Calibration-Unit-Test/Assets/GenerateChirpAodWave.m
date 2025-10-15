% clear all;
close all;

GenerateChirpAodWaveFile( ...
    "C:\Users\DELL\Documents\chirpAod", ...                                % File path (string, e.g.,"C:\data")
    280, ...                                                               % Start frequency (MHz)
    180, ...                                                               % End frequency (MHz)
    1064, ...                                                              % Sampling rate (MSa/s)
    5, ...                                                                 % Sound packet length (mm)
    [0, 0.01, 0, 0]);                                                      % split delta k
       
function result = GenerateChirpAodWaveFile( ...       
        chirpAodWaceDirectory, ...                                         % File path (string, e.g.,"C:\\data")
        frequencyHigh, ...                                                 % Start frequency (MHz)
        frequencyLow, ...                                                  % End frequency (MHz)
        sampleRate, ...                                                    % Sampling rate (MSa/s)
        soundPacketLength, ...                                             % Sound packet length (mm)
        deltaKs)                                                           % split delta k
    %% Chirp signal parameters

    SOUND_SPEED_MM_US = 5.742; % Sound speed (mm/µs)

    bandwidth = frequencyHigh - frequencyLow;                              % Bandwidth     (MHz)
    flatness = round(soundPacketLength / SOUND_SPEED_MM_US * 1000);        % Flatness time (ns) mm/(mm/us)*1000 = ns(10^-9s)
    totalSampleCount = round(flatness * sampleRate / 1000);                % Total samples (sa) ns*(Msa/s)/1000 = (10^-9s)*(10^6sa/s)/(10^-3) = (10^-3sa)/(10^-3) = sa

    % Generate filename
    result = sprintf('%s\\chirp_%.3fmm_%.3fBWMhz_%.3fMhz_%.3fMhz_%.3fns_%.3fCount.txt', ...
        chirpAodWaceDirectory, ...
        soundPacketLength, ...                                             % mm
        bandwidth, ...                                                     % MHz
        frequencyHigh, ...                                                 % MHz
        frequencyLow, ...                                                  % MHz
        flatness, ...                                                      % ns
        totalSampleCount);                                                 % sa

    %% Signal generation
    sampleIndex = (0:totalSampleCount - 1)';                                  % Sample index (0-based)    (sa)
    k_segments = ones(totalSampleCount, 1) * (bandwidth / totalSampleCount);  % Frequency slope           (MHz/sa)
    % f = frequencyHigh - k * sampleIndex;                                    % Instantaneous frequency   (MHz)

    segmentLength = floor(totalSampleCount / length(deltaKs));
    for seg = 1:length(deltaKs)
        startIdx = (seg - 1) * segmentLength + 1;
        endIdx = min(seg * segmentLength, totalSampleCount);
        k_segments(startIdx:endIdx) = k_segments(startIdx:endIdx) + deltaKs(seg);
    end

    f = frequencyHigh - cumsum(k_segments);

    % Phase calculation
    dt = 1 / sampleRate;                                                   % Sampling interval      (µs/sa)  1/(Msa/s) = 1/(10^6sa/s) = 10^-6s/sa = us/sa
    dphi = 2 * pi * f * dt;                                                % Phase increment        (rad/sa) Mhz*(µs/sa) = (10^6s^-1)*(10^-6s/sa) = rad/sa
    phi = cumsum(dphi);                                                    % Accumulated phase      (rad)    rad/sa*sa = rad

    % Generate Chirp signal
    aodWaveSignals = cos(phi); % Time-domain signal

    %% Frequency analysis
    frequencies = linspace(-sampleRate / 2, sampleRate / 2, totalSampleCount); % Frequency axis (MHz)
    fftMagnitude = fftshift(abs(fft(aodWaveSignals)));                         % Fourier transform

    %% Plotting
    figure('Position', [100, 100, 1200, 800]);

    % 1. Frequency variation
    subplot(2, 2, 1);
    plot(sampleIndex, f, 'b', 'LineWidth', 1.5);
    xlabel('Sample Index');
    ylabel('Frequency (MHz)');
    title('Instantaneous Frequency');
    grid on;

    % 2. Phase variation
    subplot(2, 2, 2);
    plot(sampleIndex, phi, 'r', 'LineWidth', 1.5);
    xlabel('Sample Index');
    ylabel('Phase (rad)');
    title('Accumulated Phase');
    grid on;

    % 3. Time-domain signal
    subplot(2, 2, 3);
    plot(sampleIndex, aodWaveSignals, 'g', 'LineWidth', 1.5);
    xlabel('Sample Index');
    ylabel('Amplitude');
    title('Time-domain Signal');
    grid on;

    % 4. Frequency spectrum
    subplot(2, 2, 4);
    plot(frequencies, fftMagnitude, 'm', 'LineWidth', 1.5);
    xlabel('Frequency (MHz)');
    ylabel('Magnitude (dB)');
    xlim([0, 500]);
    title('Frequency Spectrum');
    grid on;

    sgtitle('Chirp Signal Analysis', 'FontSize', 14);

    %% Generate File
    y = int16(32768 * aodWaveSignals);
    y = int64(y);

    for i = 1:length(y)

        if y(i) < 0
            y(i) = y(i) + 2 ^ 32;
        end

    end

    strArray = dec2hex(y, 4);
    lastFourChars = strArray(:, end - 3:end);
    stringArray = string(lastFourChars);
    fileID = fopen(result, 'wt');

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
