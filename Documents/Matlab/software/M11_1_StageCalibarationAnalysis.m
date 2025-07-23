clc;close all;clearvars;

data_rows=18;
data_cols=20;

%%%理想值
fid1 = fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230515\src1818.csv');
data_rows1=data_rows;
data_cols1=data_cols;
data1= textscan(fid1, repmat('%s',[1,data_cols1]),data_rows1,'delimiter', ',');
fclose(fid1);
%
x_array1=zeros(data_rows1,data_cols1);
y_array1=zeros(data_rows1,data_cols1);
for i=1:data_cols1
     temp11=data1{i};
    for j=1:data_rows1
        temp12=temp11{j};
        index1=find(temp12=='-');
        
        x_temp1=str2double(temp12(1:index1-1));
        y_temp1=str2double(temp12(index1+1:end));
        
        x_array1(j,i)= x_temp1;
        y_array1(j,i)= y_temp1;
    end
end

%%实际值
fid2= fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230515\dst1818.csv');
data_rows2=data_rows;
data_cols2=data_cols;
data2= textscan(fid2, repmat('%s',[1,data_cols2]),data_rows2,'delimiter', ',');
fclose(fid2);

x_array2=zeros(data_rows2,data_cols2);
y_array2=zeros(data_rows2,data_cols2);
for i=1:data_cols2
     temp21=data2{i};
    for j=1:data_rows2
        temp22=temp21{j};
        index2=find(temp22=='-');
        
        x_temp2=str2double(temp22(1:index2-1));
        y_temp2=str2double(temp22(index2+1:end));
        
        x_array2(j,i)= x_temp2;
        y_array2(j,i)= y_temp2;
    end
end
 save('new_x_array1.mat','x_array1');
 save('new_y_array1.mat','y_array1');
 save('new_x_array2.mat','x_array2');
 save('new_y_array2.mat','y_array2');
%取得XY的坐标值
center_x2=mean(x_array2(:));%x_array2(1,1);
center_y2=mean(y_array2(:));%y_array2(1,1);

new_x_array2=x_array2;%-center_x2;
new_y_array2=y_array2;%-center_y2;



center_x1=mean(x_array1(:));%x_array2(1,1);
center_y1=mean(y_array1(:));%y_array2(1,1);
new_x_array1=x_array1;%-center_x1;
new_y_array1=y_array1;%-center_y1;


% figure;
% plot(new_y_array2,new_x_array2-new_x_array1);
% figure;
% plot(new_x_array2,new_y_array2-new_y_array1);


figure;
hold on;
xlabel('*为理想值，o为实际值，口为实际值校正后的值');

% new_x_array1=x_array11;%-center_x1;
% new_y_array1=y_array11;%-center_y1;
% new_x_array2=x_array22;%-center_x2;
% new_y_array2=y_array22;%-center_y2;

theta=zeros(data_rows1,1);
cross_x=zeros(data_rows1,1);
cross_y=zeros(data_rows1,1);
b1_array=zeros(data_rows1,1);


for i=1:data_rows1
  color1=roundn(rand(1,1),-1);
  color2=roundn(rand(1,1),-1);
  color3=roundn(rand(1,1),-1);
  xx1=new_x_array1(i,:);
  yy1=new_y_array1(i,:);
  xx2=new_x_array2(i,:);
  yy2=new_y_array2(i,:);
  plot(xx1,yy1,'Color',[color1 color2 color3],'Marker','*');
  plot(xx2,yy2,'Color',[color1 color2 color3],'Marker','o');
  
  
  [fitresult1, gof1] = createFit(xx1, yy1);%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1=fitresult1.p1;
  b1=fitresult1.p2;
  b1_array(i)=b1;
  
  [fitresult2, gof2] = createFit(xx2, yy2);%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2=fitresult2.p1;
  b2=fitresult2.p2;

  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;
  cross_x(i)=(b2-b1)/(k1-k2);%%直线交点
  cross_y(i)=(b2*k1-b1*k2)/(k1-k2);%%直线交点
  text(new_x_array1(i,1)+25000,new_y_array1(i,1),num2str(theta(i)),'Color',[color1 color2 color3]);
