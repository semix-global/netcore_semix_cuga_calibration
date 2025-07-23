clc;clearvars;close all;
% 定义基本参数
lambda = 266e-9; % 波长，单位为米
w0 = 1e-3; % 束腰半径，单位为米
z = 1; % 传播距离，单位为米

% 计算传播后的光束参数
k = 2 * pi / lambda; % 波数
ZR = pi * w0^2 / lambda; % 瑞利长度
w = w0 * sqrt(1 + (z / ZR)^2); % 传播到 z 处的束宽
R = z + ZR^2 / z; % 波前曲率半径

% 创建坐标网格
L = 10 * w; % 网格大小为 10 倍光束宽度
N = 100; % 网格点数
x = linspace(-L/2, L/2, N);
y = linspace(-L/2, L/2, N);
[x1, y1] = meshgrid(x, y);

% 计算光束的电场分布
r = sqrt(x1.^2 + y1.^2); % 极径
E_Gauss = exp(-r.^2 / (w^2)) .* exp(1i * (k * z - k * r.^2 / (2 * R) - atan(z / ZR)));

% 计算光强分布
I_Gauss = abs(E_Gauss).^2;

% 绘制光强分布图
figure;
surf(x1, y1, I_Gauss, 'LineStyle', 'none');
colormap(jet);
colorbar;
xlabel('x (m)');
ylabel('y (m)');
zlabel('Intensity');
title('Gaussian Beam Intensity Distribution after 0.5 m Propagation');
view(0, 90); % 从顶部查看光强分布

% 假设我们已经按照前面的步骤计算了 I_Gauss，现在提取 x=0 时的截面数据
x_section_index = round(N/2); % 假设截面在 x 的中心位置
x_section_intensity = I_Gauss(:, x_section_index);

% 绘制 x 轴方向的强度截面分布曲线
figure;
plot(x, x_section_intensity, 'LineWidth', 2);
xlabel('Position along x (m)');
ylabel('Intensity');
title('Intensity Distribution along x-axis at y=0');
grid on;