clc;clearvars;close all;
% 读取图像
image1 = imread('C:\Users\Administrator\Desktop\2.jfif');  % 替换 'path_to_your_image.jpg' 为你的图像文件路径
image2 = image1(:,1:328);  % 初始化求和数组为零数组

totalSum = sum(image2,2);
ave=totalSum./328;

image3 = imread('C:\Users\Administrator\Desktop\1.jpg');  % 替换 'path_to_your_image.jpg' 为你的图像文件路径
image4 = image3(:,400:800);  % 初始化求和数组为零数组

totalSum1 = sum(image4,2);
ave1=totalSum1./401;

figure(1)
plot(ave);
hold on
plot(ave1);