end
% save('new_x_array1.mat','new_x_array1');
% save('new_y_array1.mat','new_y_array1');
% save('new_x_array2.mat','new_x_array2');
% save('new_y_array2.mat','new_y_array2');

a=find( theta~=0);
theta_aver=mean(theta(a));
title(strcat('x方向偏置角度均值为：',num2str(theta_aver),'度'));


new_y_array2_correction=zeros(data_rows2,data_cols2);
for i=1:data_rows2
   new_y_array2_correction(i,:)=new_y_array2(i,:)-(new_x_array2(i,:)-cross_x(i))*tan(theta(i)/180*pi);
   
   xx3=new_x_array2(i,:);
   yy3=new_y_array2_correction(i,:);
   plot(xx3,yy3,'Color',[color1 color2 color3],'Marker','s');
end
 
corrected_error=new_y_array2_correction-new_y_array1;
%   wucha=sum(sum(sqrt((corrected_error).^2)))

figure;
quiver(new_x_array1,new_y_array1,new_x_array2-new_x_array1,new_y_array2-new_y_array1,'r');title('red-y轴修正前');
figure;
quiver(new_x_array1,new_y_array1,new_x_array2-new_x_array1,new_y_array2_correction-new_y_array1,'k','AutoScaleFactor',1,'ShowArrowHead','on');title('black-y轴修正后');
% axis([-10^6 10^6 -10^6 10^6]);
hold on;


plot(new_x_array2,new_y_array2_correction,'m*');
plot(new_x_array1,new_y_array1,'b.');
% axis([-10^6 10^6 -10^6 10^6]);


alpha=zeros(data_rows1,1);
for j=1:data_cols1
  xx2_correction=new_x_array2(:,j);
  yy2_correction=new_y_array2_correction(:,j);
 
  [fitresult, gof] = createFit(xx2_correction, yy2_correction);%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k=fitresult.p1;
  b=fitresult.p2;
  
  alpha(j)=atan(k)*180/pi;
end
alpha_aver=mean(alpha);

y_error_after_correction=new_y_array2_correction-new_y_array1;
y_error_before_correction=new_y_array2-new_y_array1;



%%%验证数据
figure;
plot(y_array1(1,:),'ro');
hold on;
plot(y_array2(1,:),'bo');

fid11 = fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230515\src1818.csv');
data_rows11=data_rows;
data_cols11=data_cols;
data11= textscan(fid11, repmat('%s',[1,data_cols11]),data_rows11,'delimiter', ',');
fclose(fid11);

x_array11=zeros(data_rows11,data_cols11);
y_array11=zeros(data_rows11,data_cols11);
for i=1:data_cols11
     temp111=data11{i};
    for j=1:data_rows11
        temp112=temp111{j};
        index11=find(temp112=='-');
        
        x_temp11=str2double(temp112(1:index1-1));
        y_temp11=str2double(temp112(index1+1:end));
        
        x_array11(j,i)= x_temp11;
        y_array11(j,i)= y_temp11;
    end
end

%%实际值
fid22= fopen('C:\Users\DELL\Desktop\stagemap\stagemap\software\stage_calibration\20230515\dst1818.csv');
data_rows22=data_rows;
data_cols22=data_cols;
data22= textscan(fid22, repmat('%s',[1,data_cols22]),data_rows22,'delimiter', ',');
fclose(fid22);

x_array22=zeros(data_rows22,data_cols22);
y_array22=zeros(data_rows22,data_cols22);
for i=1:data_cols22
     temp221=data22{i};
    for j=1:data_rows2
        temp222=temp221{j};
        index22=find(temp222=='-');
        
        x_temp22=str2double(temp222(1:index22-1));
        y_temp22=str2double(temp222(index22+1:end));
        
        x_array22(j,i)= x_temp22;
        y_array22(j,i)= y_temp22;
    end
end
figure;
plot(y_array11(1,:),'r*');
hold on;
plot(y_array22(1,:),'b*');








