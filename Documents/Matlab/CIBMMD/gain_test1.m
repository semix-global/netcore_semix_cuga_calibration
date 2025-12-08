clc;

data = readmatrix('Test.xlsx');
num_cols = size(data, 2);
num_rows = size(data, 1);
a = num_cols - 1;
b = num_rows - 1;

A = zeros(b*a, b+a);
for i = 1:a
    A((b*i-(b-1):b*i), i) = 1;
end
for j = 1:b, k = 0:(a-1);
    A(b*k+j,j+a) = 1;
end

B_known = data(1,2:end);
log2_B_known = log2(B_known);
B_known = log2_B_known(:);

Darkcurrent = 0;      % 暗电流底噪去除
C = data(2:end, 2:end);
numerator = C - Darkcurrent;
denominator = 16384;    
fraction = (numerator ./ denominator) * 2000000;
% fraction = numerator;
fraction(fraction <= 0) = NaN;        % 将 <=0 的值设为 NaN
fraction(fraction >= 250000) = NaN;
log2_C = log2(fraction);              % log2_C = log2((C-Darkcurrent)./(16384*((4096-Darkcurrent)/4096))*2000000);
C = log2_C(:);

%去除无意义值
valid_idx = ~isnan(C);
A_valid = A(valid_idx, :);
C_valid = C(valid_idx);

A1 = A_valid(:, 1:a);
A2 = A_valid(:, (a+1):end);

B2 = A2 \ (C_valid - A1 * B_known);
B = [B_known; B2];

residual = norm(A_valid * B - C_valid);
disp(['有效数据残差: ', num2str(residual)]);
disp(['B2的范数: ', num2str(norm(B2))]);

V = data(2:end,1);
Gain = 2.^B2;
figure;
subplot(2,1,1);
plot(V,Gain);
xlabel('Voltage');
ylabel('Gain');
subplot(2, 1, 2);
plot(V,B2);
xlabel('Voltage');
ylabel('Log Gain');


