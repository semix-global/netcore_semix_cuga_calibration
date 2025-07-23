clc;clearvars;close all;

num=30;
x = linspace(0,5,num);

error = rand(1,num);

y1 = x.^2 + 2*error

p = polyfit(x,y1,2);

x1 = linspace(-5,10,num)
y2 = polyval(p,x1);

plot(x,y1,'o',x1,y2,'o')