%%%stage 理论数据xx1 误差仿真数据xx2 
%%%包含伸缩变换 角度变换 波动 随机 
clc;clearvars;close all;

%%%理论数据xx1
lenghtx=40;lenghty=40;
xv=0:lenghtx-1;
yv=0:lenghty-1;
[xx1,yy1]=meshgrid(xv,yv);

%校准误差模拟 角度变换Tj 伸缩变换Ts 平移Tp  
Ts=[1.002 0 0
    0 1.005 0
    0 0 1];
sita=0.005*pi/180;
Tj=[cos(sita) sin(sita) 0
    -1*sin(sita) cos(sita) 0
    0 0 1];
Tp=[1 0 0
    0 1 0
    0 0 1];

T=Tp*Ts*Tj;
%%%误差仿真数据xx2
for i=1:1:lenghtx
    for j=1:1:lenghty
        temp= T*[xx1(i,j) yy1(i,j) 1]';
        xx2(i,j)=temp(1);
        yy2(i,j)=temp(2);
    end
end

%%%残余误差模拟
%%%正态分布随机误差Qsx Qsy 低频波浪误差Qbx Qby 
Qsx=rand(lenghtx)*0.001;   %0~1um
Qsy=rand(lenghty)*0.001;  %%%0~1um
for i=1:1:lenghtx
    for j=1:1:lenghty
Qbx(i,j)=0.001*sin(yy1(i,j)/30*pi);
Qby(i,j)=0.001*sin(xx1(i,j)/20*pi);
    end;
end

 xx2=xx2+Qsx*1+Qbx*1;
 yy2=yy2+Qsy*1+Qby*1;
%%%绘制误差
figure(1);
hold on;
xlabel('*为理想值，o为实际值，口为实际值校正后的值');
 plot(xx1,yy1,'blue','Marker','*');
 plot(xx2,yy2,'red','Marker','o');
 figure(2);
quiver(xx1,yy1,xx2-xx1,yy2-yy1,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');
save( 'new_x_array1.mat','xx1');
save( 'new_y_array1.mat','yy1');
save( 'new_x_array2.mat','xx2');
save( 'new_y_array2.mat','yy2');



% %%%寻找误差项 伸缩系数sx sy 剪切系数angle_jy
% wucha=[];xishu=[];
% for angle_jy=0:0.02:0.2
%     for sx=0.98:0.002:1.02
%         for sy=0.98:0.002:1.02
%             
% %伸缩变换 变换矩阵为Ts 剪切变换Tjy 
% Ts=[sx 0 0
%     0 sy 0
%     0 0 1];
% Tjy=[1 tan(angle_jy/180*pi) 0
%     0 1 0
%     0 0 1];
% T=Tjy*Ts;
% %%%误差仿真数据xx3
% for i=1:1:lenghtx+1
%     for j=1:1:lengthy+1
%         temp= T*[xx1(i,j) yy1(i,j) 1]';
%         xx3(i,j)=temp(1);
%         yy3(i,j)=temp(2);
%     end
% end
% %%%坐标差 平方根和
%  wucha=[wucha sqrt(sum(sum((xx3-xx2).^2)))+sqrt(sum(sum((yy3-yy2).^2)))];
%  xishu=[xishu ;[angle_jy sx sy]];
%         end
%     end
% end
% figure(2)
% plot(wucha)