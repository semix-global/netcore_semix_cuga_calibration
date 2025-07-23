%%%求模拟数据的校准系数
clc;clearvars;close all;


data_rows=10;
data_cols=10;
fid1 = fopen('C:\Users\Administrator\Desktop\rawdata\src.csv');
data_rows1=data_rows;
data_cols1=data_cols;
data1= textscan(fid1, repmat('%s',[1,data_cols1]),data_rows1,'delimiter', ',');
fclose(fid1);

x_array1=zeros(data_rows1,data_cols1);
y_array1=zeros(data_rows1,data_cols1); 
for i=1:data_cols1
     temp11=data1{i};
    for j=1:data_rows1
        temp12=temp11{j};
        index1=find(temp12=='^');
        
        x_temp1=str2double(temp12(1:index1-1));
        y_temp1=str2double(temp12(index1+1:end));
        
        x_array1(j,i)= x_temp1;
        y_array1(j,i)= y_temp1;
    end
end

%%实际值
fid2= fopen('C:\Users\Administrator\Desktop\rawdata\dst.csv');
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
            index2=find(temp22=='^');
        
            x_temp2=str2double(temp22(1:index2-1));
            y_temp2=str2double(temp22(index2+1:end));
        
            x_array2(j,i)= x_temp2;
            y_array2(j,i)= y_temp2;
        end
end
xx1=x_array1;
yy1=y_array1;
xx2=x_array2;
yy2=y_array2;
lengthx=data_rows;lengthy=data_cols;

for i=1:lengthx
  [fitresult1, gof1] = createFit(xx1(i,:), yy1(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1=fitresult1.p1;
  b1=fitresult1.p2;
 % k1_array(i)=k1;
 % b1_array(i)=b1;
  [fitresult2, gof2] = createFit(xx2(i,:), yy2(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2=fitresult2.p1;
  b2=fitresult2.p2;
  %k2_array(i)=k2;
  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;  %%%每行的夹角
end

mean_theta=mean(theta);
%%%角度校准
sita=mean_theta*pi/180;
Tj=[cos(sita) -1*sin(sita) 0
    sin(sita) cos(sita) 0
    0 0 1];
for i=1:1:lengthx
    for j=1:1:lengthy
        temp= Tj*[xx1(i,j) yy1(i,j) 1]';
        xx1(i,j)=temp(1);
        yy1(i,j)=temp(2);
    end
end

figure(1)
plot(xx1(:,6),yy1(:,6));
hold on
plot(xx2(:,6),yy2(:,6));

hold off
p = polyfit(xx2(:,6), yy2(:,6), 2);
figure(2)
x_fit = linspace(xx2(1,6), xx2(10,6), 100); % 创建一个密集的x值用于绘制拟合曲线
y_fit = polyval(p, x_fit);

% 绘制原始数据点和拟合曲线
plot(xx2(:,6), yy2(:,6), 'o', x_fit, y_fit, '-');


for i=1:lengthx
  [fitresult1, gof1] = createFit(xx1(:,i), yy1(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1(i)=fitresult1.p1;
  b1=fitresult1.p2;
 % k1_array(i)=k1;
 % b1_array(i)=b1;
  [fitresult2, gof2] = createFit(xx2(:,i), yy2(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2(i)=fitresult2.p1;
  b2=fitresult2.p2;

  theta1(i)=atan(((k2(i)-k1(i))/(1+k1(i)*k2(i))))*180/pi;  %%%每列的夹角
end

mean_theta1=mean(theta1)*pi/180;



for i=1:lengthx
    for j=1:lengthy
        map(i,j)=-(yy1(i,j)-yy1(1,j))*tan(mean_theta1);
    end
end
figure(3)
surf(map);
