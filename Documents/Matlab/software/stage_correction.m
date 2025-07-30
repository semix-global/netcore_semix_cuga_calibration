%%%stage 理论数据xx1 误差实际数据xx2 校正后数据xx3 
%%%包含伸缩变换 剪切变换
clc;clearvars;close all;

%%%理论数据xx1
load( 'new_x_array1.mat');
load( 'new_y_array1.mat');
load( 'new_x_array2.mat');
load( 'new_y_array2.mat');
%%%误差数据重组 xx2
lengthx=31;lengthy=31;
T1=[1 0 -1*new_x_array1(1,32)
    0 1 -1*new_y_array1(1,32)
    0 0 1];
T2=[1 0 -1*new_x_array2(1,32)
    0 1 -1*new_y_array2(1,32)
    0 0 1];
for i=1:1:lengthx+1
    for j=1:1:lengthy+1
        temp= T1*[new_x_array1(i,j) new_y_array1(i,j) 1]';
        xxt1(i,j)=temp(1);
        yyt1(i,j)=temp(2);
    end
end
for i=1:1:lengthx+1
    for j=1:1:lengthy+1
        temp= T2*[new_x_array2(i,j) new_y_array2(i,j) 1]';
        xxt2(i,j)=temp(1);
        yyt2(i,j)=temp(2);
    end
end
%%%重组 左下角为0
for i=1:1:lengthx+1
    for j=1:1:lengthy+1
        xx2(i,j)=xxt2(33-i,33-j);
        yy2(i,j)=-1*yyt2(33-i,33-j);
        xx1(i,j)=xxt1(33-i,33-j);
        yy1(i,j)=-1*yyt1(33-i,33-j);
    end
end

 %%%寻找误差项 伸缩系数sx sy 剪切系数angle_jy
wucha=[];xishu=[];
angle_jx=-0.0123;
angle_jy=-0.003;
tlx=0;%-30;
tly=0;%20;
for angle_sita=0%:0.0001:-0.012
    for sx=1.00053%:0.0001:1.00053
        for sy=1.00047%:0.0001:1.0005
%%%随机误差 角度变换 x弯度sin y弯度sin            
%伸缩变换 变换矩阵为Ts 剪切变换Tjy 
sita=angle_sita/180*pi;
Tl=[1 0 tlx
    0 1 tly
    0 0 1];
Ts=[sx 0 0
    0 sy 0
    0 0 1];
Tjy=[1 0 0
    tan(angle_jy/180*pi) 1 0
    0 0 1];
Tjx=[1 tan(angle_jx/180*pi) 0
     0 1 0
    0 0 1];
Tr=[cos(sita) sin(sita) 0
    -sin(sita) cos(sita) 0
    0 0 1];
T=Tl*Tjy*Tjx*Ts;
%%%误差数据线性映射 xx3 yy3
for i=1:1:lengthx+1
    for j=1:1:lengthy+1
        temp= T*[xx1(i,j) yy1(i,j) 1]';
        xx3(i,j)=temp(1);
        yy3(i,j)=temp(2);
    end
end
%%%误差数据非线性映射 sin
Tx=1550004*1;Ty=1549997*1;Hx=-35;Hy=35;
for i=1:1:lengthx+1
    for j=1:1:lengthy+1
xx4(i,j)=xx3(i,j)+Hx*sin(yy3(i,j)/Tx*pi);
yy4(i,j)=yy3(i,j)+Hy*sin(xx3(i,j)/Ty*pi);
    end
    end
%   xx3=xx4;yy3=yy4;
%%%假设模型

%%%坐标差 平方根和
%  wucha=[wucha sqrt(sum(sum((xx3-xx2).^2+(yy3-yy2).^2)))];
% wucha=[wucha 10*sqrt(sum(sum((xx3(1,1)-xx2(1,1)).^2)))+sqrt(sum(sum((xx3(1,32)-xx2(1,32)).^2)))+10*sqrt(sum(sum((xx3(32,1)-xx2(32,1)).^2)))];
%  xishu=[xishu ;[angle_sita sx sy]];
        end
    end
end
% min(wucha)
% min_count=find(wucha==min(wucha))
% xishu(min_count,:)
% figure(1)
% plot(wucha)

figure(1)
hold on;
xlabel('*为理想值，o为实际值，口为实际值校正后的值');
 plot(xx1,yy1,'blue','Marker','*');
 plot(xx2,yy2,'red','Marker','o');
 plot(xx4,yy4,'green','Marker','s'); 
%   plot(xx3,yy3,'black','Marker','s'); 

%    figure(2)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(32,:),yy1(32,:),'blue','Marker','*');
%  plot(xx2(32,:),yy2(32,:),'red','Marker','o');
%  plot(xx4(32,:),yy4(32,:),'green','Marker','s'); 
%  
%   figure(3)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(1,:),yy1(1,:),'blue','Marker','*');
%  plot(xx2(1,:),yy2(1,:),'red','Marker','o');
%  plot(xx4(1,:),yy4(1,:),'green','Marker','s'); 
%   
%   figure(4)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(15,:),yy1(15,:),'blue','Marker','*');
%  plot(xx2(15,:),yy2(15,:),'red','Marker','o');
%  plot(xx4(15,:),yy4(15,:),'green','Marker','s'); 
%  
%   figure(5)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(:,1),yy1(:,1),'blue','Marker','*');
%  plot(xx2(:,1),yy2(:,1),'red','Marker','o');
%  plot(xx4(:,1),yy4(:,1),'green','Marker','s'); 
%   
%   figure(6)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(:,15),yy1(:,15),'blue','Marker','*');
%  plot(xx2(:,15),yy2(:,15),'red','Marker','o');
%  plot(xx4(:,15),yy4(:,15),'green','Marker','s'); 
%  
%    figure(7)
% hold on;
% xlabel('*为理想值，o为实际值，口为实际值校正后的值');
%  plot(xx1(:,32),yy1(:,32),'blue','Marker','*');
%  plot(xx2(:,32),yy2(:,32),'red','Marker','o');
%  plot(xx4(:,32),yy4(:,32),'green','Marker','s'); 
  
for si= 1:1:32
sinn1(si)=sum(((yy4(:,si)-yy2(:,si))))/32;
end
for si= 1:1:32
sinn2(si)=sum(((xx4(:,si)-xx2(:,si))))/32;
end
figure(8)
hold on
plot(1:32,sinn1,'*')
plot(1:32,sinn2,'s')
% plot(1:32,50*sin(yy3(:,1)/Tx*pi))
% sin(yy3(1,32)/Tx*pi)
% corrected_error_x=xx4-xx2;
% corrected_error_y=yy4-yy2;
% figure;
% surf(corrected_error_x)
% figure;
% surf(corrected_error_y)
% figure;
% quiver(xx1,yy1,xx1-xx2,yy1-yy2,'k','AutoScaleFactor',1,'ShowArrowHead','on');title('线性变换修正后');
% figure;
% quiver(xx1,yy1,xx4-xx2,yy4-yy2,'k','AutoScaleFactor',1,'ShowArrowHead','on');title('非线性变换修正后');
%%%误差定量计算
wucha=sum(sum(sqrt((xx4-xx2).^2+(yy4-yy2).^2)))
% wucha=sum(sum(sqrt((xx1-xx2).^2+(yy1-yy2).^2)))
%  wucha=sum(sum(sqrt((yy4-yy2).^2)))
 
